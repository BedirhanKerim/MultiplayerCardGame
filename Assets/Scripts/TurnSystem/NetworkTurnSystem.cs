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

    [Networked] private int _currentTurn { get; set; }
    [Networked] private TurnPhase _phase { get; set; }
    [Networked] private float _turnTimer { get; set; }
    [Networked] private int _player1Hp { get; set; }
    [Networked] private int _player2Hp { get; set; }
    [Networked] private int _player1CardId { get; set; }
    [Networked] private int _player2CardId { get; set; }
    [Networked] private NetworkBool _player1Confirmed { get; set; }
    [Networked] private NetworkBool _player2Confirmed { get; set; }

    private TurnConfigSO _config;
    private ChangeDetector _changeDetector;
    private PlayerRef _player1;
    private PlayerRef _player2;

    // ITurnSystem — local oyuncu perspektifinden
    public int CurrentTurn => _currentTurn;
    public int MaxTurns => _config != null ? _config.MaxTurns : 6;
    public int PlayerHp => IsPlayer1() ? _player1Hp : _player2Hp;
    public int OpponentHp => IsPlayer1() ? _player2Hp : _player1Hp;
    public bool IsPlayerTurn => _phase == TurnPhase.Playing;

    public void InjectConfig(TurnConfigSO config)
    {
        _config = config;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotTo, false);

        var players = Runner.ActivePlayers.ToArray();
        if (players.Length >= 2)
        {
            _player1 = players[0];
            _player2 = players[1];
        }

        // Client tarafında da inject ve GameplayManager'a set
        if (_gameplayManager != null)
            _gameplayManager.SetTurnSystem(this);

        if (Object.HasStateAuthority)
        {
            _player1Hp = _config.StartingHp;
            _player2Hp = _config.StartingHp;
            _currentTurn = 0;
            _phase = TurnPhase.WaitingForPlayers;
        }
    }

    public void StartGame()
    {
        if (!Object.HasStateAuthority) return;
        StartNextTurn();
    }

    private void StartNextTurn()
    {
        _currentTurn++;
        _player1CardId = -1;
        _player2CardId = -1;
        _player1Confirmed = false;
        _player2Confirmed = false;
        _turnTimer = _config.TurnDuration;
        _phase = TurnPhase.Playing;
    }

    // Oyuncu kartı masaya koydu
    public void PlaceCard(CardInstance card)
    {
        RPC_PlaceCard(card.Data.CardId);
    }

    // Oyuncu kartı geri çekti
    public void RemoveCard()
    {
        RPC_RemoveCard();
    }

    // Oyuncu turu onayladı
    public void ConfirmTurn()
    {
        RPC_ConfirmTurn();
    }

    public void Tick(float deltaTime)
    {
        // Timer FixedUpdateNetwork'te yönetiliyor
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PlaceCard(int cardId, RpcInfo info = default)
    {
        if (_phase != TurnPhase.Playing) return;

        if (info.Source == _player1)
            _player1CardId = cardId;
        else if (info.Source == _player2)
            _player2CardId = cardId;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RemoveCard(RpcInfo info = default)
    {
        if (_phase != TurnPhase.Playing) return;

        if (info.Source == _player1)
            _player1CardId = -1;
        else if (info.Source == _player2)
            _player2CardId = -1;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ConfirmTurn(RpcInfo info = default)
    {
        if (_phase != TurnPhase.Playing) return;

        if (info.Source == _player1)
            _player1Confirmed = true;
        else if (info.Source == _player2)
            _player2Confirmed = true;

        if (_player1Confirmed && _player2Confirmed)
            ResolveTurn();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (_phase != TurnPhase.Playing) return;

        _turnTimer -= Runner.DeltaTime;

        if (_turnTimer <= 0f)
        {
            _turnTimer = 0f;
            ResolveTurn();
        }
    }

    private void ResolveTurn()
    {
        _phase = TurnPhase.Resolving;

        var db = _deckBuilderManager.CardDatabase;
        CardInstance p1Card = FindCard(db, _player1CardId);
        CardInstance p2Card = FindCard(db, _player2CardId);

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

        _player1Hp = Mathf.Max(0, _player1Hp - p1Damage);
        _player2Hp = Mathf.Max(0, _player2Hp - p2Damage);

        if (_player1Hp <= 0 || _player2Hp <= 0 || _currentTurn >= _config.MaxTurns)
        {
            _phase = TurnPhase.GameOver;
        }
        else
        {
            StartNextTurn();
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

    // Her iki client'ta çalışır — state değişikliklerini event'e çevirir
    public override void Render()
    {
        if (_changeDetector == null) return;

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
            }
        }
    }

    private void OnPhaseChanged()
    {
        if (_phase == TurnPhase.Resolving)
        {
            var db = _deckBuilderManager.CardDatabase;
            bool isP1 = IsPlayer1();
            var myCard = FindCard(db, isP1 ? _player1CardId : _player2CardId);
            var oppCard = FindCard(db, isP1 ? _player2CardId : _player1CardId);

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

    private bool IsPlayer1()
    {
        return Runner.LocalPlayer == _player1;
    }
}
