public interface ITurnSystem
{
    int CurrentTurn { get; }
    int MaxTurns { get; }
    int PlayerHp { get; }
    int OpponentHp { get; }
    bool IsPlayerTurn { get; }
    bool IsLocalConfirmed { get; }
    void StartGame();
    void PlaceCard(CardInstance card);
    void RemoveCard();
    void ConfirmTurn();
    void Tick(float deltaTime);
}
