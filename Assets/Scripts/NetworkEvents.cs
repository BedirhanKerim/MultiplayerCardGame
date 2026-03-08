using GenericEventBus;

public enum GameState { DeckBuilder, Gameplay }

public struct GameStateChanged { public GameState State; }
public struct MatchSearchStarted { }
public struct MatchFound { }
public struct MatchFailed { }
public struct BotMatchStarted { }

public class GameEventBus : GenericEventBus<object> { }
