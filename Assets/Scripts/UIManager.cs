using DG.Tweening;
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
    [SerializeField] private GameObject _gameplayWorldCanvas;
    [SerializeField] private Button _confirmTurnButton;
    [SerializeField] private TextMeshProUGUI _playerHpText;
    [SerializeField] private TextMeshProUGUI _opponentHpText;
    [SerializeField] private TextMeshProUGUI _turnText;
    [SerializeField] private TextMeshProUGUI _timerText;

    [Header("Skill")]
    [SerializeField] private TextMeshProUGUI _skillDescriptionText;
    [SerializeField] private Button _useSkillButton;

    [Header("End Game")]
    [SerializeField] private GameObject _endGamePanel;
    [SerializeField] private TextMeshProUGUI _endGameResultText;
    [SerializeField] private Button _mainMenuButton;

    [Inject] private INetworkManager _networkManager;
    [Inject] private GameEventBus _eventBus;
    [Inject] private DeckBuilderManager _deckBuilderManager;
    [Inject] private SkillConfigSO _skillConfig;

    private void Awake()
    {
        _deckBuilderPanel.SetActive(true);
        _waitingForOpponentPanel.SetActive(false);
        _gameplayPanel.SetActive(false);
        if (_gameplayWorldCanvas != null) _gameplayWorldCanvas.SetActive(false);
        if (_endGamePanel != null) _endGamePanel.SetActive(false);
        _quickMatchButton.interactable = false;
        _playerVsBotButton.interactable = false;
    }

    private void OnEnable()
    {
        _quickMatchButton.onClick.AddListener(OnQuickMatchClicked);
        _playerVsBotButton.onClick.AddListener(OnPlayerVsBotClicked);
        _confirmTurnButton.onClick.AddListener(OnConfirmTurnClicked);
        if (_useSkillButton != null) _useSkillButton.onClick.AddListener(OnUseSkillClicked);
        if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        _deckBuilderManager.OnSelectionChanged += OnDeckSelectionChanged;
        _eventBus.SubscribeTo<MatchSearchStarted>(OnMatchSearchStarted);
        _eventBus.SubscribeTo<MatchFound>(OnMatchFound);
        _eventBus.SubscribeTo<MatchFailed>(OnMatchFailed);
        _eventBus.SubscribeTo<TurnStarted>(OnTurnStarted);
        _eventBus.SubscribeTo<TurnTimerUpdated>(OnTurnTimerUpdated);
        _eventBus.SubscribeTo<SimulationResult>(OnSimulationResult);
        _eventBus.SubscribeTo<GameOver>(OnGameOver);
        _eventBus.SubscribeTo<LocalPlayerConfirmed>(OnLocalPlayerConfirmed);
        _eventBus.SubscribeTo<SkillAssigned>(OnSkillAssigned);
        _eventBus.SubscribeTo<SkillUsed>(OnSkillUsed);
        _eventBus.SubscribeTo<HpDamageApplied>(OnHpDamageApplied);
    }

    private void OnDisable()
    {
        _quickMatchButton.onClick.RemoveListener(OnQuickMatchClicked);
        _playerVsBotButton.onClick.RemoveListener(OnPlayerVsBotClicked);
        _confirmTurnButton.onClick.RemoveListener(OnConfirmTurnClicked);
        if (_useSkillButton != null) _useSkillButton.onClick.RemoveListener(OnUseSkillClicked);
        if (_mainMenuButton != null) _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        _deckBuilderManager.OnSelectionChanged -= OnDeckSelectionChanged;
        _eventBus.UnsubscribeFrom<MatchSearchStarted>(OnMatchSearchStarted);
        _eventBus.UnsubscribeFrom<MatchFound>(OnMatchFound);
        _eventBus.UnsubscribeFrom<MatchFailed>(OnMatchFailed);
        _eventBus.UnsubscribeFrom<TurnStarted>(OnTurnStarted);
        _eventBus.UnsubscribeFrom<TurnTimerUpdated>(OnTurnTimerUpdated);
        _eventBus.UnsubscribeFrom<SimulationResult>(OnSimulationResult);
        _eventBus.UnsubscribeFrom<GameOver>(OnGameOver);
        _eventBus.UnsubscribeFrom<LocalPlayerConfirmed>(OnLocalPlayerConfirmed);
        _eventBus.UnsubscribeFrom<SkillAssigned>(OnSkillAssigned);
        _eventBus.UnsubscribeFrom<SkillUsed>(OnSkillUsed);
        _eventBus.UnsubscribeFrom<HpDamageApplied>(OnHpDamageApplied);
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
        SetButtonsInteractable(false);
        ShowGameplayPanel();
        _eventBus.Raise(new BotMatchStarted());
    }

    private void OnConfirmTurnClicked()
    {
        _eventBus.Raise(new TurnConfirmRequested());
    }

    private void OnTurnStarted(ref TurnStarted e)
    {
        _turnText.text = $"Turn {e.TurnNumber}";
        _confirmTurnButton.interactable = true;
        if (_useSkillButton != null) _useSkillButton.interactable = true;
    }

    private void OnTurnTimerUpdated(ref TurnTimerUpdated e)
    {
        int seconds = Mathf.CeilToInt(e.RemainingTime);
        _timerText.text = seconds + "s";
        _timerText.color = seconds <= 15 ? Color.red : Color.white;
    }

    private void OnSimulationResult(ref SimulationResult e)
    {
        _confirmTurnButton.interactable = false;
    }

    private void OnHpDamageApplied(ref HpDamageApplied e)
    {
        if (e.IsPlayer)
        {
            _playerHpText.text = e.NewHp.ToString();
            ShakeHpText(_playerHpText);
        }
        else
        {
            _opponentHpText.text = e.NewHp.ToString();
            ShakeHpText(_opponentHpText);
        }
    }

    private void ShakeHpText(TextMeshProUGUI text)
    {
        text.DOKill();
        text.transform.DOKill();
        text.color = Color.red;
        text.DOColor(Color.white, 0.6f).SetDelay(0.2f);
        text.transform.DOShakePosition(0.3f, 0.05f, 8);
    }

    private void OnGameOver(ref GameOver e)
    {
        _confirmTurnButton.interactable = false;
        if (_useSkillButton != null) _useSkillButton.interactable = false;

        string result;
        if (e.IsDraw)
            result = "Draw!";
        else if (e.PlayerWon)
            result = "You Win!";
        else
            result = "You Lose!";

        _turnText.text = result;

        if (_endGamePanel != null)
        {
            _endGamePanel.SetActive(true);
            if (_endGameResultText != null) _endGameResultText.text = result;
        }
    }

    private void OnLocalPlayerConfirmed(ref LocalPlayerConfirmed _)
    {
        _confirmTurnButton.interactable = false;
        if (_useSkillButton != null) _useSkillButton.interactable = false;
    }

    private void OnSkillAssigned(ref SkillAssigned e)
    {
        if (_skillDescriptionText != null) _skillDescriptionText.text = _skillConfig.GetDescription(e.Skill);
        if (_useSkillButton != null) _useSkillButton.interactable = true;
    }

    private void OnSkillUsed(ref SkillUsed _)
    {
        if (_useSkillButton != null) _useSkillButton.interactable = false;
    }

    private void OnUseSkillClicked()
    {
        _eventBus.Raise(new SkillUseRequested());
    }

    private void OnMainMenuClicked()
    {
        _networkManager.Disconnect();
    }

    private void ShowGameplayPanel()
    {
        _deckBuilderPanel.SetActive(false);
        _gameplayPanel.SetActive(true);
        if (_gameplayWorldCanvas != null) _gameplayWorldCanvas.SetActive(true);
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
