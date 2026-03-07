using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private NetworkManager _networkManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameEventBus>(Lifetime.Singleton);
        builder.RegisterComponent(_uiManager);
        builder.RegisterComponent(_networkManager).As<INetworkManager>();
    }
}
