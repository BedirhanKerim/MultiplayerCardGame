using System.Collections.Generic;

public interface IOpponent
{
    IReadOnlyList<CardInstance> Deck { get; }
    void SelectDeck();
    CardInstance PlayCard();
}
