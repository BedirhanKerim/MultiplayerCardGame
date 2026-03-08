using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VContainer;

public class HandLayoutManager
{
    [Inject] private GameEventBus _eventBus;
    [Inject] private DeckBuilderManager _deckBuilderManager;

    private Vector3 _handCenter = new(0f, 0.1f, -1.9f);
    private float _horizontalSpacing = 0.63f;
    private float _arcHeight = 0.28f;
    private float _depthSpacing = 0.15f;
    private float _cardTiltAngle = 20f;
    private int _baseSortingOrder = 10;
    private float _animDuration = 0.4f;
    private Ease _animEase = Ease.OutBack;

    private readonly List<CardUnit> _handCards = new();

    public IReadOnlyList<CardUnit> HandCards => _handCards;

    [Inject]
    public void Initialize()
    {
        _eventBus.SubscribeTo<BotMatchStarted>(OnGameStarted);
        _eventBus.SubscribeTo<MatchFound>(OnMatchStarted);
    }

    private void OnGameStarted(ref BotMatchStarted _) => DealHand();
    private void OnMatchStarted(ref MatchFound _) => DealHand();

    private void DealHand()
    {
        _handCards.Clear();

        foreach (var card in _deckBuilderManager.SelectedCards)
        {
            _handCards.Add(card);
            card.gameObject.SetActive(true);
        }

        ArrangeCards();
    }

    public void RemoveCard(CardUnit card)
    {
        if (!_handCards.Remove(card)) return;
        ArrangeCards();
    }

    public void AddCard(CardUnit card)
    {
        if (_handCards.Contains(card)) return;
        _handCards.Add(card);
        ArrangeCards();
    }

    public void ReturnCardToSlot(CardUnit card)
    {
        int index = _handCards.IndexOf(card);
        if (index < 0) return;

        card.Canvas.sortingOrder = _baseSortingOrder + index;
        CalculateCardTransform(index, _handCards.Count, out Vector3 pos, out Quaternion rot);

        card.transform.DOKill();
        card.transform.DOMove(pos, _animDuration).SetEase(_animEase);
        card.transform.DORotateQuaternion(rot, _animDuration).SetEase(_animEase);
    }

    private void ArrangeCards()
    {
        int count = _handCards.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            CalculateCardTransform(i, count, out Vector3 pos, out Quaternion rot);

            CardUnit card = _handCards[i];
            card.Canvas.sortingOrder = _baseSortingOrder + i;
            card.transform.DOKill();
            card.transform.DOMove(pos, _animDuration).SetEase(_animEase);
            card.transform.DORotateQuaternion(rot, _animDuration).SetEase(_animEase);
        }
    }

    private void CalculateCardTransform(int index, int count, out Vector3 position, out Quaternion rotation)
    {
        // t: -1 (en sol) to +1 (en sag), 1 kart varsa 0 (orta)
        float t = count > 1 ? (2f * index / (count - 1)) - 1f : 0f;

        float x = _handCenter.x + t * (count - 1) * _horizontalSpacing * 0.5f;

        // y: soldan saga artar (sag kartlar sol kartlarin ustunde gorunur)
        float normalizedIndex = count > 1 ? (float)index / (count - 1) : 0f;
        float y = _handCenter.y + normalizedIndex * _depthSpacing;

        // z: bombe - ortadaki kartlar kameraya yakin, kenardakiler uzak
        float z = _handCenter.z + (1f - t * t) * _arcHeight;

        position = new Vector3(x, y, z);

        // rotY: fan efekti - sol kartlar negatif, sag kartlar pozitif
        float yTilt = t * _cardTiltAngle;
        rotation = Quaternion.Euler(90f, yTilt, 0f);
    }
}
