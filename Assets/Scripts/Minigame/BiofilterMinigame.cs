using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MG.09 - Biofilter.
/// Matches the shared minigame pattern used in the project:
/// - StartMinigame activates immediately for manager/tests
/// - Intro page with Start
/// - Gameplay page
/// - Result page with Continue
/// </summary>
public class BiofilterMinigame : MonoBehaviour, IMinigame
{
    public string MinigameName => "Build the Biofilter!";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => _isActive;

    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Gameplay")]
    public float timeLimit = 20f;

    private static readonly string[] CorrectOrder = { "Gravel", "Sand", "Charcoal", "Plants" };

    private const int HighReward = 2;
    private const int MidReward = 1;
    private const int LowReward = 0;

    private bool _isActive;
    private bool _isResolving;
    private bool _hasStartedRun;

    private VisualElement _root;
    private VisualElement _introContainer;
    private VisualElement _gameplayContainer;
    private VisualElement _resultContainer;

    private Label _titleLabel;
    private Label _descriptionLabel;
    private Label _timerLabel;
    private Label _progressLabel;
    private Label _waterQualityLabel;
    private Label _selectedLayersLabel;
    private Label _feedbackLabel;
    private Label _resultHeadlineLabel;
    private Label _resultLabel;
    private Label _resultFactLabel;
    private Label _resultScoreLabel;

    private Button _beginButton;
    private Button _exitButton;
    private Button _gravelButton;
    private Button _sandButton;
    private Button _charcoalButton;
    private Button _plantsButton;
    private Button _runFilterButton;

    private float _timeRemaining;
    private int _pendingReward;
    private readonly List<string> _selectedLayers = new();

    private void Start()
    {
        HideUI();
    }

    private void Update()
    {
        if (!_isActive || _isResolving || !_hasStartedRun)
        {
            return;
        }

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            UpdateHud();
            ResolveResult();
            return;
        }

        UpdateHud();
    }

    public void StartMinigame()
    {
        _isActive = true;
        _isResolving = false;
        _hasStartedRun = false;
        _pendingReward = 0;

        if (uiDocument == null)
        {
            Debug.LogError("BiofilterMinigame: UIDocument is not assigned.");
            _isActive = false;
            OnMinigameExited?.Invoke();
            return;
        }

        _root = uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("BiofilterMinigame: rootVisualElement is null.");
            _isActive = false;
            OnMinigameExited?.Invoke();
            return;
        }

        CacheUi();
        RegisterCallbacks();
        ShowIntroState();

        _root.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Kept because some shared tests / manager flows in the project expect it.
    /// This closes the UI only; it does not emit completion/exit events by itself.
    /// </summary>
    public void CloseMinigame()
    {
        DisableButtons();
        HideUI();

        _isActive = false;
        _isResolving = false;
        _hasStartedRun = false;
    }

    public int CalculateReward(int correctPositions)
    {
        if (correctPositions == 4) return HighReward;
        if (correctPositions >= 2) return MidReward;
        return LowReward;
    }

    private void CacheUi()
    {
        _titleLabel = _root.Q<Label>("minigame_title");
        _descriptionLabel = _root.Q<Label>("minigame_description");
        _introContainer = _root.Q<VisualElement>("intro_container");
        _gameplayContainer = _root.Q<VisualElement>("gameplay_container");
        _resultContainer = _root.Q<VisualElement>("result_container");

        _timerLabel = _root.Q<Label>("timer_label");
        _progressLabel = _root.Q<Label>("progress_label");
        _waterQualityLabel = _root.Q<Label>("water_quality_label");
        _selectedLayersLabel = _root.Q<Label>("selected_layers_label");
        _feedbackLabel = _root.Q<Label>("feedback_label");
        _resultHeadlineLabel = _root.Q<Label>("result_headline");
        _resultLabel = _root.Q<Label>("result_label");
        _resultFactLabel = _root.Q<Label>("result_fact");
        _resultScoreLabel = _root.Q<Label>("result_score");

        _beginButton = _root.Q<Button>("begin_button");
        _exitButton = _root.Q<Button>("exit_button");

        _gravelButton = _root.Q<Button>("gravel_button");
        _sandButton = _root.Q<Button>("sand_button");
        _charcoalButton = _root.Q<Button>("charcoal_button");
        _plantsButton = _root.Q<Button>("plants_button");
        _runFilterButton = _root.Q<Button>("run_filter_button");
    }

    private void RegisterCallbacks()
    {
        RegisterButton(_beginButton, HandleBeginClicked);
        RegisterButton(_exitButton, HandleExitClicked);

        RegisterButton(_gravelButton, HandleGravelClicked);
        RegisterButton(_sandButton, HandleSandClicked);
        RegisterButton(_charcoalButton, HandleCharcoalClicked);
        RegisterButton(_plantsButton, HandlePlantsClicked);
        RegisterButton(_runFilterButton, HandleRunFilterClicked);
    }

    private void RegisterButton(Button button, EventCallback<ClickEvent> callback)
    {
        if (button == null)
        {
            return;
        }

        button.UnregisterCallback(callback);
        button.SetEnabled(true);
        button.RegisterCallback(callback);
    }

    private void ShowIntroState()
    {
        SetLabel(_titleLabel, "Build the Biofilter!");
        SetLabel(_descriptionLabel, "Rules:\n1. Tap 4 filter parts.\n2. Put them in order: Gravel, Sand, Charcoal, Plants.\n3. Tap Run Filter before time is up.");

        SetDisplay(_introContainer, DisplayStyle.Flex);
        SetDisplay(_gameplayContainer, DisplayStyle.None);
        SetDisplay(_resultContainer, DisplayStyle.None);

        if (_exitButton != null)
        {
            _exitButton.style.display = DisplayStyle.None;
        }

        ResetRunState();
    }

    private void ResetRunState()
    {
        _timeRemaining = timeLimit;
        _pendingReward = 0;
        _selectedLayers.Clear();

        UpdateSelectedLayersLabel();
        UpdateHud();
        SetLabel(_feedbackLabel, "Build from bottom to top.");
    }

    private void StartRun()
    {
        _isActive = true;
        _isResolving = false;
        _hasStartedRun = true;
        ResetRunState();

        SetLabel(_titleLabel, "Build the Biofilter!");
        SetLabel(_descriptionLabel, "Tap parts in the right order.");

        SetDisplay(_introContainer, DisplayStyle.None);
        SetDisplay(_gameplayContainer, DisplayStyle.Flex);
        SetDisplay(_resultContainer, DisplayStyle.None);

        if (_exitButton != null)
        {
            _exitButton.style.display = DisplayStyle.None;
        }

        SetButtonsEnabled(true);
    }

    private void HandleBeginClicked(ClickEvent evt)
    {
        if (!_isActive || _isResolving || _hasStartedRun)
        {
            return;
        }

        if (_beginButton != null)
        {
            _beginButton.UnregisterCallback<ClickEvent>(HandleBeginClicked);
            _beginButton.SetEnabled(false);
        }

        StartRun();
    }

    private void HandleGravelClicked(ClickEvent evt) => AddLayer("Gravel");
    private void HandleSandClicked(ClickEvent evt) => AddLayer("Sand");
    private void HandleCharcoalClicked(ClickEvent evt) => AddLayer("Charcoal");
    private void HandlePlantsClicked(ClickEvent evt) => AddLayer("Plants");

    private void AddLayer(string layer)
    {
        if (!_isActive || _isResolving || !_hasStartedRun)
        {
            return;
        }

        if (_selectedLayers.Count >= 4)
        {
            return;
        }

        _selectedLayers.Add(layer);
        UpdateSelectedLayersLabel();
        SetLabel(_feedbackLabel, $"Added {layer}.");
        UpdateHud();

        AudioManager.Instance?.PlayConfirm();
    }

    private void UpdateSelectedLayersLabel()
    {
        if (_selectedLayersLabel == null)
        {
            return;
        }

        _selectedLayersLabel.text = _selectedLayers.Count == 0
            ? "None yet"
            : string.Join(" -> ", _selectedLayers);
    }

    private void UpdateHud()
    {
        if (_timerLabel != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, _timeRemaining));
            _timerLabel.text = $"Time: {seconds}s";
            _timerLabel.style.color = _timeRemaining <= 4f ? new Color(1f, 0.3f, 0.3f) : Color.white;
        }

        if (_progressLabel != null)
        {
            _progressLabel.text = $"Steps: {_selectedLayers.Count} / 4";
        }

        if (_waterQualityLabel != null)
        {
            _waterQualityLabel.text = _selectedLayers.Count < 4
                ? "Water: Dirty"
                : "Water: Ready to Filter";
        }
    }

    private void HandleRunFilterClicked(ClickEvent evt)
    {
        if (!_isActive || _isResolving || !_hasStartedRun)
        {
            return;
        }

        ResolveResult();
    }

    private void ResolveResult()
    {
        if (!_isActive || _isResolving)
        {
            return;
        }

        _hasStartedRun = false;
        _isActive = false;
        _isResolving = true;

        int correctPositions = 0;
        for (int i = 0; i < Mathf.Min(_selectedLayers.Count, CorrectOrder.Length); i++)
        {
            if (_selectedLayers[i] == CorrectOrder[i])
            {
                correctPositions++;
            }
        }

        _pendingReward = CalculateReward(correctPositions);

        if (_pendingReward == HighReward)
        {
            AudioManager.Instance?.PlayBirdsChirping();
        }

        ShowResult(correctPositions);
    }

    private void ShowResult(int correctPositions)
    {
        string headline = _pendingReward == HighReward
            ? "Awesome job!"
            : _pendingReward == MidReward
                ? "Nice work!"
                : "Keep trying!";

        string message = $"You put {correctPositions} layer(s) in the right spot.";
        string fact = "Biofilters can help clean dirty water.";

        SetDisplay(_introContainer, DisplayStyle.None);
        SetDisplay(_gameplayContainer, DisplayStyle.None);
        SetDisplay(_resultContainer, DisplayStyle.Flex);

        if (_exitButton != null)
        {
            _exitButton.style.display = DisplayStyle.Flex;
            _exitButton.text = "Continue";
        }

        SetLabel(_resultHeadlineLabel, headline);
        SetLabel(_resultLabel, message);
        SetLabel(_resultFactLabel, $"Fact: {fact}");
        SetLabel(_resultScoreLabel, $"Pollution reduction: {_pendingReward}");
    }

    private void HandleExitClicked(ClickEvent evt)
    {
        if (!_isActive && !_isResolving)
        {
            return;
        }

        bool finishingFromEndPage = _isResolving;
        int reward = _pendingReward;

        CloseMinigame();

        if (finishingFromEndPage)
        {
            OnMinigameComplete?.Invoke(reward);
        }
        else
        {
            OnMinigameExited?.Invoke();
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (_gravelButton != null) _gravelButton.SetEnabled(enabled);
        if (_sandButton != null) _sandButton.SetEnabled(enabled);
        if (_charcoalButton != null) _charcoalButton.SetEnabled(enabled);
        if (_plantsButton != null) _plantsButton.SetEnabled(enabled);
        if (_runFilterButton != null) _runFilterButton.SetEnabled(enabled);
    }

    private void DisableButtons()
    {
        DisableButton(_beginButton, HandleBeginClicked);
        DisableButton(_exitButton, HandleExitClicked);

        DisableButton(_gravelButton, HandleGravelClicked);
        DisableButton(_sandButton, HandleSandClicked);
        DisableButton(_charcoalButton, HandleCharcoalClicked);
        DisableButton(_plantsButton, HandlePlantsClicked);
        DisableButton(_runFilterButton, HandleRunFilterClicked);
    }

    private void DisableButton(Button button, EventCallback<ClickEvent> callback)
    {
        if (button == null)
        {
            return;
        }

        button.UnregisterCallback(callback);
        button.SetEnabled(false);
    }

    private void SetLabel(Label label, string text)
    {
        if (label == null)
        {
            return;
        }

        label.text = text;
        label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void SetDisplay(VisualElement element, DisplayStyle displayStyle)
    {
        if (element != null)
        {
            element.style.display = displayStyle;
        }
    }

    private void HideUI()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}

