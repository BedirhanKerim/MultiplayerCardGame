using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button _quickMatchButton;
    [SerializeField] private Button _playerVsBotButton;
    [SerializeField] private GameObject _waitingForOpponentPanel;
    [SerializeField] private GameObject _deckBuilderPanel;

    [Inject] private INetworkManager _networkManager;
    [Inject] private GameEventBus _eventBus;
    [Inject] private DeckBuilderManager _deckBuilderManager;

    private void Awake()
    {
        _waitingForOpponentPanel.SetActive(false);
        _quickMatchButton.interactable = false;
        _playerVsBotButton.interactable = false;
    }

    private void OnEnable()
    {
        _quickMatchButton.onClick.AddListener(OnQuickMatchClicked);
        _playerVsBotButton.onClick.AddListener(OnPlayerVsBotClicked);
        _deckBuilderManager.OnSelectionChanged += OnDeckSelectionChanged;
        _eventBus.SubscribeTo<MatchSearchStarted>(OnMatchSearchStarted);
        _eventBus.SubscribeTo<MatchFound>(OnMatchFound);
        _eventBus.SubscribeTo<MatchFailed>(OnMatchFailed);
    }

    private void OnDisable()
    {
        _quickMatchButton.onClick.RemoveListener(OnQuickMatchClicked);
        _playerVsBotButton.onClick.RemoveListener(OnPlayerVsBotClicked);
        _deckBuilderManager.OnSelectionChanged -= OnDeckSelectionChanged;
        _eventBus.UnsubscribeFrom<MatchSearchStarted>(OnMatchSearchStarted);
        _eventBus.UnsubscribeFrom<MatchFound>(OnMatchFound);
        _eventBus.UnsubscribeFrom<MatchFailed>(OnMatchFailed);
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
        HideDeckBuilderPanel();
    }

    private void OnPlayerVsBotClicked()
    {
        _eventBus.Raise(new BotMatchStarted());
        _eventBus.Raise(new GameStateChanged { State = GameState.Gameplay });
        SetButtonsInteractable(false);
        HideDeckBuilderPanel();
    }

    private void HideDeckBuilderPanel()
    {
        _deckBuilderPanel.SetActive(false);
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
