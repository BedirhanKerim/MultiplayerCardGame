using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class NetworkManager : MonoBehaviour, INetworkManager, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner _runnerPrefab;

    [Inject] private GameEventBus _eventBus;
    [Inject] private NetworkTurnSystem _turnSystem;

    private NetworkRunner _runner;

    public async void StartQuickMatch()
    {
        _eventBus.Raise(new MatchSearchStarted());

        _runner = Instantiate(_runnerPrefab);
        _runner.AddCallbacks(this);

        var sceneInfo = new NetworkSceneInfo();
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0) {
            sceneInfo.AddSceneRef(SceneRef.FromIndex(activeScene.buildIndex));
        }

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "QuickMatch",
            PlayerCount = 2,
            Scene = sceneInfo,
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>(),
        });

        if (!result.Ok)
        {
            Debug.LogError($"Failed to start game: {result.ErrorMessage}");
            CleanupRunner();
            _eventBus.Raise(new MatchFailed());
        }
    }

    private void CleanupRunner()
    {
        if (_runner != null)
        {
            Destroy(_runner.gameObject);
            _runner = null;
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player joined: {player}");

        if (runner.ActivePlayers.Count() >= 2)
        {
            _eventBus.Raise(new MatchFound());
            _eventBus.Raise(new GameStateChanged { State = GameState.Gameplay });

            if (runner.IsServer)
                _turnSystem.StartGame();
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player left: {player}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner shutdown: {shutdownReason}");
        CleanupRunner();
        _eventBus.Raise(new MatchFailed());
    }

    // Unused callbacks
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}
