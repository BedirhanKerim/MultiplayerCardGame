using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class BotOpponent : IOpponent
{
    [Inject] private DeckBuilderManager _deckBuilderManager;

    private readonly List<CardInstance> _deck = new();
    private readonly List<CardInstance> _hand = new();

    private const int DeckSize = 6;

    public IReadOnlyList<CardInstance> Deck => _deck;

    public void SelectDeck()
    {
        _deck.Clear();
        _hand.Clear();

        var allCards = new List<CardDataEntry>(_deckBuilderManager.CardDatabase.Cards);

        for (int i = allCards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allCards[i], allCards[j]) = (allCards[j], allCards[i]);
        }

        for (int i = 0; i < DeckSize && i < allCards.Count; i++)
        {
            var card = new CardInstance(allCards[i]);
            _deck.Add(card);
            _hand.Add(card);
        }
    }

    public CardInstance PlayCard()
    {
        if (_hand.Count == 0) return null;

        int index = Random.Range(0, _hand.Count);
        var card = _hand[index];
        _hand.RemoveAt(index);
        return card;
    }
}
