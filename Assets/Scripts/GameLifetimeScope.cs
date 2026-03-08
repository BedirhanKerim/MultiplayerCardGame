using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private DeckBuilderManager _deckBuilderManager;
    [SerializeField] private GameplayManager _gameplayManager;
    [SerializeField] private InputRaycaster _inputRaycaster;
    [SerializeField] private NetworkTurnSystem _networkTurnSystem;
    [SerializeField] private TurnConfigSO _turnConfig;
    [SerializeField] private SkillConfigSO _skillConfig;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameEventBus>(Lifetime.Singleton);
        builder.RegisterComponent(_uiManager);
        builder.RegisterComponent(_networkManager).As<INetworkManager>();
        builder.RegisterComponent(_deckBuilderManager);
        builder.Register<HandLayoutManager>(Lifetime.Singleton);
        builder.RegisterComponent(_gameplayManager);
        builder.RegisterComponent(_inputRaycaster);
        builder.RegisterComponent(_networkTurnSystem);
        builder.Register<BotOpponent>(Lifetime.Singleton).As<IOpponent>();
        builder.RegisterInstance(_turnConfig);
        builder.RegisterInstance(_skillConfig);
    }
}
