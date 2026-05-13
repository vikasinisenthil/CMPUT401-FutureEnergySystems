using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MG.06 - Ride a Bike Minigame.
/// </summary>
public class RideBikeMinigame : MonoBehaviour, IMinigame
{
    public string MinigameName => "Ride a Bike!";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => _isActive;

    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Gameplay")]
    public float timeLimit = 15f;
    public float goalDistance = 50f;
    public float metresPerTap = 1.5f;

    /// <summary>
    /// Optional sprite override. If not set, sprite is fetched from GameManager.
    /// </summary>
    public Sprite playerSprite;

    private const int FullReward = 2;
    private const int HalfReward = 1;
    private const int ZeroReward = 0;
    private const float SpeedWindow = 0.8f;

    private static readonly string[] WheelSpinClasses =
    {
        "rb-wheel-spin-1",
        "rb-wheel-spin-2",
        "rb-wheel-spin-3",
        "rb-wheel-spin-4"
    };

    private bool _isActive;
    private bool _isResolving;
    private bool _hasStartedRun;
    private bool _completionQueued;
    private int _pendingReward;
    private float _timeRemaining;
    private float _distanceTravelled;
    private int _wheelFrame;
    private int _recentTaps;
    private float _speedResetTimer;

    private VisualElement _root;
    private Label _titleLabel;
    private Label _descriptionLabel;
    private VisualElement _introContainer;
    private VisualElement _gameplayContainer;
    private VisualElement _resultContainer;
    private VisualElement _rulesPopup;
    private Button _beginButton;
    private Button _rulesButton;
    private Button _retryButton;
    private Button _backButton;
    private Button _exitButton;

    private VisualElement _tapArea;
    private Label _timerLabel;
    private Label _distanceLabel;
    private Label _speedometerLabel;
    private Label _tapHint;
    private VisualElement _progressTrack;
    private VisualElement _progressFill;
    private VisualElement _bikeIcon;
    private VisualElement _wheelRear;
    private VisualElement _wheelFront;
    private VisualElement _motionLines;
    private VisualElement _riderSilhouette;

    private Label _resultHeadlineLabel;
    private Label _resultLabel;
    private Label _resultFactLabel;
    private Label _resultScoreLabel;

    private void Start()
    {
        HideUI();
    }

    private void LateUpdate()
    {
        if (!_isActive && !_isResolving &&
            uiDocument != null &&
            uiDocument.rootVisualElement != null &&
            uiDocument.rootVisualElement.style.display != DisplayStyle.None)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void Update()
    {
        if (!_isActive || _isResolving || !_hasStartedRun || _completionQueued)
        {
            return;
        }

        _timeRemaining -= Time.deltaTime;

        _speedResetTimer -= Time.deltaTime;
        if (_speedResetTimer <= 0f)
        {
            _recentTaps = 0;
            _speedResetTimer = SpeedWindow;
        }

        UpdateHUD();

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            HandleTimerEnd();
        }
    }

    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("RideBikeMinigame: UIDocument not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        _root = uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("RideBikeMinigame: rootVisualElement is null.");
            OnMinigameExited?.Invoke();
            return;
        }

        CacheUi();
        RegisterCallbacks();
        ConfigureRiderVisual();

        _isActive = true;
        _isResolving = false;
        _hasStartedRun = false;
        _completionQueued = false;
        _pendingReward = 0;

        _root.style.display = DisplayStyle.Flex;
        ShowIntroState();
    }

    public int CalculateReward(float distance)
    {
        if (distance >= goalDistance) return FullReward;
        if (distance >= goalDistance / 2f) return HalfReward;
        return ZeroReward;
    }

    public float GetDistanceTravelled() => _distanceTravelled;
    public float GetTimeRemaining() => _timeRemaining;

    private void CacheUi()
    {
        _titleLabel = _root.Q<Label>("minigame_title");
        _descriptionLabel = _root.Q<Label>("minigame_description");
        _introContainer = _root.Q<VisualElement>("intro_container");
        _gameplayContainer = _root.Q<VisualElement>("gameplay_container");
        _resultContainer = _root.Q<VisualElement>("result_container");
        _rulesPopup = _root.Q<VisualElement>("rules_popup");
        _beginButton = _root.Q<Button>("begin_button");
        _rulesButton = _root.Q<Button>("rules_button");
        _retryButton = _root.Q<Button>("retry_button");
        _backButton = _root.Q<Button>("back_button");
        _exitButton = _root.Q<Button>("exit_button");

        _tapArea = _root.Q<VisualElement>("tap_area");
        _timerLabel = _root.Q<Label>("timer_label");
        _distanceLabel = _root.Q<Label>("distance_label");
        _speedometerLabel = _root.Q<Label>("speed_label");
        _tapHint = _root.Q<Label>("tap_hint");
        _progressTrack = _root.Q<VisualElement>("progress_track");
        _progressFill = _root.Q<VisualElement>("progress_fill");
        _bikeIcon = _root.Q<VisualElement>("bike_icon");
        _wheelRear = _root.Q<VisualElement>("wheel_rear");
        _wheelFront = _root.Q<VisualElement>("wheel_front");
        _motionLines = _root.Q<VisualElement>("motion_lines");
        _riderSilhouette = _root.Q<VisualElement>("rider_silhouette");

        _resultHeadlineLabel = _root.Q<Label>("result_headline");
        _resultLabel = _root.Q<Label>("result_label");
        _resultFactLabel = _root.Q<Label>("result_fact");
        _resultScoreLabel = _root.Q<Label>("result_score");
    }

    private void RegisterCallbacks()
    {
        if (_beginButton != null)
        {
            _beginButton.UnregisterCallback<ClickEvent>(HandleBeginClicked);
            _beginButton.SetEnabled(true);
            _beginButton.RegisterCallback<ClickEvent>(HandleBeginClicked);
        }

        if (_rulesButton != null)
        {
            _rulesButton.UnregisterCallback<ClickEvent>(HandleRulesClicked);
            _rulesButton.SetEnabled(true);
            _rulesButton.RegisterCallback<ClickEvent>(HandleRulesClicked);
        }

        if (_retryButton != null)
        {
            _retryButton.UnregisterCallback<ClickEvent>(HandleRetryClicked);
            _retryButton.SetEnabled(true);
            _retryButton.RegisterCallback<ClickEvent>(HandleRetryClicked);
        }

        if (_backButton != null)
        {
            _backButton.UnregisterCallback<ClickEvent>(HandleBackClicked);
            _backButton.SetEnabled(true);
            _backButton.RegisterCallback<ClickEvent>(HandleBackClicked);
        }

        if (_exitButton != null)
        {
            _exitButton.UnregisterCallback<ClickEvent>(HandleExitClicked);
            _exitButton.SetEnabled(true);
            _exitButton.RegisterCallback<ClickEvent>(HandleExitClicked);
        }
    }

    private void ShowIntroState()
    {
        if (_titleLabel != null)
        {
            _titleLabel.text = "Welcome to Ride a Bike";
        }

        if (_descriptionLabel != null)
        {
            _descriptionLabel.text =
                "Rules:\n1. Tap to pedal.\n2. Reach the finish line before time is up.\n3. Faster taps help you go farther.";
            _descriptionLabel.style.display = DisplayStyle.Flex;
            _descriptionLabel.style.fontSize = 20f;
            _descriptionLabel.style.marginTop = 0f;
            _descriptionLabel.style.marginBottom = 18f;
            _descriptionLabel.style.maxWidth = 760f;
            _descriptionLabel.style.paddingLeft = 72f;
            _descriptionLabel.style.paddingRight = 72f;
            _descriptionLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        }

        if (_introContainer != null) _introContainer.style.display = DisplayStyle.Flex;
        if (_gameplayContainer != null) _gameplayContainer.style.display = DisplayStyle.None;
        if (_resultContainer != null) _resultContainer.style.display = DisplayStyle.None;
        if (_rulesPopup != null) _rulesPopup.style.display = DisplayStyle.None;
        if (_retryButton != null) _retryButton.style.display = DisplayStyle.None;

        if (_exitButton != null)
        {
            _exitButton.style.display = DisplayStyle.Flex;
            _exitButton.text = "Exit";
        }

        if (_tapArea != null)
        {
            _tapArea.UnregisterCallback<ClickEvent>(HandleTap);
        }

        _distanceTravelled = 0f;
        _timeRemaining = timeLimit;
        _recentTaps = 0;
        _speedResetTimer = SpeedWindow;
        _wheelFrame = 0;
        UpdateHUD();
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

    private void StartRun()
    {
        _isActive = true;
        _isResolving = false;
        _hasStartedRun = true;
        _completionQueued = false;
        _pendingReward = 0;
        _distanceTravelled = 0f;
        _timeRemaining = timeLimit;
        _recentTaps = 0;
        _speedResetTimer = SpeedWindow;
        _wheelFrame = 0;

        if (_titleLabel != null)
        {
            _titleLabel.text = "Ride a Bike!";
        }

        if (_descriptionLabel != null)
        {
            _descriptionLabel.text = "Tap fast to pedal and reach the goal distance.";
            _descriptionLabel.style.display = DisplayStyle.Flex;
            _descriptionLabel.style.fontSize = 18f;
            _descriptionLabel.style.marginTop = 0f;
            _descriptionLabel.style.marginBottom = 10f;
            _descriptionLabel.style.maxWidth = 500f;
            _descriptionLabel.style.paddingLeft = 0f;
            _descriptionLabel.style.paddingRight = 0f;
            _descriptionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        if (_introContainer != null) _introContainer.style.display = DisplayStyle.None;
        if (_gameplayContainer != null) _gameplayContainer.style.display = DisplayStyle.Flex;
        if (_resultContainer != null) _resultContainer.style.display = DisplayStyle.None;
        if (_rulesPopup != null) _rulesPopup.style.display = DisplayStyle.None;
        if (_retryButton != null) _retryButton.style.display = DisplayStyle.None;
        if (_exitButton != null) _exitButton.style.display = DisplayStyle.None;

        if (_tapArea != null)
        {
            _tapArea.style.display = DisplayStyle.Flex;
            _tapArea.UnregisterCallback<ClickEvent>(HandleTap);
            _tapArea.RegisterCallback<ClickEvent>(HandleTap);
        }

        if (_tapHint != null)
        {
            _tapHint.style.display = DisplayStyle.Flex;
        }

        if (_motionLines != null)
        {
            _motionLines.RemoveFromClassList("rb-motion-lines-show");
        }

        UpdateHUD();
    }

    private void HandleTap(ClickEvent evt)
    {
        if (!_isActive || _isResolving || !_hasStartedRun || _completionQueued)
        {
            return;
        }

        _distanceTravelled += metresPerTap;
        _recentTaps++;
        _speedResetTimer = SpeedWindow;

        SpinWheels();
        FlashMotionLines();
        ShowTapEffect(evt);

        if (_tapHint != null)
        {
            _tapHint.style.display = DisplayStyle.None;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

        if (_distanceTravelled >= goalDistance)
        {
            _distanceTravelled = goalDistance;
            HandleGoalReached();
        }
    }

    private void SpinWheels()
    {
        if (_wheelRear == null || _wheelFront == null)
        {
            return;
        }

        string previousClass = WheelSpinClasses[_wheelFrame % WheelSpinClasses.Length];
        _wheelRear.RemoveFromClassList(previousClass);
        _wheelFront.RemoveFromClassList(previousClass);

        _wheelFrame = (_wheelFrame + 1) % WheelSpinClasses.Length;
        string nextClass = WheelSpinClasses[_wheelFrame];
        _wheelRear.AddToClassList(nextClass);
        _wheelFront.AddToClassList(nextClass);
    }

    private void FlashMotionLines()
    {
        if (_motionLines == null || _root == null)
        {
            return;
        }

        _motionLines.AddToClassList("rb-motion-lines-show");
        _root.schedule.Execute(() => _motionLines?.RemoveFromClassList("rb-motion-lines-show")).StartingIn(180);
    }

    private void UpdateHUD()
    {
        if (_timerLabel != null)
        {
            int secs = Mathf.CeilToInt(Mathf.Max(0f, _timeRemaining));
            _timerLabel.text = $"Time: {secs}s";
            _timerLabel.style.color = _timeRemaining <= 3f
                ? new Color(1f, 0.3f, 0.3f)
                : Color.white;
        }

        if (_distanceLabel != null)
        {
            _distanceLabel.text = $"{_distanceTravelled:F0} / {goalDistance:F0} m";
        }

        if (_speedometerLabel != null)
        {
            float speed = (_recentTaps / SpeedWindow) * metresPerTap * 3.6f;
            _speedometerLabel.text = $"🚲 {speed:F0} km/h";
        }

        if (_progressFill != null)
        {
            float pct = Mathf.Clamp01(_distanceTravelled / goalDistance) * 100f;
            _progressFill.style.width = Length.Percent(pct);
        }

        if (_bikeIcon != null && _progressTrack != null)
        {
            float pct = Mathf.Clamp01(_distanceTravelled / goalDistance);
            float trackWidth = _progressTrack.resolvedStyle.width;
            if (trackWidth > 0f)
            {
                _bikeIcon.style.left = new Length(pct * (trackWidth - 32f), LengthUnit.Pixel);
            }
        }
    }

    private void HandleGoalReached()
    {
        if (_tapArea != null)
        {
            _tapArea.UnregisterCallback<ClickEvent>(HandleTap);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBirdsChirping();
        }

        ShowResult(
            "You made it!",
            $"Goal reached! Distance biked: {_distanceTravelled:F0} m.",
            "Riding a bike helps keep the air cleaner.",
            FullReward);
    }

    private void HandleTimerEnd()
    {
        if (!_isActive || _completionQueued)
        {
            return;
        }

        if (_tapArea != null)
        {
            _tapArea.UnregisterCallback<ClickEvent>(HandleTap);
        }

        int reward = CalculateReward(_distanceTravelled);
        bool hitGoal = _distanceTravelled >= goalDistance;

        if (hitGoal && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBirdsChirping();
        }

        string headline = hitGoal
            ? "You made it!"
            : reward == HalfReward
                ? "Good effort"
                : "Keep practicing";

        string message = hitGoal
            ? $"Goal reached! Distance biked: {_distanceTravelled:F0} m."
            : reward == HalfReward
                ? $"Nice try! You biked {_distanceTravelled:F0} m."
                : $"Keep going! You biked {_distanceTravelled:F0} m.";
        ShowResult(
            headline,
            message,
            "Every pedal helps make cleaner air.",
            reward);
    }

    private void ShowResult(string headline, string message, string fact, int reward)
    {
        _completionQueued = true;
        _isActive = false;
        _isResolving = true;
        _hasStartedRun = false;
        _pendingReward = reward;

        if (_descriptionLabel != null)
        {
            _descriptionLabel.text = string.Empty;
            _descriptionLabel.style.display = DisplayStyle.None;
        }

        if (_introContainer != null) _introContainer.style.display = DisplayStyle.None;
        if (_gameplayContainer != null) _gameplayContainer.style.display = DisplayStyle.None;
        if (_rulesPopup != null) _rulesPopup.style.display = DisplayStyle.None;
        if (_resultContainer != null) _resultContainer.style.display = DisplayStyle.Flex;

        if (_rulesButton != null) _rulesButton.style.display = DisplayStyle.Flex;
        if (_retryButton != null) _retryButton.style.display = DisplayStyle.Flex;
        if (_exitButton != null)
        {
            _exitButton.style.display = DisplayStyle.Flex;
            _exitButton.text = "Continue";
        }

        SetLabelText(_resultHeadlineLabel, headline);
        SetLabelText(_resultLabel, message);
        SetLabelText(_resultFactLabel, $"Fact: {fact}");
        SetLabelText(_resultScoreLabel, $"Pollution reduction: {reward}");
    }

    private void HandleRulesClicked(ClickEvent evt)
    {
        if (!_isResolving || _rulesPopup == null || _resultContainer == null)
        {
            return;
        }

        _resultContainer.style.display = DisplayStyle.None;
        _rulesPopup.style.display = DisplayStyle.Flex;
    }

    private void HandleBackClicked(ClickEvent evt)
    {
        if (!_isResolving || _rulesPopup == null || _resultContainer == null)
        {
            return;
        }

        _rulesPopup.style.display = DisplayStyle.None;
        _resultContainer.style.display = DisplayStyle.Flex;
    }

    private void HandleRetryClicked(ClickEvent evt)
    {
        if (!_isResolving)
        {
            return;
        }

        _isResolving = false;
        _isActive = true;
        _completionQueued = false;
        StartRun();
    }

    private void HandleExitClicked(ClickEvent evt)
    {
        if (!_isActive && !_isResolving)
        {
            return;
        }

        if (_tapArea != null)
        {
            _tapArea.UnregisterCallback<ClickEvent>(HandleTap);
        }

        DisableButtons();
        HideUI();

        bool finishingFromEndPage = _isResolving;
        _isActive = false;
        _isResolving = false;
        _hasStartedRun = false;
        _completionQueued = false;

        if (finishingFromEndPage)
        {
            OnMinigameComplete?.Invoke(_pendingReward);
        }
        else
        {
            OnMinigameExited?.Invoke();
        }
    }

    private void DisableButtons()
    {
        if (_beginButton != null)
        {
            _beginButton.UnregisterCallback<ClickEvent>(HandleBeginClicked);
            _beginButton.SetEnabled(false);
        }

        if (_rulesButton != null)
        {
            _rulesButton.UnregisterCallback<ClickEvent>(HandleRulesClicked);
            _rulesButton.SetEnabled(false);
        }

        if (_retryButton != null)
        {
            _retryButton.UnregisterCallback<ClickEvent>(HandleRetryClicked);
            _retryButton.SetEnabled(false);
        }

        if (_backButton != null)
        {
            _backButton.UnregisterCallback<ClickEvent>(HandleBackClicked);
            _backButton.SetEnabled(false);
        }

        if (_exitButton != null)
        {
            _exitButton.UnregisterCallback<ClickEvent>(HandleExitClicked);
            _exitButton.SetEnabled(false);
        }
    }

    private void SetLabelText(Label label, string value)
    {
        if (label == null)
        {
            return;
        }

        label.text = value;
        label.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void HideUI()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void ShowTapEffect(ClickEvent evt)
    {
        VisualElement card = _root?.Q("minigame_card");
        if (card != null)
        {
            card.AddToClassList("bike-card-flash");
            _root.schedule.Execute(() => card?.RemoveFromClassList("bike-card-flash")).StartingIn(120);
        }

        if (_tapArea == null)
        {
            return;
        }

        Vector2 localPos = evt.localPosition;
        const int glowSize = 80;
        float left = localPos.x - glowSize * 0.5f;
        float top = localPos.y - glowSize * 0.5f;

        var glow = new VisualElement();
        glow.AddToClassList("bike-click-glow");
        glow.style.left = new Length(left, LengthUnit.Pixel);
        glow.style.top = new Length(top, LengthUnit.Pixel);
        glow.pickingMode = PickingMode.Ignore;
        _tapArea.Add(glow);

        _root.schedule.Execute(() => glow.AddToClassList("bike-click-glow-out")).StartingIn(40);
        _root.schedule.Execute(() => glow.RemoveFromHierarchy()).StartingIn(350);
    }

    private void ConfigureRiderVisual()
    {
        Sprite resolvedSprite = playerSprite;
        HeroType hero = HeroType.Cyclist;

        GameController gc = GameObject.FindFirstObjectByType<GameController>();
        if (gc != null)
        {
            hero = gc.player != null ? gc.player.heroType : HeroType.Cyclist;
        }
        else if (GameManager.Instance != null && GameManager.Instance.SelectedHeroes != null && GameManager.Instance.SelectedHeroes.Length > 0)
        {
            hero = GameManager.Instance.SelectedHeroes[0];
        }

        if (resolvedSprite == null && GameManager.Instance != null)
        {
            CharacterSprites sprites = GameManager.Instance.GetCharacterSprites(hero);
            if (sprites != null)
            {
                resolvedSprite = sprites.idleSprite;
            }
        }

        Image riderImage = _root.Q<Image>("rider_image");
        VisualElement frame = _root.Q(className: "rb-frame");
        VisualElement wheelRear = _root.Q<VisualElement>("wheel_rear");
        VisualElement wheelFront = _root.Q<VisualElement>("wheel_front");

        bool spriteHasBike = hero == HeroType.Cyclist;

        if (riderImage != null)
        {
            switch (hero)
            {
                case HeroType.Cyclist:
                    riderImage.style.width = new Length(280, LengthUnit.Pixel);
                    riderImage.style.height = new Length(240, LengthUnit.Pixel);
                    riderImage.style.left = new Length(-30, LengthUnit.Pixel);
                    riderImage.style.bottom = new Length(-20, LengthUnit.Pixel);
                    break;
                case HeroType.Ranger:
                    riderImage.style.width = new Length(220, LengthUnit.Pixel);
                    riderImage.style.height = new Length(200, LengthUnit.Pixel);
                    riderImage.style.left = new Length(-20, LengthUnit.Pixel);
                    riderImage.style.bottom = new Length(-10, LengthUnit.Pixel);
                    break;
                case HeroType.Scientist:
                    riderImage.style.width = new Length(200, LengthUnit.Pixel);
                    riderImage.style.height = new Length(180, LengthUnit.Pixel);
                    riderImage.style.left = new Length(0, LengthUnit.Pixel);
                    riderImage.style.bottom = new Length(0, LengthUnit.Pixel);
                    break;
            }
        }

        if (riderImage != null)
        {
            if (resolvedSprite != null)
            {
                riderImage.sprite = resolvedSprite;
                riderImage.style.display = DisplayStyle.Flex;
                if (_riderSilhouette != null)
                {
                    _riderSilhouette.style.display = DisplayStyle.None;
                }

                DisplayStyle bikeDisplay = spriteHasBike ? DisplayStyle.None : DisplayStyle.Flex;
                if (frame != null) frame.style.display = bikeDisplay;
                if (wheelRear != null) wheelRear.style.display = bikeDisplay;
                if (wheelFront != null) wheelFront.style.display = bikeDisplay;
            }
            else
            {
                riderImage.style.display = DisplayStyle.None;
                if (_riderSilhouette != null)
                {
                    _riderSilhouette.style.display = DisplayStyle.Flex;
                }

                if (frame != null) frame.style.display = DisplayStyle.Flex;
                if (wheelRear != null) wheelRear.style.display = DisplayStyle.Flex;
                if (wheelFront != null) wheelFront.style.display = DisplayStyle.Flex;
            }
        }
    }
}

