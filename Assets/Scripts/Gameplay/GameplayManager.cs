using DG.Tweening;
using Lean.Pool;
using UnityEngine;
using VContainer;

public class GameplayManager : MonoBehaviour
{
    [Inject] private GameEventBus _eventBus;
    [Inject] private HandLayoutManager _handLayout;
    [Inject] private DeckBuilderManager _deckBuilderManager;
    [Inject] private SkillConfigSO _skillConfig;

    [Header("Table")]
    [SerializeField] private Transform _playerCardLocation;
    [SerializeField] private Transform _opponentCardLocation;
    [SerializeField] private float _snapDistance = 2f;

    [Header("Animation")]
    [SerializeField] private float _snapDuration = 0.25f;
    [SerializeField] private float _flipDuration = 0.4f;

    [Header("Damage Projectile")]
    [SerializeField] private DamageProjectile _damageProjectilePrefab;
    [SerializeField] private Transform _playerHpLocation;
    [SerializeField] private Transform _opponentHpLocation;

    private ITurnSystem _turnSystem;
    private Quaternion _faceDownRotation;
    private Quaternion _faceUpRotation;
    private CardUnit _playedCard;
    private CardUnit _opponentCardUnit;
    private bool _isActive;

    public void SetTurnSystem(ITurnSystem turnSystem)
    {
        _turnSystem = turnSystem;
    }

    private void Awake()
    {
        _faceDownRotation = Quaternion.Euler(270f, 270f, 270f);
        _faceUpRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void OnEnable()
    {
        _eventBus.SubscribeTo<GameStateChanged>(OnGameStateChanged);
        _eventBus.SubscribeTo<TurnStarted>(OnTurnStarted);
        _eventBus.SubscribeTo<TurnEnded>(OnTurnEnded);
        _eventBus.SubscribeTo<SimulationResult>(OnSimulationResult);
        _eventBus.SubscribeTo<TurnConfirmRequested>(OnTurnConfirmRequested);
        _eventBus.SubscribeTo<SkillUseRequested>(OnSkillUseRequested);
        _eventBus.SubscribeTo<OpponentCardChanged>(OnOpponentCardChanged);
        _eventBus.SubscribeTo<TurnTimerUpdated>(OnTurnTimerUpdated);
    }

    private void OnDisable()
    {
        _eventBus.UnsubscribeFrom<GameStateChanged>(OnGameStateChanged);
        _eventBus.UnsubscribeFrom<TurnStarted>(OnTurnStarted);
        _eventBus.UnsubscribeFrom<TurnEnded>(OnTurnEnded);
        _eventBus.UnsubscribeFrom<SimulationResult>(OnSimulationResult);
        _eventBus.UnsubscribeFrom<TurnConfirmRequested>(OnTurnConfirmRequested);
        _eventBus.UnsubscribeFrom<SkillUseRequested>(OnSkillUseRequested);
        _eventBus.UnsubscribeFrom<OpponentCardChanged>(OnOpponentCardChanged);
        _eventBus.UnsubscribeFrom<TurnTimerUpdated>(OnTurnTimerUpdated);
        UnsubscribeFromCards();
    }

    private void Update()
    {
        if (_isActive && _turnSystem != null)
            _turnSystem.Tick(Time.deltaTime);
    }

    private void OnGameStateChanged(ref GameStateChanged e)
    {
        if (e.State == GameState.Gameplay)
        {
            _isActive = true;
            SpawnOpponentCard();
            SubscribeToCards();
        }
        else
        {
            _isActive = false;
            UnsubscribeFromCards();
        }
    }

    private void OnTurnStarted(ref TurnStarted e)
    {
        HideOpponentCard();
        if (_playedCard != null)
        {
            _playedCard.gameObject.SetActive(false);
            _playedCard = null;
        }
    }

    private void OnTurnEnded(ref TurnEnded e)
    {
    }

    private void OnSimulationResult(ref SimulationResult e)
    {
        Debug.Log($"[GM] OnSimulationResult: OppCard={e.OpponentCard?.Data.CardId}, PlayerHp={e.PlayerHp}, OppHp={e.OpponentHp}");

        if (_playedCard != null && e.PlayerCard != null)
        {
            _playedCard.CardView.Setup(e.PlayerCard);
        }
        if (_opponentCardUnit != null && e.OpponentCard != null)
        {
            _opponentCardUnit.CardView.Setup(e.OpponentCard);
            _opponentCardUnit.transform.DOKill();
            _opponentCardUnit.transform.DORotateQuaternion(_faceUpRotation, _flipDuration).SetEase(Ease.OutBack);
        }

        bool canShootPlayer = _playedCard != null && _opponentCardUnit != null;

        // Player's card attacks opponent
        if (canShootPlayer && _opponentHpLocation != null)
        {
            int oppHp = e.OpponentHp;
            int playerAtk = e.PlayerCard != null ? e.PlayerCard.CurrentAttack : 0;
            bool pierces = e.OpponentDamage > 0;
            SpawnDamageProjectile(playerAtk, e.OpponentDamage,
                _playedCard.CardView.AttackTransform.position,
                _opponentCardUnit.CardView.DefenseTransform.position,
                pierces ? _opponentHpLocation.position : Vector3.zero,
                pierces,
                pierces ? () => _eventBus.Raise(new HpDamageApplied { IsPlayer = false, NewHp = oppHp }) : null);
        }

        // Opponent's card attacks player
        if (canShootPlayer && _playerHpLocation != null)
        {
            int playerHp = e.PlayerHp;
            int oppAtk = e.OpponentCard != null ? e.OpponentCard.CurrentAttack : 0;
            bool pierces = e.PlayerDamage > 0;
            SpawnDamageProjectile(oppAtk, e.PlayerDamage,
                _opponentCardUnit.CardView.AttackTransform.position,
                _playedCard.CardView.DefenseTransform.position,
                pierces ? _playerHpLocation.position : Vector3.zero,
                pierces,
                pierces ? () => _eventBus.Raise(new HpDamageApplied { IsPlayer = true, NewHp = playerHp }) : null);
        }
    }

    private void OnOpponentCardChanged(ref OpponentCardChanged e)
    {
        if (e.Card != null)
            ShowOpponentCard(e.Card);
        else
            HideOpponentCard();
    }

    private void SpawnOpponentCard()
    {
        if (_opponentCardUnit == null)
        {
            _opponentCardUnit = LeanPool.Spawn(_deckBuilderManager.CardPrefab);
            _opponentCardUnit.Interactable = false;
        }
        _opponentCardUnit.gameObject.SetActive(false);
    }

    private void ShowOpponentCard(CardInstance card)
    {
        _opponentCardUnit.CardView.Setup(card);
        _opponentCardUnit.gameObject.SetActive(true);
        _opponentCardUnit.transform.position = _opponentCardLocation.position;
        _opponentCardUnit.transform.rotation = _faceDownRotation;
    }

    private void HideOpponentCard()
    {
        if (_opponentCardUnit != null)
            _opponentCardUnit.gameObject.SetActive(false);
    }

    private void OnTurnTimerUpdated(ref TurnTimerUpdated e)
    {
        if (e.RemainingTime <= 1f && !_turnSystem.IsLocalConfirmed)
        {
            if (_playedCard == null && _handLayout.HandCards.Count > 0)
            {
                var cards = _handLayout.HandCards;
                var randomCard = cards[Random.Range(0, cards.Count)];
                PlayCard(randomCard);
            }
            _turnSystem.ConfirmTurn();
        }
    }

    private void OnTurnConfirmRequested(ref TurnConfirmRequested _)
    {
        if (_playedCard == null) return;
        _turnSystem?.ConfirmTurn();
    }

    private void OnSkillUseRequested(ref SkillUseRequested _)
    {
        _turnSystem?.UseSkill();
    }



    private void SubscribeToCards()
    {
        foreach (var card in _handLayout.HandCards)
            card.OnDropped += OnCardDropped;
    }

    private void UnsubscribeFromCards()
    {
        foreach (var card in _handLayout.HandCards)
            card.OnDropped -= OnCardDropped;
    }

    private void OnCardDropped(CardUnit card)
    {
        float distance = Vector3.Distance(card.transform.position, _playerCardLocation.position);

        if (distance <= _snapDistance)
            PlayCard(card);
        else
            ReturnCardToHand(card);
    }

    private void PlayCard(CardUnit card)
    {
        if (_playedCard != null && _playedCard != card)
            ReturnCardToHand(_playedCard);

        _playedCard = card;
        _handLayout.RemoveCard(card);
        _turnSystem?.PlaceCard(card.CardInstance);

        card.transform.DOKill();
        card.transform.DOMove(_playerCardLocation.position, _snapDuration);
        card.transform.DORotateQuaternion(_faceUpRotation, _snapDuration);
    }

    private void SpawnDamageProjectile(int attack, int damage, Vector3 attackPos, Vector3 defensePos, Vector3 hpPos, bool pierces, System.Action onHit = null)
    {
        if (_damageProjectilePrefab == null) return;
        var projectile = LeanPool.Spawn(_damageProjectilePrefab, attackPos, Quaternion.identity);
        projectile.Play(attack, damage, attackPos, defensePos, hpPos, pierces, onHit);
    }

    private void ReturnCardToHand(CardUnit card)
    {
        if (_playedCard == card)
        {
            _playedCard = null;
            _turnSystem?.RemoveCard();
            _handLayout.AddCard(card);
        }
        else
        {
            _handLayout.ReturnCardToSlot(card);
        }
    }
}
