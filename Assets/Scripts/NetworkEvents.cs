using GenericEventBus;

public enum GameState { DeckBuilder, Gameplay }

public struct GameStateChanged { public GameState State; }
public struct MatchSearchStarted { }
public struct MatchFound { }
public struct MatchFailed { }
public struct BotMatchStarted { }

public struct TurnStarted { public int TurnNumber; }
public struct TurnTimerUpdated { public float RemainingTime; }
public struct TurnConfirmRequested { }
public struct TurnEnded { public int TurnNumber; }
public struct SimulationResult
{
    public CardInstance PlayerCard;
    public CardInstance OpponentCard;
    public int PlayerDamage;
    public int OpponentDamage;
    public int PlayerHp;
    public int OpponentHp;
}
public struct GameOver { public bool PlayerWon; public bool IsDraw; }
public struct OpponentCardChanged { public CardInstance Card; }
public struct LocalPlayerConfirmed { }
public struct SkillAssigned { public SkillType Skill; }
public struct SkillUseRequested { }
public struct SkillUsed { }
public struct HpDamageApplied { public bool IsPlayer; public int NewHp; }

public class GameEventBus : GenericEventBus<object> { }
