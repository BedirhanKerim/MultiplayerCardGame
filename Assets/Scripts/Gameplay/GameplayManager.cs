using DG.Tweening;
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

    private ITurnSystem _turnSystem;
    private CardUnit _playedCard;
    private CardUnit _opponentCardUnit;
    private bool _isActive;

    public void SetTurnSystem(ITurnSystem turnSystem)
    {
        _turnSystem = turnSystem;
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
            _opponentCardUnit = Instantiate(_deckBuilderManager.CardPrefab);
            _opponentCardUnit.Interactable = false;
        }
        _opponentCardUnit.gameObject.SetActive(false);
    }

    private void ShowOpponentCard(CardInstance card)
    {
        _opponentCardUnit.CardView.Setup(card);
        _opponentCardUnit.gameObject.SetActive(true);
        _opponentCardUnit.transform.position = _opponentCardLocation.position;
        _opponentCardUnit.transform.rotation = _opponentCardLocation.rotation;
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
        card.transform.DORotateQuaternion(_playerCardLocation.rotation, _snapDuration);
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
