using System.Linq;
using Fusion;
using UnityEngine;
using VContainer;

public enum TurnPhase { WaitingForPlayers, Playing, Resolving, GameOver }

public class NetworkTurnSystem : NetworkBehaviour, ITurnSystem
{
    [Inject] private GameEventBus _eventBus;
    [Inject] private DeckBuilderManager _deckBuilderManager;
    [Inject] private GameplayManager _gameplayManager;
    [Inject] private TurnConfigSO _config;
    [Inject] private SkillConfigSO _skillConfig;

    [Networked] private int _currentTurn { get; set; }
    [Networked] private TurnPhase _phase { get; set; }
    [Networked] private float _turnTimer { get; set; }
    [Networked] private int _player1Hp { get; set; }
    [Networked] private int _player2Hp { get; set; }
    [Networked] private int _player1CardId { get; set; }
    [Networked] private int _player2CardId { get; set; }
    [Networked] private NetworkBool _player1Confirmed { get; set; }
    [Networked] private NetworkBool _player2Confirmed { get; set; }

    [Networked] private PlayerRef _player1 { get; set; }
    [Networked] private PlayerRef _player2 { get; set; }

    // Skill networked properties
    [Networked] private int _player1SkillType { get; set; }
    [Networked] private int _player2SkillType { get; set; }
    [Networked] private NetworkBool _player1UsedSkill { get; set; }
    [Networked] private NetworkBool _player2UsedSkill { get; set; }
    [Networked] private int _player1NextTurnAtkBoost { get; set; }
    [Networked] private int _player2NextTurnAtkBoost { get; set; }

    // Resolved card stats after skill application
    [Networked] private int _resolvedP1Atk { get; set; }
    [Networked] private int _resolvedP1Def { get; set; }
    [Networked] private int _resolvedP2Atk { get; set; }
    [Networked] private int _resolvedP2Def { get; set; }

    private ChangeDetector _changeDetector;

    public int CurrentTurn => _currentTurn;
    public int MaxTurns => _config != null ? _config.MaxTurns : 6;
    public int PlayerHp => IsPlayer1() ? _player1Hp : _player2Hp;
    public int OpponentHp => IsPlayer1() ? _player2Hp : _player1Hp;
    public bool IsPlayerTurn => _phase == TurnPhase.Playing;

    public SkillType AssignedSkill => (SkillType)(IsPlayer1() ? _player1SkillType : _player2SkillType);
    public bool IsSkillUsed => IsPlayer1() ? (bool)_player1UsedSkill : (bool)_player2UsedSkill;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);
        _gameplayManager.SetTurnSystem(this);
    }

    public void StartGame()
    {
        if (!Object.HasStateAuthority) return;

        var players = Runner.ActivePlayers.ToArray();
        _player1 = players[0];
        _player2 = players[1];

        _player1Hp = _config.StartingHp;
        _player2Hp = _config.StartingHp;
        _player1NextTurnAtkBoost = 0;
        _player2NextTurnAtkBoost = 0;
        _currentTurn = 0;
        StartNextTurn();
    }

    private void StartNextTurn()
    {
        _currentTurn++;
        _player1CardId = -1;
        _player2CardId = -1;
        _player1Confirmed = false;
        _player2Confirmed = false;
        _player1UsedSkill = false;
        _player2UsedSkill = false;
        _player1SkillType = Random.Range(0, 6);
        _player2SkillType = Random.Range(0, 6);
        _turnTimer = _config.TurnDuration;
        _phase = TurnPhase.Playing;
    }

    public bool IsLocalConfirmed => IsPlayer1() ? (bool)_player1Confirmed : (bool)_player2Confirmed;

    public void PlaceCard(CardInstance card)
    {
        if (IsLocalConfirmed) return;
        RPC_PlaceCard(card.Data.CardId);
    }

    public void RemoveCard()
    {
        if (IsLocalConfirmed) return;
        RPC_RemoveCard();
    }

    public void ConfirmTurn()
    {
        if (IsLocalConfirmed) return;
        RPC_ConfirmTurn();
    }

    public void UseSkill()
    {
        if (IsLocalConfirmed || IsSkillUsed) return;
        RPC_UseSkill();
    }

    public void Tick(float deltaTime)
    {
        // Timer FixedUpdateNetwork'te yönetiliyor
    }

    private PlayerRef GetSource(RpcInfo info)
    {
        return info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PlaceCard(int cardId, RpcInfo info = default)
    {
        var source = GetSource(info);
        Debug.Log($"[NTS] RPC_PlaceCard cardId={cardId} source={source} phase={_phase}");
        if (_phase != TurnPhase.Playing) return;

        if (source == _player1)
            _player1CardId = cardId;
        else if (source == _player2)
            _player2CardId = cardId;
        Debug.Log($"[NTS] After PlaceCard: P1Card={_player1CardId}, P2Card={_player2CardId}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RemoveCard(RpcInfo info = default)
    {
        if (_phase != TurnPhase.Playing) return;
        var source = GetSource(info);

        if (source == _player1)
            _player1CardId = -1;
        else if (source == _player2)
            _player2CardId = -1;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ConfirmTurn(RpcInfo info = default)
    {
        var source = GetSource(info);
        Debug.Log($"[NTS] RPC_ConfirmTurn source={source} phase={_phase}");
        if (_phase != TurnPhase.Playing) return;

        if (source == _player1)
            _player1Confirmed = true;
        else if (source == _player2)
            _player2Confirmed = true;

        Debug.Log($"[NTS] P1Confirmed={_player1Confirmed}, P2Confirmed={_player2Confirmed}");

        if (_player1Confirmed && _player2Confirmed)
        {
            Debug.Log("[NTS] Both confirmed, resolving turn...");
            ResolveTurn();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UseSkill(RpcInfo info = default)
    {
        var source = GetSource(info);
        if (_phase != TurnPhase.Playing) return;

        if (source == _player1)
        {
            if ((bool)_player1Confirmed || (bool)_player1UsedSkill) return;
            _player1UsedSkill = true;
        }
        else if (source == _player2)
        {
            if ((bool)_player2Confirmed || (bool)_player2UsedSkill) return;
            _player2UsedSkill = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (_phase == TurnPhase.Playing)
        {
            _turnTimer -= Runner.DeltaTime;
            if (_turnTimer <= 0f)
            {
                _turnTimer = 0f;
                ResolveTurn();
            }
        }
        else if (_phase == TurnPhase.Resolving)
        {
            _turnTimer -= Runner.DeltaTime;
            if (_turnTimer <= 0f)
            {
                if (_player1Hp <= 0 || _player2Hp <= 0 || _currentTurn >= _config.MaxTurns)
                    _phase = TurnPhase.GameOver;
                else
                    StartNextTurn();
            }
        }
    }

    private void ResolveTurn()
    {
        Debug.Log($"[NTS] ResolveTurn! P1Card={_player1CardId}, P2Card={_player2CardId}, P1Hp={_player1Hp}, P2Hp={_player2Hp}");
        _phase = TurnPhase.Resolving;

        var db = _deckBuilderManager.CardDatabase;
        CardInstance p1Card = FindCard(db, _player1CardId);
        CardInstance p2Card = FindCard(db, _player2CardId);

        // Apply previous turn's Shield carry-over (Skill 6 next-turn ATK boost)
        if (p1Card != null && _player1NextTurnAtkBoost > 0)
            p1Card.CurrentAttack += _player1NextTurnAtkBoost;
        if (p2Card != null && _player2NextTurnAtkBoost > 0)
            p2Card.CurrentAttack += _player2NextTurnAtkBoost;
        _player1NextTurnAtkBoost = 0;
        _player2NextTurnAtkBoost = 0;

        // Apply skill effects for Player 1
        if ((bool)_player1UsedSkill)
            ApplySkillEffects((SkillType)_player1SkillType, ref p1Card, ref p2Card, isPlayer1: true);

        // Apply skill effects for Player 2
        if ((bool)_player2UsedSkill)
            ApplySkillEffects((SkillType)_player2SkillType, ref p2Card, ref p1Card, isPlayer1: false);

        // Calculate damage
        int p1Damage = 0;
        int p2Damage = 0;

        if (p1Card != null && p2Card != null)
        {
            int p1Atk = p1Card.CurrentAttack - p2Card.CurrentDefense;
            if (p1Atk > 0) p2Damage = p1Atk;

            int p2Atk = p2Card.CurrentAttack - p1Card.CurrentDefense;
            if (p2Atk > 0) p1Damage = p2Atk;
        }
        else if (p1Card == null && p2Card != null)
        {
            p1Damage = p2Card.CurrentAttack;
        }
        else if (p1Card != null && p2Card == null)
        {
            p2Damage = p1Card.CurrentAttack;
        }

        // Apply Shield absorb
        if ((bool)_player1UsedSkill && (SkillType)_player1SkillType == SkillType.Shield)
        {
            p1Damage = Mathf.Max(0, p1Damage - _skillConfig.ShieldAbsorb);
            _player2NextTurnAtkBoost = _skillConfig.ShieldOpponentAttackBoost;
        }
        if ((bool)_player2UsedSkill && (SkillType)_player2SkillType == SkillType.Shield)
        {
            p2Damage = Mathf.Max(0, p2Damage - _skillConfig.ShieldAbsorb);
            _player1NextTurnAtkBoost = _skillConfig.ShieldOpponentAttackBoost;
        }

        // Apply HealPlayer
        if ((bool)_player1UsedSkill && (SkillType)_player1SkillType == SkillType.HealPlayer)
            _player1Hp += _skillConfig.HealAmount;
        if ((bool)_player2UsedSkill && (SkillType)_player2SkillType == SkillType.HealPlayer)
            _player2Hp += _skillConfig.HealAmount;

        // Store resolved card stats for clients
        _resolvedP1Atk = p1Card != null ? p1Card.CurrentAttack : 0;
        _resolvedP1Def = p1Card != null ? p1Card.CurrentDefense : 0;
        _resolvedP2Atk = p2Card != null ? p2Card.CurrentAttack : 0;
        _resolvedP2Def = p2Card != null ? p2Card.CurrentDefense : 0;

        _player1Hp = Mathf.Max(0, _player1Hp - p1Damage);
        _player2Hp = Mathf.Max(0, _player2Hp - p2Damage);
        _turnTimer = _config.ResolveDuration;
    }

    private void ApplySkillEffects(SkillType skill, ref CardInstance myCard, ref CardInstance oppCard, bool isPlayer1)
    {
        switch (skill)
        {
            case SkillType.BoostAttack:
                if (myCard != null)
                    myCard.CurrentAttack += _skillConfig.AttackBoost;
                break;
            case SkillType.BoostDefense:
                if (myCard != null)
                    myCard.CurrentDefense += _skillConfig.DefenseBoost;
                break;
            case SkillType.ReduceOpponentAttack:
                if (oppCard != null)
                    oppCard.CurrentAttack = Mathf.Max(0, oppCard.CurrentAttack - _skillConfig.OpponentAttackReduction);
                break;
            case SkillType.ReduceOpponentDefense:
                if (oppCard != null)
                    oppCard.CurrentDefense = Mathf.Max(0, oppCard.CurrentDefense - _skillConfig.OpponentDefenseReduction);
                break;
            // HealPlayer and Shield are handled separately in ResolveTurn
        }
    }

    private CardInstance FindCard(CardDatabaseSO db, int cardId)
    {
        if (cardId < 0) return null;
        foreach (var entry in db.Cards)
        {
            if (entry.CardId == cardId)
                return new CardInstance(entry);
        }
        return null;
    }

    public override void Render()
    {
        if (_changeDetector == null || _eventBus == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(_currentTurn):
                    _eventBus.Raise(new TurnStarted { TurnNumber = _currentTurn });
                    break;
                case nameof(_turnTimer):
                    _eventBus.Raise(new TurnTimerUpdated { RemainingTime = _turnTimer });
                    break;
                case nameof(_phase):
                    OnPhaseChanged();
                    break;
                case nameof(_player1CardId):
                case nameof(_player2CardId):
                    OnOpponentCardIdChanged();
                    break;
                case nameof(_player1Confirmed):
                case nameof(_player2Confirmed):
                    if (IsLocalConfirmed)
                        _eventBus.Raise(new LocalPlayerConfirmed());
                    break;
                case nameof(_player1SkillType):
                case nameof(_player2SkillType):
                    _eventBus.Raise(new SkillAssigned { Skill = AssignedSkill });
                    break;
                case nameof(_player1UsedSkill):
                case nameof(_player2UsedSkill):
                    if (IsSkillUsed)
                        _eventBus.Raise(new SkillUsed());
                    break;
            }
        }
    }

    private void OnPhaseChanged()
    {
        Debug.Log($"[NTS] OnPhaseChanged: {_phase}");
        if (_phase == TurnPhase.Resolving)
        {
            var db = _deckBuilderManager.CardDatabase;
            bool isP1 = IsPlayer1();
            var myCard = FindCard(db, isP1 ? _player1CardId : _player2CardId);
            var oppCard = FindCard(db, isP1 ? _player2CardId : _player1CardId);

            // Apply resolved stats from host
            if (myCard != null)
            {
                myCard.CurrentAttack = isP1 ? _resolvedP1Atk : _resolvedP2Atk;
                myCard.CurrentDefense = isP1 ? _resolvedP1Def : _resolvedP2Def;
            }
            if (oppCard != null)
            {
                oppCard.CurrentAttack = isP1 ? _resolvedP2Atk : _resolvedP1Atk;
                oppCard.CurrentDefense = isP1 ? _resolvedP2Def : _resolvedP1Def;
            }

            Debug.Log($"[NTS] SimResult: isP1={isP1}, myCard={myCard?.Data.CardId}, oppCard={oppCard?.Data.CardId}, P1Hp={_player1Hp}, P2Hp={_player2Hp}");

            _eventBus.Raise(new TurnEnded { TurnNumber = _currentTurn });
            _eventBus.Raise(new SimulationResult
            {
                PlayerCard = myCard,
                OpponentCard = oppCard,
                PlayerDamage = 0,
                OpponentDamage = 0,
                PlayerHp = PlayerHp,
                OpponentHp = OpponentHp
            });
        }
        else if (_phase == TurnPhase.GameOver)
        {
            bool isDraw = _player1Hp == _player2Hp;
            bool playerWon = IsPlayer1() ? _player1Hp > _player2Hp : _player2Hp > _player1Hp;
            _eventBus.Raise(new GameOver { PlayerWon = playerWon, IsDraw = isDraw });
        }
    }

    private void OnOpponentCardIdChanged()
    {
        bool isP1 = IsPlayer1();
        int oppCardId = isP1 ? _player2CardId : _player1CardId;

        if (oppCardId >= 0)
        {
            var card = FindCard(_deckBuilderManager.CardDatabase, oppCardId);
            _eventBus.Raise(new OpponentCardChanged { Card = card });
        }
        else
        {
            _eventBus.Raise(new OpponentCardChanged { Card = null });
        }
    }

    private bool IsPlayer1()
    {
        return Runner.LocalPlayer == _player1;
    }
}
