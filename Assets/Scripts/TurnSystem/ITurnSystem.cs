public interface ITurnSystem
{
    int CurrentTurn { get; }
    int MaxTurns { get; }
    int PlayerHp { get; }
    int OpponentHp { get; }
    bool IsPlayerTurn { get; }
    bool IsLocalConfirmed { get; }
    SkillType AssignedSkill { get; }
    bool IsSkillUsed { get; }
    void StartGame();
    void PlaceCard(CardInstance card);
    void RemoveCard();
    void ConfirmTurn();
    void UseSkill();
    void Tick(float deltaTime);
}
