using DG.Tweening;
using UnityEngine;
using VContainer;

public class GameplayManager : MonoBehaviour
{
    [Inject] private GameEventBus _eventBus;
    [Inject] private HandLayoutManager _handLayout;

    [Header("Table")]
    [SerializeField] private Transform _playerCardLocation;
    [SerializeField] private float _snapDistance = 2f;

    [Header("Animation")]
    [SerializeField] private float _snapDuration = 0.25f;

    private CardUnit _playedCard;

    private void OnEnable()
    {
        _eventBus.SubscribeTo<GameStateChanged>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        _eventBus.UnsubscribeFrom<GameStateChanged>(OnGameStateChanged);
        UnsubscribeFromCards();
    }

    private void OnGameStateChanged(ref GameStateChanged e)
    {
        if (e.State == GameState.Gameplay)
        {
            // HandLayoutManager.DealHand runs first via BotMatchStarted/MatchFound,
            // GameStateChanged is raised after, so HandCards is ready here.
            SubscribeToCards();
        }
        else
        {
            UnsubscribeFromCards();
        }
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

        card.transform.DOKill();
        card.transform.DOMove(_playerCardLocation.position, _snapDuration);
        card.transform.DORotateQuaternion(_playerCardLocation.rotation, _snapDuration);
    }

    private void ReturnCardToHand(CardUnit card)
    {
        if (_playedCard == card)
        {
            _playedCard = null;
            _handLayout.AddCard(card);
        }
        else
        {
            _handLayout.ReturnCardToSlot(card);
        }
    }
}
