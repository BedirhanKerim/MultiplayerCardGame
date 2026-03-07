using GenericEventBus;

public struct MatchSearchStarted { }
public struct MatchFound { }
public struct MatchFailed { }

public class GameEventBus : GenericEventBus<object> { }
