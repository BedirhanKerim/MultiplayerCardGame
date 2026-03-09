using UnityEngine;
using VContainer;
using VContainer.Unity;

public class LocalTurnSystem : ITurnSystem, IStartable
{
    [Inject] private GameEventBus _eventBus;
    [Inject] private IOpponent _opponent;
    [Inject] private SkillConfigSO _skillConfig;
    [Inject] private GameplayManager _gameplayManager;

    private TurnConfigSO _config;
    private float _turnTimer;
    private bool _timerRunning;
    private CardInstance _placedCard;
    private CardInstance _opponentCard;

    private SkillType _assignedSkill;
    private bool _isSkillUsed;
    private int _playerNextTurnAtkBoost;
    private int _opponentNextTurnAtkBoost;

    private bool _waitingForResolve;
    private float _resolveTimer;

    private SkillType _botSkill;
    private bool _botUsedSkill;

    public int CurrentTurn { get; private set; }
    public int MaxTurns => _config.MaxTurns;
    public int PlayerHp { get; private set; }
    public int OpponentHp { get; private set; }
    public bool IsPlayerTurn => _timerRunning;
    public bool IsLocalConfirmed => !_timerRunning;
    public SkillType AssignedSkill => _assignedSkill;
    public bool IsSkillUsed => _isSkillUsed;

    [Inject]
    public void Initialize(TurnConfigSO config)
    {
        _config = config;
    }

    public void Start()
    {
        _eventBus.SubscribeTo<BotMatchStarted>(OnBotMatchStarted);
        Debug.Log("[LTS] LocalTurnSystem started, listening for BotMatchStarted");
    }

    private void OnBotMatchStarted(ref BotMatchStarted _)
    {
        _opponent.SelectDeck();
        _eventBus.Raise(new GameStateChanged { State = GameState.Gameplay });
        _gameplayManager.SetTurnSystem(this);
        StartGame();
    }

    public void StartGame()
    {
        PlayerHp = _config.StartingHp;
        OpponentHp = _config.StartingHp;
        CurrentTurn = 0;
        _playerNextTurnAtkBoost = 0;
        _opponentNextTurnAtkBoost = 0;
        _waitingForResolve = false;
        StartNextTurn();
    }

    private void StartNextTurn()
    {
        CurrentTurn++;
        _placedCard = null;
        _isSkillUsed = false;
        _assignedSkill = (SkillType)Random.Range(0, 6);
        _turnTimer = _config.TurnDuration;
        _timerRunning = true;

        _eventBus.Raise(new TurnStarted { TurnNumber = CurrentTurn });
        _eventBus.Raise(new SkillAssigned { Skill = _assignedSkill });

        // Bot places its card face-down after turn starts
        _opponentCard = _opponent.PlayCard();
        _eventBus.Raise(new OpponentCardChanged { Card = _opponentCard });

        // Bot randomly uses a skill with 50% chance
        _botSkill = (SkillType)Random.Range(0, 6);
        _botUsedSkill = Random.value < 0.5f;
    }

    public void PlaceCard(CardInstance card)
    {
        _placedCard = card;
    }

    public void RemoveCard()
    {
        _placedCard = null;
    }

    public void ConfirmTurn()
    {
        if (!_timerRunning) return;
        _timerRunning = false;

        _eventBus.Raise(new TurnEnded { TurnNumber = CurrentTurn });

        RunSimulation(_placedCard, _opponentCard);
    }

    public void UseSkill()
    {
        if (!_timerRunning || _isSkillUsed) return;
        _isSkillUsed = true;
        _eventBus.Raise(new SkillUsed());
    }

    public void Tick(float deltaTime)
    {
        if (_waitingForResolve)
        {
            _resolveTimer -= deltaTime;
            if (_resolveTimer <= 0f)
            {
                _waitingForResolve = false;
                CheckGameOver();
            }
            return;
        }

        if (!_timerRunning) return;

        _turnTimer -= deltaTime;
        _eventBus.Raise(new TurnTimerUpdated { RemainingTime = _turnTimer });

        if (_turnTimer <= 0f)
        {
            _turnTimer = 0f;
            ConfirmTurn();
        }
    }

    private void RunSimulation(CardInstance playerCard, CardInstance opponentCard)
    {
        // Apply previous turn's Shield carry-over
        if (playerCard != null && _playerNextTurnAtkBoost > 0)
            playerCard.CurrentAttack += _playerNextTurnAtkBoost;
        if (opponentCard != null && _opponentNextTurnAtkBoost > 0)
            opponentCard.CurrentAttack += _opponentNextTurnAtkBoost;
        _playerNextTurnAtkBoost = 0;
        _opponentNextTurnAtkBoost = 0;

        // Apply player skill stat modifications
        if (_isSkillUsed)
        {
            switch (_assignedSkill)
            {
                case SkillType.BoostAttack:
                    if (playerCard != null)
                        playerCard.CurrentAttack += _skillConfig.AttackBoost;
                    break;
                case SkillType.BoostDefense:
                    if (playerCard != null)
                        playerCard.CurrentDefense += _skillConfig.DefenseBoost;
                    break;
                case SkillType.ReduceOpponentAttack:
                    if (opponentCard != null)
                        opponentCard.CurrentAttack = Mathf.Max(0, opponentCard.CurrentAttack - _skillConfig.OpponentAttackReduction);
                    break;
                case SkillType.ReduceOpponentDefense:
                    if (opponentCard != null)
                        opponentCard.CurrentDefense = Mathf.Max(0, opponentCard.CurrentDefense - _skillConfig.OpponentDefenseReduction);
                    break;
            }
        }

        // Apply bot skill stat modifications
        if (_botUsedSkill)
        {
            switch (_botSkill)
            {
                case SkillType.BoostAttack:
                    if (opponentCard != null)
                        opponentCard.CurrentAttack += _skillConfig.AttackBoost;
                    break;
                case SkillType.BoostDefense:
                    if (opponentCard != null)
                        opponentCard.CurrentDefense += _skillConfig.DefenseBoost;
                    break;
                case SkillType.ReduceOpponentAttack:
                    if (playerCard != null)
                        playerCard.CurrentAttack = Mathf.Max(0, playerCard.CurrentAttack - _skillConfig.OpponentAttackReduction);
                    break;
                case SkillType.ReduceOpponentDefense:
                    if (playerCard != null)
                        playerCard.CurrentDefense = Mathf.Max(0, playerCard.CurrentDefense - _skillConfig.OpponentDefenseReduction);
                    break;
            }
        }

        // Calculate damage
        int playerDamage = 0;
        int opponentDamage = 0;

        if (playerCard != null && opponentCard != null)
        {
            int atkVsDef = playerCard.CurrentAttack - opponentCard.CurrentDefense;
            if (atkVsDef > 0)
                opponentDamage = atkVsDef;

            int oppAtkVsDef = opponentCard.CurrentAttack - playerCard.CurrentDefense;
            if (oppAtkVsDef > 0)
                playerDamage = oppAtkVsDef;
        }
        else if (playerCard == null && opponentCard != null)
        {
            playerDamage = opponentCard.CurrentAttack;
        }
        else if (playerCard != null && opponentCard == null)
        {
            opponentDamage = playerCard.CurrentAttack;
        }

        // Apply Shield absorb
        if (_isSkillUsed && _assignedSkill == SkillType.Shield)
        {
            playerDamage = Mathf.Max(0, playerDamage - _skillConfig.ShieldAbsorb);
            _opponentNextTurnAtkBoost = _skillConfig.ShieldOpponentAttackBoost;
        }

        // Apply bot Shield absorb
        if (_botUsedSkill && _botSkill == SkillType.Shield)
        {
            opponentDamage = Mathf.Max(0, opponentDamage - _skillConfig.ShieldAbsorb);
            _playerNextTurnAtkBoost = _skillConfig.ShieldOpponentAttackBoost;
        }

        // Apply HealPlayer
        if (_isSkillUsed && _assignedSkill == SkillType.HealPlayer)
            PlayerHp = Mathf.Min(_config.StartingHp, PlayerHp + _skillConfig.HealAmount);

        // Apply bot HealPlayer
        if (_botUsedSkill && _botSkill == SkillType.HealPlayer)
            OpponentHp = Mathf.Min(_config.StartingHp, OpponentHp + _skillConfig.HealAmount);

        PlayerHp = Mathf.Max(0, PlayerHp - playerDamage);
        OpponentHp = Mathf.Max(0, OpponentHp - opponentDamage);

        _eventBus.Raise(new SimulationResult
        {
            PlayerCard = playerCard,
            OpponentCard = opponentCard,
            PlayerDamage = playerDamage,
            OpponentDamage = opponentDamage,
            PlayerHp = PlayerHp,
            OpponentHp = OpponentHp
        });

        // Wait for resolve duration before checking game over / starting next turn
        _waitingForResolve = true;
        _resolveTimer = _config.ResolveDuration;
    }

    private void CheckGameOver()
    {
        if (PlayerHp <= 0 || OpponentHp <= 0 || CurrentTurn >= MaxTurns)
        {
            bool isDraw = PlayerHp == OpponentHp;
            bool playerWon = PlayerHp > OpponentHp;
            _eventBus.Raise(new GameOver { PlayerWon = playerWon, IsDraw = isDraw });
        }
        else
        {
            StartNextTurn();
        }
    }
}
