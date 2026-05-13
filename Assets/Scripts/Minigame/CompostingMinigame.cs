using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CompostingMinigame : MonoBehaviour, IMinigame
{
    public string MinigameName => "Compost Correctly!";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => _isActive;

    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Gameplay")]
    public float timeLimit = 20f;
    public int highRewardThreshold = 8;
    public int midRewardThreshold = 4;

    private const int HighReward = 2;
    private const int MidReward = 1;
    private const int LowReward = 0;

    private enum BinType
    {
        Compost,
        Recycle,
        Landfill
    }

    private struct CompostItem
    {
        public string Name;
        public string Emoji;
        public BinType CorrectBin;

        public CompostItem(string name, string emoji, BinType correctBin)
        {
            Name = name;
            Emoji = emoji;
            CorrectBin = correctBin;
        }
    }

    private static readonly CompostItem[] ItemPool =
    {
        new CompostItem("Banana Peel", "🍌", BinType.Compost),
        new CompostItem("Apple Core", "🍎", BinType.Compost),
        new CompostItem("Dry Leaves", "🍂", BinType.Compost),
        new CompostItem("Coffee Grounds", "☕", BinType.Compost),
        new CompostItem("Eggshells", "🥚", BinType.Compost),
        new CompostItem("Plastic Bottle", "🧴", BinType.Recycle),
        new CompostItem("Newspaper", "📰", BinType.Recycle),
        new CompostItem("Aluminum Can", "🥫", BinType.Recycle),
        new CompostItem("Chip Bag", "🥡", BinType.Landfill),
        new CompostItem("Dirty Tissue", "🧻", BinType.Landfill),
        new CompostItem("Candy Wrapper", "🍬", BinType.Landfill)
    };

    private bool _isActive;
    private bool _isResolving;
    private bool _hasStartedRun;

    private VisualElement _root;
    private VisualElement _introContainer;
    private VisualElement _gameplayContainer;
    private VisualElement _resultContainer;
    private VisualElement _itemCard;

    private Label _titleLabel;
    private Label _descriptionLabel;
    private Label _timerLabel;
    private Label _scoreLabel;
    private Label _streakLabel;
    private Label _itemEmojiLabel;
    private Label _itemLabel;
    private Label _feedbackLabel;
    private Label _resultHeadlineLabel;
    private Label _resultLabel;
    private Label _resultFactLabel;
    private Label _resultScoreLabel;

    private Button _beginButton;
    private Button _exitButton;
    private Button _compostButton;
    private Button _recycleButton;
    private Button _landfillButton;

    private float _timeRemaining;
    private int _correctCount;
    private int _attemptCount;
    private int _streakCount;
    private int _pendingReward;
    private CompostItem _currentItem;
    private readonly List<CompostItem> _runItems = new();
    private readonly System.Random _rng = new();

    private void Start()
    {
        HideUI();
    }

    private void LateUpdate()
    {
        if (!_isActive && !_isResolving && uiDocument != null && uiDocument.rootVisualElement != null
            && uiDocument.rootVisualElement.style.display != DisplayStyle.None)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
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
            HandleTimerEnd();
            return;
        }

        UpdateHud();
    }

    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("CompostingMinigame: UIDocument is not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        _root = uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("CompostingMinigame: rootVisualElement is null.");
            OnMinigameExited?.Invoke();
            return;
        }

        CacheUi();
        RegisterCallbacks();

        _isActive = true;
        _isResolving = false;
        _hasStartedRun = false;
        _pendingReward = 0;

        _root.style.display = DisplayStyle.Flex;
        ShowIntroState();
    }

    public int CalculateReward(int correctCount)
    {
        if (correctCount >= highRewardThreshold) return HighReward;
        if (correctCount >= midRewardThreshold) return MidReward;
        return LowReward;
    }

    private void CacheUi()
    {
        _titleLabel = _root.Q<Label>("minigame_title");
        _descriptionLabel = _root.Q<Label>("minigame_description");
        _introContainer = _root.Q<VisualElement>("intro_container");
        _gameplayContainer = _root.Q<VisualElement>("gameplay_container");
        _resultContainer = _root.Q<VisualElement>("result_container");
        _itemCard = _root.Q<VisualElement>("item_card");

        _timerLabel = _root.Q<Label>("timer_label");
        _scoreLabel = _root.Q<Label>("score_label");
        _streakLabel = _root.Q<Label>("streak_label");
        _itemEmojiLabel = _root.Q<Label>("item_emoji_label");
        _itemLabel = _root.Q<Label>("item_label");
        _feedbackLabel = _root.Q<Label>("feedback_label");
        _resultHeadlineLabel = _root.Q<Label>("result_headline");
        _resultLabel = _root.Q<Label>("result_label");
        _resultFactLabel = _root.Q<Label>("result_fact");
        _resultScoreLabel = _root.Q<Label>("result_score");

        _beginButton = _root.Q<Button>("begin_button");
        _exitButton = _root.Q<Button>("exit_button");

        _compostButton = _root.Q<Button>("compost_bin_button");
        _recycleButton = _root.Q<Button>("recycle_bin_button");
        _landfillButton = _root.Q<Button>("landfill_bin_button");
    }

    private void RegisterCallbacks()
    {
        RegisterButton(_beginButton, HandleBeginClicked);
        RegisterButton(_exitButton, HandleExitClicked);

        RegisterButton(_compostButton, HandleCompostClicked);
        RegisterButton(_recycleButton, HandleRecycleClicked);
        RegisterButton(_landfillButton, HandleLandfillClicked);
    }

    private void RegisterButton(Button button, EventCallback<ClickEvent> callback)
    {
        if (button == null) return;
        button.UnregisterCallback(callback);
        button.SetEnabled(true);
        button.RegisterCallback(callback);
    }

    private void ShowIntroState()
    {
        SetLabel(_titleLabel, "Compost Correctly!");
        SetLabel(_descriptionLabel, "Rules:\n1. Look at the item.\n2. Tap Compost, Recycle, or Landfill.\n3. Sort as many as you can before time is up.");

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
        _correctCount = 0;
        _attemptCount = 0;
        _streakCount = 0;
        _pendingReward = 0;

        _runItems.Clear();
        _runItems.AddRange(ItemPool);
        ShuffleItems();

        _currentItem = _runItems[0];
        SetLabel(_feedbackLabel, "Pick the right bin.");
        UpdateCurrentItemDisplay();
        UpdateHud();
    }

    private void ShuffleItems()
    {
        for (int i = _runItems.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_runItems[i], _runItems[j]) = (_runItems[j], _runItems[i]);
        }
    }

    private void StartRun()
    {
        _isActive = true;
        _isResolving = false;
        _hasStartedRun = true;
        ResetRunState();

        SetLabel(_titleLabel, "Compost Correctly!");
        SetLabel(_descriptionLabel, "Tap the right bin for each item.");

        SetDisplay(_introContainer, DisplayStyle.None);
        SetDisplay(_gameplayContainer, DisplayStyle.Flex);
        SetDisplay(_resultContainer, DisplayStyle.None);

        SetBinsEnabled(true);
    }

    private void HandleBeginClicked(ClickEvent evt)
    {
        if (!_isActive || _isResolving || _hasStartedRun) return;
        StartRun();
    }

    private void HandleCompostClicked(ClickEvent evt) => HandleBinChoice(BinType.Compost);
    private void HandleRecycleClicked(ClickEvent evt) => HandleBinChoice(BinType.Recycle);
    private void HandleLandfillClicked(ClickEvent evt) => HandleBinChoice(BinType.Landfill);

    private void HandleBinChoice(BinType chosenBin)
    {
        if (!_isActive || _isResolving || !_hasStartedRun) return;

        _attemptCount++;

        bool correct = chosenBin == _currentItem.CorrectBin;
        if (correct)
        {
            _correctCount++;
            _streakCount++;
            SetLabel(_feedbackLabel, $"Correct! {_currentItem.Name} goes in {GetBinDisplayName(chosenBin)}.");
            AudioManager.Instance?.PlayConfirm();
        }
        else
        {
            _streakCount = 0;
            SetLabel(_feedbackLabel, $"Not quite. {_currentItem.Name} belongs in {GetBinDisplayName(_currentItem.CorrectBin)}.");
        }

        FlashItemCard(correct);
        AdvanceToNextItem();
        UpdateHud();
    }

    private void AdvanceToNextItem()
    {
        int index = _attemptCount % _runItems.Count;
        _currentItem = _runItems[index];
        UpdateCurrentItemDisplay();
    }

    private void UpdateCurrentItemDisplay()
    {
        SetLabel(_itemEmojiLabel, _currentItem.Emoji);
        SetLabel(_itemLabel, _currentItem.Name);
    }

    private void FlashItemCard(bool correct)
    {
        if (_itemCard == null || _root == null) return;

        string className = correct ? "cm-item-card-correct" : "cm-item-card-wrong";
        _itemCard.AddToClassList(className);
        _root.schedule.Execute(() => _itemCard?.RemoveFromClassList(className)).StartingIn(180);
    }

    private string GetBinDisplayName(BinType binType)
    {
        return binType switch
        {
            BinType.Compost => "Compost",
            BinType.Recycle => "Recycle",
            BinType.Landfill => "Landfill",
            _ => "Unknown"
        };
    }

    private void UpdateHud()
    {
        if (_timerLabel != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, _timeRemaining));
            _timerLabel.text = $"Time: {seconds}s";
            _timerLabel.style.color = _timeRemaining <= 4f ? new Color(1f, 0.3f, 0.3f) : Color.white;
        }

        if (_scoreLabel != null) _scoreLabel.text = $"Correct: {_correctCount}";
        if (_streakLabel != null) _streakLabel.text = $"Streak: {_streakCount}";
    }

    private void HandleTimerEnd()
    {
        if (!_isActive || _isResolving || !_hasStartedRun) return;

        _hasStartedRun = false;
        _isActive = false;
        _isResolving = true;
        _pendingReward = CalculateReward(_correctCount);
        SetBinsEnabled(false);

        if (_pendingReward == HighReward)
        {
            AudioManager.Instance?.PlayBirdsChirping();
        }

        ShowResult();
    }

    private void ShowResult()
    {
        string headline = _pendingReward == HighReward
            ? "Awesome sorting!"
            : _pendingReward == MidReward
                ? "Nice work!"
                : "Keep trying!";

        string message = $"You got {_correctCount} right.";
        string fact = "Composting food scraps can help plants grow.";

        SetDisplay(_introContainer, DisplayStyle.None);
        SetDisplay(_gameplayContainer, DisplayStyle.None);
        SetDisplay(_resultContainer, DisplayStyle.Flex);

        SetLabel(_resultHeadlineLabel, headline);
        SetLabel(_resultLabel, message);
        SetLabel(_resultFactLabel, $"Fact: {fact}");
        SetLabel(_resultScoreLabel, $"Pollution reduction: {_pendingReward}");
        if (_exitButton != null)
        {
            _exitButton.text = "Continue";
            _exitButton.style.display = DisplayStyle.Flex;
        }
    }

    private void HandleExitClicked(ClickEvent evt)
    {
        if (!_isActive && !_isResolving) return;

        bool finishingFromEndPage = _isResolving;
        int reward = _pendingReward;

        DisableButtons();
        HideUI();

        _isActive = false;
        _isResolving = false;
        _hasStartedRun = false;

        if (finishingFromEndPage)
        {
            OnMinigameComplete?.Invoke(reward);
        }
        else
        {
            OnMinigameExited?.Invoke();
        }
    }

    private void SetBinsEnabled(bool enabled)
    {
        if (_compostButton != null) _compostButton.SetEnabled(enabled);
        if (_recycleButton != null) _recycleButton.SetEnabled(enabled);
        if (_landfillButton != null) _landfillButton.SetEnabled(enabled);
    }

    private void DisableButtons()
    {
        DisableButton(_beginButton, HandleBeginClicked);
        DisableButton(_exitButton, HandleExitClicked);

        DisableButton(_compostButton, HandleCompostClicked);
        DisableButton(_recycleButton, HandleRecycleClicked);
        DisableButton(_landfillButton, HandleLandfillClicked);
    }

    private void DisableButton(Button button, EventCallback<ClickEvent> callback)
    {
        if (button == null) return;
        button.UnregisterCallback(callback);
        button.SetEnabled(false);
    }

    private void SetLabel(Label label, string text)
    {
        if (label == null) return;
        label.text = text;
        label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void SetDisplay(VisualElement element, DisplayStyle displayStyle)
    {
        if (element != null) element.style.display = displayStyle;
    }

    private void HideUI()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}
