using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class DeckBuilderManager : MonoBehaviour
{
    [Inject] private GameEventBus _eventBus;

    [Header("References")]
    [SerializeField] private GameObject _deckBuilderRoot;
    [SerializeField] private CardUnit _cardPrefab;
    [SerializeField] private CardDatabaseSO _cardDatabase;

    [Header("Slots")]
    [SerializeField] private Transform[] _inventorySlots;
    [SerializeField] private Transform[] _deckSlots;

    private const int MaxDeckSize = 6;

    private readonly List<CardUnit> _spawnedCards = new();
    private readonly List<CardUnit> _selectedCards = new();
    private readonly Dictionary<CardUnit, Transform> _originalSlots = new();
    private readonly Dictionary<CardUnit, int> _cardDeckSlotIndex = new();

    public IReadOnlyList<CardUnit> SelectedCards => _selectedCards;
    public bool IsDeckFull => _selectedCards.Count == MaxDeckSize;
    public event Action OnSelectionChanged;

    public void Show() => _deckBuilderRoot.SetActive(true);
    public void Hide() => _deckBuilderRoot.SetActive(false);

    private void OnEnable()
    {
        _eventBus.SubscribeTo<MatchSearchStarted>(OnMatchSearchStarted);
        _eventBus.SubscribeTo<MatchFailed>(OnMatchFailed);
    }

    private void OnDisable()
    {
        _eventBus.UnsubscribeFrom<MatchSearchStarted>(OnMatchSearchStarted);
        _eventBus.UnsubscribeFrom<MatchFailed>(OnMatchFailed);
    }

    private void Start()
    {
        SpawnAllCards();
    }

    private void OnMatchSearchStarted(ref MatchSearchStarted _) => Hide();
    private void OnMatchFailed(ref MatchFailed _) => Show();

    private void SpawnAllCards()
    {
        var cards = _cardDatabase.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            var cardUnit = Instantiate(_cardPrefab);
            cardUnit.CardView.Setup(new CardInstance(cards[i]));

            if (i < _inventorySlots.Length)
                cardUnit.transform.position = _inventorySlots[i].position;

            cardUnit.OnClicked += OnCardClicked;
            _originalSlots[cardUnit] = _inventorySlots[i];
            _spawnedCards.Add(cardUnit);
        }
    }

    private void OnCardClicked(CardUnit card)
    {
        if (_selectedCards.Contains(card))
        {
            RemoveFromDeck(card);
        }
        else if (_selectedCards.Count < MaxDeckSize)
        {
            AddToDeck(card);
        }

        OnSelectionChanged?.Invoke();
    }

    private void AddToDeck(CardUnit card)
    {
        for (int i = 0; i < _deckSlots.Length; i++)
        {
            if (!_cardDeckSlotIndex.ContainsValue(i))
            {
                MoveCardTo(card, _deckSlots[i]);
                _selectedCards.Add(card);
                _cardDeckSlotIndex[card] = i;
                return;
            }
        }
    }

    private void RemoveFromDeck(CardUnit card)
    {
        if (_originalSlots.TryGetValue(card, out var originalSlot))
        {
            MoveCardTo(card, originalSlot);
        }
        _cardDeckSlotIndex.Remove(card);
        _selectedCards.Remove(card);
    }

    private void MoveCardTo(CardUnit card, Transform slot)
    {
        card.transform.position = slot.position;
    }

    private void OnDestroy()
    {
        foreach (var card in _spawnedCards)
        {
            card.OnClicked -= OnCardClicked;
        }
    }
}
