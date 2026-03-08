using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class UIManager : MonoBehaviour
{
    [Header("Deck Builder")]
    [SerializeField] private GameObject _deckBuilderPanel;
    [SerializeField] private Button _quickMatchButton;
    [SerializeField] private Button _playerVsBotButton;
    [SerializeField] private GameObject _waitingForOpponentPanel;

    [Header("Gameplay")]
    [SerializeField] private GameObject _gameplayPanel;
    [SerializeField] private Button _confirmTurnButton;
    [SerializeField] private TextMeshProUGUI _playerHpText;
    [SerializeField] private TextMeshProUGUI _opponentHpText;
    [SerializeField] private TextMeshProUGUI _turnText;
    [SerializeField] private TextMeshProUGUI _timerText;

    [Inject] private INetworkManager _networkManager;
    [Inject] private GameEventBus _eventBus;
    [Inject] private DeckBuilderManager _deckBuilderManager;

    private void Awake()
    {
        _deckBuilderPanel.SetActive(true);
        _waitingForOpponentPanel.SetActive(false);
        _gameplayPanel.SetActive(false);
        _quickMatchButton.interactable = false;
        _playerVsBotButton.interactable = false;
    }

    private void OnEnable()
    {
        _quickMatchButton.onClick.AddListener(OnQuickMatchClicked);
        _playerVsBotButton.onClick.AddListener(OnPlayerVsBotClicked);
        _confirmTurnButton.onClick.AddListener(OnConfirmTurnClicked);
        _deckBuilderManager.OnSelectionChanged += OnDeckSelectionChanged;
        _eventBus.SubscribeTo<MatchSearchStarted>(OnMatchSearchStarted);
        _eventBus.SubscribeTo<MatchFound>(OnMatchFound);
        _eventBus.SubscribeTo<MatchFailed>(OnMatchFailed);
        _eventBus.SubscribeTo<TurnStarted>(OnTurnStarted);
        _eventBus.SubscribeTo<TurnTimerUpdated>(OnTurnTimerUpdated);
        _eventBus.SubscribeTo<SimulationResult>(OnSimulationResult);
        _eventBus.SubscribeTo<GameOver>(OnGameOver);
        _eventBus.SubscribeTo<LocalPlayerConfirmed>(OnLocalPlayerConfirmed);
    }

    private void OnDisable()
    {
        _quickMatchButton.onClick.RemoveListener(OnQuickMatchClicked);
        _playerVsBotButton.onClick.RemoveListener(OnPlayerVsBotClicked);
        _confirmTurnButton.onClick.RemoveListener(OnConfirmTurnClicked);
        _deckBuilderManager.OnSelectionChanged -= OnDeckSelectionChanged;
        _eventBus.UnsubscribeFrom<MatchSearchStarted>(OnMatchSearchStarted);
        _eventBus.UnsubscribeFrom<MatchFound>(OnMatchFound);
        _eventBus.UnsubscribeFrom<MatchFailed>(OnMatchFailed);
        _eventBus.UnsubscribeFrom<TurnStarted>(OnTurnStarted);
        _eventBus.UnsubscribeFrom<TurnTimerUpdated>(OnTurnTimerUpdated);
        _eventBus.UnsubscribeFrom<SimulationResult>(OnSimulationResult);
        _eventBus.UnsubscribeFrom<GameOver>(OnGameOver);
        _eventBus.UnsubscribeFrom<LocalPlayerConfirmed>(OnLocalPlayerConfirmed);
    }

    private void OnDeckSelectionChanged()
    {
        bool deckFull = _deckBuilderManager.IsDeckFull;
        _quickMatchButton.interactable = deckFull;
        _playerVsBotButton.interactable = deckFull;
    }

    private void OnQuickMatchClicked()
    {
        _networkManager.StartQuickMatch();
        ShowGameplayPanel();
    }

    private void OnPlayerVsBotClicked()
    {
        _eventBus.Raise(new BotMatchStarted());
        _eventBus.Raise(new GameStateChanged { State = GameState.Gameplay });
        SetButtonsInteractable(false);
        ShowGameplayPanel();
    }

    private void OnConfirmTurnClicked()
    {
        _eventBus.Raise(new TurnConfirmRequested());
    }

    private void OnTurnStarted(ref TurnStarted e)
    {
        _turnText.text = $"Turn {e.TurnNumber}";
        _confirmTurnButton.interactable = true;
    }

    private void OnTurnTimerUpdated(ref TurnTimerUpdated e)
    {
        int seconds = Mathf.CeilToInt(e.RemainingTime);
        _timerText.text = seconds.ToString();
    }

    private void OnSimulationResult(ref SimulationResult e)
    {
        _playerHpText.text = e.PlayerHp.ToString();
        _opponentHpText.text = e.OpponentHp.ToString();
        _confirmTurnButton.interactable = false;
    }

    private void OnGameOver(ref GameOver e)
    {
        _confirmTurnButton.interactable = false;
        if (e.IsDraw)
            _turnText.text = "Draw!";
        else if (e.PlayerWon)
            _turnText.text = "You Win!";
        else
            _turnText.text = "You Lose!";
    }

    private void OnLocalPlayerConfirmed(ref LocalPlayerConfirmed _)
    {
        _confirmTurnButton.interactable = false;
    }

    private void ShowGameplayPanel()
    {
        _deckBuilderPanel.SetActive(false);
        _gameplayPanel.SetActive(true);
    }

    private void OnMatchSearchStarted(ref MatchSearchStarted _)
    {
        _waitingForOpponentPanel.SetActive(true);
        SetButtonsInteractable(false);
    }

    private void OnMatchFound(ref MatchFound _)
    {
        _waitingForOpponentPanel.SetActive(false);
        _eventBus.Raise(new GameStateChanged { State = GameState.Gameplay });
    }

    private void OnMatchFailed(ref MatchFailed _)
    {
        _waitingForOpponentPanel.SetActive(false);
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        _quickMatchButton.interactable = interactable;
        _playerVsBotButton.interactable = interactable;
    }
}
