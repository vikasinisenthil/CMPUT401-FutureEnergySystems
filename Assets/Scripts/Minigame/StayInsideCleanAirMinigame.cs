using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MG.01 — Stay Inside &amp; Clean The Air.
/// Several smoke puff <see cref="Image"/>s (Resources); each puff is cleared individually by tapping it.
/// </summary>
public class StayInsideCleanAirMinigame : MonoBehaviour, IMinigame
{
    public const string SmokePuffResourcePath = "Icons/stay_inside_smoke_puff";
    public const string WindGustResourcePath = "Icons/stay_inside_wind";

    private const string AirClearCelebrationMessage =
        "Great job! You cleared the smoke!";

    public const int SmokePuffCountMin = 6;
    public const int SmokePuffCountMax = 6;

    public string MinigameName => "Stay Inside & Clean the Air!";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => isActive;

    public UIDocument uiDocument;

    [Tooltip("Optional override if Resources smoke puff PNG is missing.")]
    [SerializeField] private Texture2D smokePuffTextureOverride;

    [Tooltip("Optional override for wind gust icons (defaults to Resources wind asset).")]
    [SerializeField] private Texture2D windGustTextureOverride;

    [Tooltip("Seconds to clear enough smoke.")]
    public float timeLimit = 12f;

    [Tooltip("At time-up, clearing at least this much air (percent) earns full reward. Round always runs until the timer ends.")]
    [Range(50f, 95f)]
    public float winClearPercentRequired = 75f;

    [Tooltip("At least this much cleared (percent) when time is up earns a small reward.")]
    [Range(0f, 90f)]
    public float partialRewardClearPercent = 40f;

    [Tooltip("How many taps are required to fully clear one smoke puff.")]
    [Min(1)]
    public int tapsPerCloudToClear = 8;

    [Tooltip("Legacy setting kept for compatibility; drag no longer clears smoke directly.")]
    public float windStrengthPerDragUnit = 0.004f;

    [Tooltip("Legacy setting kept for compatibility; individual cloud taps now use tapsPerCloudToClear.")]
    [Range(0.01f, 0.15f)]
    public float tapWindBurst = 0.065f;

    [Tooltip("Smoke drift: horizontal speed as percent of play area width per second. Keep low for a very slow haze.")]
    [Range(0.02f, 0.35f)]
    public float smokeDriftPercentPerSecond = 0.47f;


    /// <summary>1 = full smoke, 0 = all clear.</summary>
    private float smokeDensity = 1f;

    private bool isActive;
    private bool isResolving;
    private bool hasStartedRound;
    private int pendingPollutionReduction;
    private VisualElement root;
    private VisualElement introContainer;
    private VisualElement gameplayContainer;
    private VisualElement resultContainer;
    private VisualElement rulesPopup;
    private VisualElement roomFrame;
    private VisualElement smokeCloudsContainer;
    private VisualElement windElementsContainer;
    private readonly List<Image> smokePuffImages = new();
    private readonly List<Image> windImages = new();
    private Texture2D windGustTexture;
    private float[] smokePuffVanishBias;
    private float[] smokePuffLeftPercent;
    private float[] smokePuffTopPercent;
    private float[] smokePuffWidthPercent;
    private float[] smokePuffDriftSpeed;
    private int[] smokePuffTapCounts;
    /// <summary>+1 = drift right, -1 = drift left (bounces at playfield edges).</summary>
    private float[] smokePuffDriftSign;
    private Label descriptionLabel;
    private Label timerLabel;
    private Label progressLabel;
    private Label resultHeadlineLabel;
    private Label resultLabel;
    private Label resultFactLabel;
    private Label resultScoreLabel;
    private Label airClearCelebrationLabel;
    private float timeRemaining;
    private bool pointerDown;
    private bool draggedThisStroke;
    private float gustVisualBoost;

    private EventCallback<PointerDownEvent> _onPointerDown;
    private EventCallback<PointerMoveEvent> _onPointerMove;
    private EventCallback<PointerUpEvent> _onPointerUp;
    private EventCallback<ClickEvent> _onWindTap;

    private Button beginButton;
    private Button rulesButton;
    private Button retryButton;
    private Button backButton;
    private Button exitButton;

    public int SmokePuffInstanceCount => smokePuffImages.Count;

    void Start()
    {
        HideUI();
    }

    void LateUpdate()
    {
        if (!isActive && !isResolving && uiDocument != null && uiDocument.rootVisualElement != null
            && uiDocument.rootVisualElement.style.display != DisplayStyle.None)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    void Update()
    {
        if (!isActive || isResolving || !hasStartedRound) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerDisplay();
            FinishRound();
            return;
        }

        UpdateTimerDisplay();
        gustVisualBoost = Mathf.MoveTowards(gustVisualBoost, 0f, Time.deltaTime * 1.65f);

        if (smokeDensity > 0.02f)
            UpdateSmokeDrift();

        RefreshSmokeVisuals();
    }

    private const float SmokePuffFrameMarginPercent = 0.35f;

    private void UpdateSmokeDrift()
    {
        if (smokePuffImages.Count == 0 || smokePuffLeftPercent == null
            || smokePuffLeftPercent.Length != smokePuffImages.Count
            || smokePuffDriftSign == null || smokePuffDriftSign.Length != smokePuffImages.Count)
            return;

        float dt = Time.deltaTime;
        float margin = SmokePuffFrameMarginPercent;

        for (int i = 0; i < smokePuffImages.Count; i++)
        {
            float w = smokePuffWidthPercent[i];
            float left = smokePuffLeftPercent[i] + smokePuffDriftSpeed[i] * smokePuffDriftSign[i] * dt;
            float maxLeft = 100f - w - margin;

            if (left < margin)
            {
                left = margin;
                smokePuffDriftSign[i] = 1f;
            }
            else if (left > maxLeft)
            {
                left = maxLeft;
                smokePuffDriftSign[i] = -1f;
            }

            smokePuffLeftPercent[i] = left;

            Image img = smokePuffImages[i];
            img.style.left = new Length(left, LengthUnit.Percent);
            img.style.top = new Length(smokePuffTopPercent[i], LengthUnit.Percent);
            img.style.right = StyleKeyword.Auto;
        }
    }

    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("StayInsideCleanAirMinigame: UIDocument is not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("StayInsideCleanAirMinigame: rootVisualElement is null.");
            OnMinigameExited?.Invoke();
            return;
        }

        isActive = true;
        isResolving = false;
        hasStartedRound = false;
        pendingPollutionReduction = 0;

        root.style.display = DisplayStyle.Flex;

        CacheUi();
        RegisterCallbacks();
        ShowIntroState();
    }

    private void CacheUi()
    {
        descriptionLabel = root.Q<Label>("minigame_description");
        introContainer = root.Q<VisualElement>("intro_container");
        gameplayContainer = root.Q<VisualElement>("gameplay_container");
        resultContainer = root.Q<VisualElement>("result_container");
        rulesPopup = root.Q<VisualElement>("rules_popup");

        roomFrame = root.Q<VisualElement>("room_frame");
        smokeCloudsContainer = root.Q<VisualElement>("smoke_clouds_container");
        windElementsContainer = root.Q<VisualElement>("wind_elements_container");
        timerLabel = root.Q<Label>("timer_label");
        progressLabel = root.Q<Label>("progress_label");
        resultHeadlineLabel = root.Q<Label>("result_headline");
        resultLabel = root.Q<Label>("result_label");
        resultFactLabel = root.Q<Label>("result_fact");
        resultScoreLabel = root.Q<Label>("result_score");
        airClearCelebrationLabel = root.Q<Label>("air_clear_celebration_label");

        beginButton = root.Q<Button>("begin_button");
        rulesButton = root.Q<Button>("rules_button");
        retryButton = root.Q<Button>("retry_button");
        backButton = root.Q<Button>("back_button");
        exitButton = root.Q<Button>("exit_button");
    }

    private void RegisterCallbacks()
    {
        if (beginButton != null)
        {
            beginButton.UnregisterCallback<ClickEvent>(HandleBeginClicked);
            beginButton.SetEnabled(true);
            beginButton.RegisterCallback<ClickEvent>(HandleBeginClicked);
        }

        if (rulesButton != null)
        {
            rulesButton.UnregisterCallback<ClickEvent>(HandleRulesClicked);
            rulesButton.SetEnabled(true);
            rulesButton.RegisterCallback<ClickEvent>(HandleRulesClicked);
        }

        if (retryButton != null)
        {
            retryButton.UnregisterCallback<ClickEvent>(HandleRetryClicked);
            retryButton.SetEnabled(true);
            retryButton.RegisterCallback<ClickEvent>(HandleRetryClicked);
        }

        if (backButton != null)
        {
            backButton.UnregisterCallback<ClickEvent>(HandleBackClicked);
            backButton.SetEnabled(true);
            backButton.RegisterCallback<ClickEvent>(HandleBackClicked);
        }

        if (exitButton != null)
        {
            exitButton.UnregisterCallback<ClickEvent>(HandleExitClicked);
            exitButton.SetEnabled(true);
            exitButton.RegisterCallback<ClickEvent>(HandleExitClicked);
        }
    }

    private void ShowIntroState()
    {
        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Rules:\n1. Tap the smoke clouds.\n2. Keep tapping each one until it fades.\n3. Clear as many as you can before time is up.";
            descriptionLabel.style.display = DisplayStyle.Flex;
            descriptionLabel.style.fontSize = 20f;
            descriptionLabel.style.marginTop = 0f;
            descriptionLabel.style.marginBottom = 18f;
            descriptionLabel.style.maxWidth = 760f;
            descriptionLabel.style.paddingLeft = 72f;
            descriptionLabel.style.paddingRight = 72f;
            descriptionLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        }

        if (introContainer != null)
        {
            introContainer.style.display = DisplayStyle.Flex;
        }

        if (gameplayContainer != null)
        {
            gameplayContainer.style.display = DisplayStyle.None;
        }

        if (resultContainer != null)
        {
            resultContainer.style.display = DisplayStyle.None;
        }

        if (rulesPopup != null)
        {
            rulesPopup.style.display = DisplayStyle.None;
        }

        if (resultLabel != null)
        {
            resultLabel.text = string.Empty;
            resultLabel.style.display = DisplayStyle.None;
        }

        if (exitButton != null)
        {
            exitButton.style.display = DisplayStyle.Flex;
            exitButton.text = "Exit";
        }

        if (retryButton != null)
        {
            retryButton.style.display = DisplayStyle.None;
        }
    }

    private void HandleBeginClicked(ClickEvent evt)
    {
        if (!isActive || isResolving || hasStartedRound)
        {
            return;
        }

        if (beginButton != null)
        {
            beginButton.UnregisterCallback<ClickEvent>(HandleBeginClicked);
            beginButton.SetEnabled(false);
        }

        StartRound();
    }

    private void StartRound()
    {
        smokeDensity = 1f;
        isActive = true;
        isResolving = false;
        hasStartedRound = true;
        pendingPollutionReduction = 0;
        pointerDown = false;
        draggedThisStroke = false;
        timeRemaining = timeLimit;
        gustVisualBoost = 0f;

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Tap each smoke cloud directly to clear it.";
            descriptionLabel.style.display = DisplayStyle.Flex;
            descriptionLabel.style.fontSize = 18f;
            descriptionLabel.style.marginTop = -15f;
            descriptionLabel.style.marginBottom = 10f;
            descriptionLabel.style.maxWidth = 500f;
            descriptionLabel.style.paddingLeft = 0f;
            descriptionLabel.style.paddingRight = 0f;
            descriptionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        if (introContainer != null)
        {
            introContainer.style.display = DisplayStyle.None;
        }

        if (gameplayContainer != null)
        {
            gameplayContainer.style.display = DisplayStyle.Flex;
        }

        if (roomFrame != null)
        {
            roomFrame.pickingMode = PickingMode.Position;
        }

        if (resultContainer != null)
        {
            resultContainer.style.display = DisplayStyle.None;
        }

        if (rulesPopup != null)
        {
            rulesPopup.style.display = DisplayStyle.None;
        }

        if (exitButton != null)
        {
            exitButton.style.display = DisplayStyle.None;
        }

        if (retryButton != null)
        {
            retryButton.style.display = DisplayStyle.None;
        }

        ClearWindLayer();
        ResolveWindTexture();
        RebuildSmokePuffs();
        HideAirClearCelebration();

        RefreshSmokeVisuals();
        UpdateProgressLabel();
        UpdateTimerDisplay();

        if (roomFrame != null)
        {
            _onPointerDown = OnPointerDown;
            _onPointerMove = OnPointerMove;
            _onPointerUp = OnPointerUp;
            _onWindTap = OnWindTap;

            roomFrame.RegisterCallback(_onPointerDown);
            roomFrame.RegisterCallback(_onPointerMove);
            roomFrame.RegisterCallback(_onPointerUp);
            roomFrame.RegisterCallback(_onWindTap);
        }

        root.schedule.Execute(RefreshSmokeVisuals).StartingIn(0);

        Debug.Log("StayInsideCleanAirMinigame: Tap or drag to clear smoke in the room.");
    }

    private void RebuildSmokePuffs()
    {
        smokePuffImages.Clear();
        if (smokeCloudsContainer == null) return;

        smokeCloudsContainer.Clear();

        Texture2D tex = smokePuffTextureOverride;
        if (tex == null)
        {
            tex = Resources.Load<Texture2D>(SmokePuffResourcePath);
            if (tex == null)
            {
                var sprite = Resources.Load<Sprite>(SmokePuffResourcePath);
                if (sprite != null)
                    tex = sprite.texture;
            }
        }

        if (tex == null)
        {
            Debug.LogWarning(
                $"StayInsideCleanAirMinigame: Missing Resources/{SmokePuffResourcePath}.png or smokePuffTextureOverride.");
            return;
        }

        int count = UnityEngine.Random.Range(SmokePuffCountMin, SmokePuffCountMax + 1);
        smokePuffVanishBias = new float[count];
        smokePuffLeftPercent = new float[count];
        smokePuffTopPercent = new float[count];
        smokePuffWidthPercent = new float[count];
        smokePuffDriftSpeed = new float[count];
        smokePuffDriftSign = new float[count];
        smokePuffTapCounts = new int[count];
        /* Middle-right column — inset leaves a wider strip for the far-right puff (i==1). */
        float midRightColumnLeft = UnityEngine.Random.Range(48f, 52f);
        const float midRightMaxWidthPercent = 21f;

        for (int i = 0; i < count; i++)
        {
            var img = new Image();
            img.AddToClassList("sica-smoke-puff-instance");
            img.pickingMode = PickingMode.Ignore;
            img.image = tex;
            img.scaleMode = ScaleMode.ScaleToFit;
            img.style.position = Position.Absolute;

            float size;
            if (i == 0)
                size = UnityEngine.Random.Range(22f, 30f);
            else if (i == 1)
                /* Far right — large; clamped to available strip so it stays clear of the middle-right pair. */
                size = UnityEngine.Random.Range(45f, 49f);
            else if (i == 2)
                size = UnityEngine.Random.Range(19f, 22f);
            else if (i == 3)
                size = UnityEngine.Random.Range(22f, 30f);
            else if (i == 4)
                size = UnityEngine.Random.Range(19f, 22f);
            else if (i == 5)
                /* Middle-left bridge puff — large; sized so it stays left of the middle-right column. */
                size = UnityEngine.Random.Range(36f, 40f);
            else
                size = UnityEngine.Random.Range(20f, 32f);

            img.style.width = new Length(size, LengthUnit.Percent);
            img.style.height = new Length(size, LengthUnit.Percent);

            float leftPct;
            float topPct;

            if (i == 0)
            {
                leftPct = UnityEngine.Random.Range(-2f, 2f);
                topPct = UnityEngine.Random.Range(1f, 6f);
                img.style.left = new Length(leftPct, LengthUnit.Percent);
                img.style.top = new Length(topPct, LengthUnit.Percent);
                img.style.right = StyleKeyword.Auto;
            }
            else if (i == 1)
            {
                /* Right strip: past the middle column; size capped by remaining width, then hugged to the right edge. */
                float margin = UnityEngine.Random.Range(0.5f, 2f);
                float minLeft = midRightColumnLeft + midRightMaxWidthPercent + 0.85f;
                float leftEdge = Mathf.Max(minLeft, 100f - size - margin);
                float maxW = Mathf.Max(0f, 100f - leftEdge - margin);
                size = Mathf.Min(size, maxW);
                leftEdge = Mathf.Max(minLeft, 100f - size - margin);
                topPct = UnityEngine.Random.Range(54f, 62f);

                img.style.width = new Length(size, LengthUnit.Percent);
                img.style.height = new Length(size, LengthUnit.Percent);
                img.style.left = new Length(leftEdge, LengthUnit.Percent);
                img.style.right = StyleKeyword.Auto;
                img.style.top = new Length(topPct, LengthUnit.Percent);
                leftPct = leftEdge;
            }
            else if (i == 2)
            {
                /* Middle-right upper — ~35% from top; column ~48–52% left. */
                leftPct = midRightColumnLeft;
                topPct = UnityEngine.Random.Range(33f, 37f);
                img.style.left = new Length(leftPct, LengthUnit.Percent);
                img.style.top = new Length(topPct, LengthUnit.Percent);
                img.style.right = StyleKeyword.Auto;
            }
            else if (i == 3)
            {
                /* Bottom-left anchor — keeps the extra puff away from the top-left (i==0). */
                leftPct = UnityEngine.Random.Range(-2f, 6f);
                topPct = UnityEngine.Random.Range(48f, 58f);
                img.style.left = new Length(leftPct, LengthUnit.Percent);
                img.style.top = new Length(topPct, LengthUnit.Percent);
                img.style.right = StyleKeyword.Auto;
            }
            else if (i == 4)
            {
                /* Middle-right lower — ~65% from top; same x as i==2. */
                leftPct = midRightColumnLeft;
                topPct = UnityEngine.Random.Range(63f, 67f);
                img.style.left = new Length(leftPct, LengthUnit.Percent);
                img.style.top = new Length(topPct, LengthUnit.Percent);
                img.style.right = StyleKeyword.Auto;
            }
            else if (i == 5)
            {
                /* Between left pair and middle-right column — visual mass ~35–40% across the playfield. */
                leftPct = UnityEngine.Random.Range(12f, 18f);
                topPct = UnityEngine.Random.Range(34f, 44f);
                img.style.left = new Length(leftPct, LengthUnit.Percent);
                img.style.top = new Length(topPct, LengthUnit.Percent);
                img.style.right = StyleKeyword.Auto;
            }
            else
            {
                float rl;
                float rt;
                int guard = 0;
                do
                {
                    rl = UnityEngine.Random.Range(4f, 72f);
                    rt = UnityEngine.Random.Range(4f, 58f);
                    guard++;
                } while (guard < 24 && ((rl < 20f && rt < 18f) || (rl < 14f && rt > 44f)));
                leftPct = rl;
                topPct = rt;
                img.style.left = new Length(rl, LengthUnit.Percent);
                img.style.top = new Length(rt, LengthUnit.Percent);
                img.style.right = StyleKeyword.Auto;
            }

            smokePuffLeftPercent[i] = leftPct;
            smokePuffTopPercent[i] = topPct;
            smokePuffWidthPercent[i] = size;
            smokePuffDriftSpeed[i] = smokeDriftPercentPerSecond * UnityEngine.Random.Range(0.72f, 1.28f);
            smokePuffDriftSign[i] = 1f;

            img.style.rotate = new Rotate(new Angle(UnityEngine.Random.Range(-18f, 18f)));

            smokePuffVanishBias[i] = 0.55f + 0.35f * ((i * 0.17f) % 1f);
            smokePuffTapCounts[i] = 0;

            smokeCloudsContainer.Add(img);
            smokePuffImages.Add(img);
        }
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (!isActive || isResolving || roomFrame == null) return;
        pointerDown = true;
        draggedThisStroke = false;
        BoostGustVisual(0.12f);
        roomFrame.CapturePointer(evt.pointerId);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isActive || isResolving || !pointerDown) return;

        float mag = evt.deltaPosition.magnitude;
        if (mag < 0.25f) return;
        if (mag > 2f) draggedThisStroke = true;

        BoostGustVisual(mag * 0.02f);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (roomFrame == null) return;
        roomFrame.ReleasePointer(evt.pointerId);
        pointerDown = false;
    }

    private void OnWindTap(ClickEvent evt)
    {
        if (!isActive || isResolving) return;
        if (draggedThisStroke)
        {
            draggedThisStroke = false;
            return;
        }

        TryTapSmokeAtPosition(evt.position);
    }

    private void BoostGustVisual(float amount)
    {
        gustVisualBoost = Mathf.Clamp01(gustVisualBoost + amount);
    }

    private void TryTapSmokeAtPosition(Vector2 panelPosition)
    {
        int puffIndex = FindTopmostSmokePuffAtPosition(panelPosition);
        if (puffIndex < 0)
        {
            return;
        }

        BoostGustVisual(0.45f);
        smokePuffTapCounts[puffIndex] = Mathf.Min(tapsPerCloudToClear, smokePuffTapCounts[puffIndex] + 1);
        UpdateAggregateSmokeDensity();
        RefreshSmokeVisuals();
        UpdateProgressLabel();
        PlayWindFeedback();
    }

    private int FindTopmostSmokePuffAtPosition(Vector2 panelPosition)
    {
        if (smokePuffImages == null || smokePuffTapCounts == null)
        {
            return -1;
        }

        for (int i = smokePuffImages.Count - 1; i >= 0; i--)
        {
            Image img = smokePuffImages[i];
            if (img == null)
            {
                continue;
            }

            if (i >= smokePuffTapCounts.Length || smokePuffTapCounts[i] >= tapsPerCloudToClear)
            {
                continue;
            }

            if (img.worldBound.Contains(panelPosition))
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateAggregateSmokeDensity()
    {
        int total = smokePuffImages.Count;
        if (total <= 0 || tapsPerCloudToClear <= 0 || smokePuffTapCounts == null)
        {
            smokeDensity = 0f;
            return;
        }

        float remainingTapWeight = 0f;
        for (int i = 0; i < total; i++)
        {
            int taps = i < smokePuffTapCounts.Length ? smokePuffTapCounts[i] : 0;
            remainingTapWeight += Mathf.Clamp(tapsPerCloudToClear - taps, 0, tapsPerCloudToClear);
        }

        smokeDensity = Mathf.Clamp01(remainingTapWeight / (total * tapsPerCloudToClear));
    }

    private void PlayWindFeedback()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayConfirm();
    }

    private void ResolveWindTexture()
    {
        windGustTexture = windGustTextureOverride;
        if (windGustTexture == null)
        {
            windGustTexture = Resources.Load<Texture2D>(WindGustResourcePath);
            if (windGustTexture == null)
            {
                var sprite = Resources.Load<Sprite>(WindGustResourcePath);
                if (sprite != null)
                    windGustTexture = sprite.texture;
            }
        }

        if (windGustTexture == null)
        {
            Debug.LogWarning(
                $"StayInsideCleanAirMinigame: Missing Resources/{WindGustResourcePath}.png or windGustTextureOverride.");
        }
    }

    private void ClearWindLayer()
    {
        if (windElementsContainer != null)
            windElementsContainer.Clear();
        windImages.Clear();
    }

    /* x = left %, y = top %, z = width/height % — matches reference layout: varied sizes & spread. */
    private static readonly Vector3[] WindLayoutSlots =
    {
        new Vector3(8f, 10f, 20f),   /* top-left — medium */
        new Vector3(11f, 54f, 27f),   /* bottom-left — larger, slightly inset */
        new Vector3(51f, 20f, 15f),  /* center-right — small, upper-middle */
        new Vector3(69f, 36f, 28f),   /* far right — largest, vertically centered band */
    };

    private void SpawnWindElementsWhenAirFullyClear()
    {
        if (windElementsContainer == null || windGustTexture == null)
            return;

        ClearWindLayer();

        for (int k = 0; k < WindLayoutSlots.Length; k++)
        {
            Vector3 slot = WindLayoutSlots[k];
            float w = slot.z;
            var img = new Image();
            img.AddToClassList("sica-wind-instance");
            img.pickingMode = PickingMode.Ignore;
            img.image = windGustTexture;
            img.scaleMode = ScaleMode.ScaleToFit;
            img.style.position = Position.Absolute;
            img.style.width = new Length(w, LengthUnit.Percent);
            img.style.height = new Length(w, LengthUnit.Percent);
            img.style.left = new Length(slot.x, LengthUnit.Percent);
            img.style.top = new Length(slot.y, LengthUnit.Percent);
            img.style.right = StyleKeyword.Auto;
            img.style.opacity = 0.92f;
            img.style.visibility = Visibility.Visible;
            img.style.rotate = new Rotate(new Angle(0f));

            windElementsContainer.Add(img);
            windImages.Add(img);
        }

        ShowAirClearCelebration();
    }

    private void ShowAirClearCelebration()
    {
        if (airClearCelebrationLabel == null)
            return;
        airClearCelebrationLabel.text = AirClearCelebrationMessage;
        airClearCelebrationLabel.style.display = DisplayStyle.Flex;
    }

    private void HideAirClearCelebration()
    {
        if (airClearCelebrationLabel == null)
            return;
        airClearCelebrationLabel.text = "";
        airClearCelebrationLabel.style.display = DisplayStyle.None;
    }

    private void SyncWindLayer()
    {
        if (windElementsContainer == null || windGustTexture == null || !isActive)
            return;

        /* Same threshold as smoke fully hidden in RefreshSmokeVisuals. */
        bool airFullyClear = smokeDensity <= 0.02f;

        if (!airFullyClear)
        {
            if (windImages.Count > 0)
                ClearWindLayer();
            return;
        }

        if (windImages.Count == 0)
            SpawnWindElementsWhenAirFullyClear();
        else
        {
            for (int i = 0; i < windImages.Count; i++)
            {
                windImages[i].style.opacity = 0.92f;
                windImages[i].style.visibility = Visibility.Visible;
            }
        }
    }

    public float GetClearedPercent() => (1f - smokeDensity) * 100f;

    public float GetSmokeDensity() => smokeDensity;

    public int CalculatePollutionReduction()
    {
        return ComputePollutionReductionForCleared(
            GetClearedPercent(), winClearPercentRequired, partialRewardClearPercent);
    }

    public static int ComputePollutionReductionForCleared(
        float clearedPercent, float winAtPercent, float partialAtPercent)
    {
        if (clearedPercent + 0.001f >= winAtPercent) return 2;
        if (clearedPercent + 0.001f >= partialAtPercent) return 1;
        return 0;
    }

    private void RefreshSmokeVisuals()
    {
        SyncWindLayer();

        if (smokePuffImages.Count == 0) return;

        for (int i = 0; i < smokePuffImages.Count; i++)
        {
            Image img = smokePuffImages[i];
            int taps = smokePuffTapCounts != null && i < smokePuffTapCounts.Length
                ? smokePuffTapCounts[i]
                : 0;
            float remaining = 1f - Mathf.Clamp01((float)taps / Mathf.Max(1, tapsPerCloudToClear));
            float vanish = smokePuffVanishBias != null && i < smokePuffVanishBias.Length
                ? smokePuffVanishBias[i]
                : 1f;
            bool pushing = pointerDown || gustVisualBoost > 0.12f;
            float op = Mathf.Pow(remaining, 0.48f + 0.2f * vanish) * (pushing ? 1.05f : 1f);
            op = Mathf.Clamp01(op);

            if (remaining <= 0.001f)
            {
                img.style.opacity = 0f;
                img.style.visibility = Visibility.Hidden;
            }
            else
            {
                img.style.visibility = Visibility.Visible;
                img.style.opacity = op;
            }
        }
    }

    private void UpdateProgressLabel()
    {
        if (progressLabel != null)
            progressLabel.text = $"Air cleared: {Mathf.Clamp(Mathf.RoundToInt(GetClearedPercent()), 0, 100)}% / {(int)winClearPercentRequired}% to win";
    }

    private void UpdateTimerDisplay()
    {
        if (timerLabel != null)
        {
            int seconds = Mathf.CeilToInt(timeRemaining);
            timerLabel.text = $"Time: {seconds}s";
            timerLabel.style.color = timeRemaining <= 3f ? new Color(1f, 0.35f, 0.35f) : Color.white;
        }
    }

    private void FinishRound()
    {
        if (!isActive || isResolving) return;

        TeardownInput();
        HideAirClearCelebration();

        int reduction = ComputePollutionReductionForCleared(
            GetClearedPercent(), winClearPercentRequired, partialRewardClearPercent);
        bool clearedEnough = GetClearedPercent() + 0.001f >= winClearPercentRequired;

        isActive = false;
        isResolving = true;
        hasStartedRound = false;
        pendingPollutionReduction = reduction;

        string headline;
        string message;
        string fact;
        if (clearedEnough)
        {
            headline = "Air is clear";
            message = "You finished in time.";
            fact = "Clean air helps us feel better.";
        }
        else if (reduction == 1)
        {
            headline = "Good progress";
            message = "Good try! You cleared some smoke.";
            fact = "Even a little less smoke helps.";
        }
        else
        {
            headline = "Too much smoke left";
            message = "The smoke is still strong. Try again next time.";
            fact = "Too much smoke can make it hard to breathe.";
        }

        ShowResult(headline, message, fact, reduction);
    }

    private void TeardownInput()
    {
        if (roomFrame != null && _onPointerDown != null)
        {
            roomFrame.UnregisterCallback(_onPointerDown);
            roomFrame.UnregisterCallback(_onPointerMove);
            roomFrame.UnregisterCallback(_onPointerUp);
            roomFrame.UnregisterCallback(_onWindTap);
        }

        if (roomFrame != null)
        {
            roomFrame.ReleasePointer(PointerId.mousePointerId);
            for (int i = 0; i < PointerId.touchPointerCount; i++)
            {
                roomFrame.ReleasePointer(PointerId.touchPointerIdBase + i);
            }
        }

        _onPointerDown = null;
        _onPointerMove = null;
        _onPointerUp = null;
        _onWindTap = null;

        pointerDown = false;
    }

    private void HandleExitClicked(ClickEvent evt)
    {
        if (!isActive && !isResolving) return;

        TeardownInput();
        DisableButtons();
        HideUI();
        bool finishingFromEndPage = isResolving;
        isActive = false;
        isResolving = false;
        hasStartedRound = false;

        if (finishingFromEndPage)
        {
            OnMinigameComplete?.Invoke(pendingPollutionReduction);
        }
        else
        {
            OnMinigameExited?.Invoke();
        }
    }

    private void HandleRulesClicked(ClickEvent evt)
    {
        if (!isResolving || rulesPopup == null || resultContainer == null)
        {
            return;
        }

        resultContainer.style.display = DisplayStyle.None;
        rulesPopup.style.display = DisplayStyle.Flex;
    }

    private void HandleBackClicked(ClickEvent evt)
    {
        if (!isResolving || rulesPopup == null || resultContainer == null)
        {
            return;
        }

        rulesPopup.style.display = DisplayStyle.None;
        resultContainer.style.display = DisplayStyle.Flex;
    }

    private void HandleRetryClicked(ClickEvent evt)
    {
        if (!isResolving)
        {
            return;
        }

        isResolving = false;
        isActive = true;
        StartRound();
    }

    private void ShowResult(string headline, string message, string fact, int reduction)
    {
        if (descriptionLabel != null)
        {
            descriptionLabel.text = string.Empty;
            descriptionLabel.style.display = DisplayStyle.None;
        }

        if (introContainer != null)
        {
            introContainer.style.display = DisplayStyle.None;
        }

        if (gameplayContainer != null)
        {
            gameplayContainer.style.display = DisplayStyle.None;
        }

        if (roomFrame != null)
        {
            roomFrame.pickingMode = PickingMode.Ignore;
        }

        if (resultContainer != null)
        {
            resultContainer.style.display = DisplayStyle.Flex;
        }

        if (rulesPopup != null)
        {
            rulesPopup.style.display = DisplayStyle.None;
        }

        if (rulesButton != null)
        {
            rulesButton.style.display = DisplayStyle.Flex;
        }

        if (retryButton != null)
        {
            retryButton.style.display = DisplayStyle.Flex;
        }

        if (exitButton != null)
        {
            exitButton.style.display = DisplayStyle.Flex;
            exitButton.text = "Continue";
        }

        SetLabelText(resultHeadlineLabel, headline);
        SetLabelText(resultLabel, message);
        SetLabelText(resultFactLabel, $"Fact: {fact}");
        SetLabelText(resultScoreLabel, $"Pollution reduction: {reduction}");
    }

    private void DisableButtons()
    {
        if (beginButton != null)
        {
            beginButton.UnregisterCallback<ClickEvent>(HandleBeginClicked);
            beginButton.SetEnabled(false);
        }

        if (rulesButton != null)
        {
            rulesButton.UnregisterCallback<ClickEvent>(HandleRulesClicked);
            rulesButton.SetEnabled(false);
        }

        if (retryButton != null)
        {
            retryButton.UnregisterCallback<ClickEvent>(HandleRetryClicked);
            retryButton.SetEnabled(false);
        }

        if (backButton != null)
        {
            backButton.UnregisterCallback<ClickEvent>(HandleBackClicked);
            backButton.SetEnabled(false);
        }

        if (exitButton != null)
        {
            exitButton.UnregisterCallback<ClickEvent>(HandleExitClicked);
            exitButton.SetEnabled(false);
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
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }
}


