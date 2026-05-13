using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MG.02 — "The Tree Planter" minigame.
///
/// Spec:
/// - Spawn a 5-second timer.
/// - Instruction: "Tap rapidly to plant trees!"
/// - Count clicks. If Clicks > 10: Remove 2 Pollution, play Birds Chirping sound.
///   If Clicks &lt; 10: Remove 1 Pollution.
/// - When landing on green: deep breath sound; screen glows green (optional, can add assets later).
///
/// Setup: Assign to BoardSquare12's minigameObject (MG.02).
/// </summary>
public class PlantTreesMinigame : MonoBehaviour, IMinigame
{
    public string MinigameName => "Plant Trees!";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => isActive;

    /// <summary>The UIDocument for this minigame's UI. Set in the Unity Editor.</summary>
    public UIDocument uiDocument;

    /// <summary>Time limit in seconds (spec: 5 seconds).</summary>
    public float timeLimit = 5f;

    /// <summary>Click count above this threshold gives -2 pollution and birds chirping (spec: > 10).</summary>
    public int highScoreThreshold = 10;

    private const int HIGH_REWARD = 2;
    private const int LOW_REWARD = 1;
    private const float MinPlantLeftPercent = 4f;
    private const float MaxPlantLeftPercent = 88f;
    private const float MinPlantBottomPercent = 4f;
    private const float MaxPlantBottomPercent = 30f;

    private bool isActive;
    private bool isResolving;
    private bool hasStartedRun;
    private VisualElement root;
    private VisualElement introContainer;
    private VisualElement gameContainer;
    private VisualElement resultContainer;
    private VisualElement rulesPopup;
    private VisualElement tapArea;
    private VisualElement gardenGround;
    private Label titleLabel;
    private Label descriptionLabel;
    private Label timerLabel;
    private Label treesCountLabel;
    private Label resultHeadlineLabel;
    private Label resultLabel;
    private Label resultScoreLabel;
    private Button beginButton;
    private Button rulesButton;
    private Button retryButton;
    private Button backButton;
    private Button exitButton;

    private float timeRemaining;
    private int clickCount;
    private int pendingPollutionReduction;
    private readonly List<VisualElement> plantedItems = new();

    private void Start()
    {
        HideUI();
    }

    private void LateUpdate()
    {
        if (!isActive && !isResolving && uiDocument != null && uiDocument.rootVisualElement != null
            && uiDocument.rootVisualElement.style.display != DisplayStyle.None)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void Update()
    {
        if (!isActive || isResolving || !hasStartedRun)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerDisplay();
            HandleTimerEnd();
            return;
        }

        UpdateTimerDisplay();
    }

    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("PlantTreesMinigame: UIDocument is not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("PlantTreesMinigame: rootVisualElement is null.");
            OnMinigameExited?.Invoke();
            return;
        }

        isActive = true;
        isResolving = false;
        hasStartedRun = false;
        pendingPollutionReduction = 0;

        CacheUi();
        RegisterCallbacks();
        ShowIntroState();

        root.style.display = DisplayStyle.Flex;
    }

    public int CalculatePollutionReduction(int clicks)
    {
        if (clicks > highScoreThreshold) return HIGH_REWARD;
        if (clicks > 0) return LOW_REWARD;
        return 0;
    }

    public int GetTreesPlanted() => clickCount;
    public float GetTimeRemaining() => timeRemaining;

    private void CacheUi()
    {
        titleLabel = root.Q<Label>("minigame_title");
        descriptionLabel = root.Q<Label>("minigame_description");
        introContainer = root.Q<VisualElement>("intro_container");
        gameContainer = root.Q<VisualElement>("game_container");
        resultContainer = root.Q<VisualElement>("result_container");
        rulesPopup = root.Q<VisualElement>("rules_popup");
        tapArea = root.Q<VisualElement>("tap_area");
        gardenGround = root.Q<VisualElement>("garden_ground");
        timerLabel = root.Q<Label>("timer_label");
        treesCountLabel = root.Q<Label>("trees_count_label");
        resultHeadlineLabel = root.Q<Label>("result_headline");
        resultLabel = root.Q<Label>("result_label");
        resultScoreLabel = root.Q<Label>("result_score");
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
        if (titleLabel != null)
        {
            titleLabel.text = "Welcome to The Tree Planter";
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Rules:\n1. Tap inside the green area to plant.\n2. Each tap adds one sprout in a random spot.\n3. When time ends, sprouts grow into trees.";
            descriptionLabel.style.display = DisplayStyle.Flex;
        }

        if (introContainer != null)
        {
            introContainer.style.display = DisplayStyle.Flex;
        }

        if (gameContainer != null)
        {
            gameContainer.style.display = DisplayStyle.None;
        }

        if (resultContainer != null)
        {
            resultContainer.style.display = DisplayStyle.None;
        }

        if (rulesPopup != null)
        {
            rulesPopup.style.display = DisplayStyle.None;
        }

        if (rulesButton != null)
        {
            rulesButton.style.display = DisplayStyle.None;
        }

        if (retryButton != null)
        {
            retryButton.style.display = DisplayStyle.None;
        }

        if (exitButton != null)
        {
            exitButton.style.display = DisplayStyle.Flex;
            exitButton.text = "Continue";
        }

        ResetRunState();
    }

    private void StartRun()
    {
        isActive = true;
        isResolving = false;
        hasStartedRun = true;
        ResetRunState();

        if (titleLabel != null)
        {
            titleLabel.text = "The Tree Planter";
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Tap rapidly to fill the garden with seedlings!";
            descriptionLabel.style.display = DisplayStyle.Flex;
        }

        if (introContainer != null)
        {
            introContainer.style.display = DisplayStyle.None;
        }

        if (gameContainer != null)
        {
            gameContainer.style.display = DisplayStyle.Flex;
        }

        if (resultContainer != null)
        {
            resultContainer.style.display = DisplayStyle.None;
        }

        if (rulesPopup != null)
        {
            rulesPopup.style.display = DisplayStyle.None;
        }

        if (rulesButton != null)
        {
            rulesButton.style.display = DisplayStyle.None;
        }

        if (retryButton != null)
        {
            retryButton.style.display = DisplayStyle.None;
        }

        if (exitButton != null)
        {
            exitButton.style.display = DisplayStyle.None;
        }

        if (tapArea != null)
        {
            tapArea.UnregisterCallback<ClickEvent>(HandleTap);
            tapArea.RegisterCallback<ClickEvent>(HandleTap);
        }
    }

    private void ResetRunState()
    {
        clickCount = 0;
        timeRemaining = timeLimit;
        pendingPollutionReduction = 0;
        ClearGarden();
        UpdateTimerDisplay();
        UpdateTreesCountDisplay();
    }

    private void HandleBeginClicked(ClickEvent evt)
    {
        if (!isActive || isResolving || hasStartedRun)
        {
            return;
        }

        StartRun();
    }

    private void HandleTap(ClickEvent evt)
    {
        if (!isActive || isResolving || !hasStartedRun)
        {
            return;
        }

        clickCount++;
        UpdateTreesCountDisplay();
        AddSeedlingPlaceholder();
        ShowTapVisualEffects(evt);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }
    }

    private void ShowTapVisualEffects(ClickEvent evt)
    {
        VisualElement card = root?.Q("minigame_card");
        if (card != null)
        {
            card.AddToClassList("pt-card-glow");
            root.schedule.Execute(() =>
            {
                card?.RemoveFromClassList("pt-card-glow");
            }).StartingIn(150);
        }

        if (tapArea == null) return;

        Vector2 localPos = evt.localPosition;
        const int glowSize = 80;
        float left = localPos.x - glowSize * 0.5f;
        float top = localPos.y - glowSize * 0.5f;

        var glow = new VisualElement();
        glow.AddToClassList("pt-click-glow");
        glow.style.left = new Length(left, LengthUnit.Pixel);
        glow.style.top = new Length(top, LengthUnit.Pixel);
        glow.pickingMode = PickingMode.Ignore;
        tapArea.Add(glow);

        root.schedule.Execute(() => glow.AddToClassList("pt-click-glow-out")).StartingIn(50);
        root.schedule.Execute(() => glow.RemoveFromHierarchy()).StartingIn(400);
    }

    private void HandleTimerEnd()
    {
        if (!isActive || isResolving || !hasStartedRun)
        {
            return;
        }

        if (tapArea != null)
        {
            tapArea.UnregisterCallback<ClickEvent>(HandleTap);
        }

        hasStartedRun = false;
        isActive = false;
        isResolving = true;
        pendingPollutionReduction = CalculatePollutionReduction(clickCount);
        GrowGardenIntoTrees();

        if (clickCount > highScoreThreshold && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBirdsChirping();
        }

        ShowResult();
    }

    private void ShowResult()
    {
        string headline = clickCount > highScoreThreshold
            ? "Amazing forest work!"
            : clickCount > 0
                ? "Nice planting run!"
                : "No trees planted this time";

        string message = clickCount > highScoreThreshold
            ? "Your garden grew into a happy little forest."
            : clickCount > 0
                ? "Your little plants grew into trees."
                : "Try tapping the green area faster on the next round.";

        if (descriptionLabel != null)
        {
            descriptionLabel.style.display = DisplayStyle.None;
        }

        if (introContainer != null)
        {
            introContainer.style.display = DisplayStyle.None;
        }

        if (gameContainer != null)
        {
            gameContainer.style.display = DisplayStyle.None;
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

        if (resultHeadlineLabel != null)
        {
            resultHeadlineLabel.text = headline;
        }

        if (resultLabel != null)
        {
            resultLabel.text = $"{message}\nSeedlings planted: {clickCount}";
        }

        if (resultScoreLabel != null)
        {
            resultScoreLabel.text = $"Pollution reduced by {pendingPollutionReduction}!";
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

        StartRun();
    }

    private void HandleExitClicked(ClickEvent evt)
    {
        if (!isActive && !isResolving)
        {
            return;
        }

        DisableButtons();

        if (tapArea != null)
        {
            tapArea.UnregisterCallback<ClickEvent>(HandleTap);
        }

        HideUI();
        bool finishingFromEndPage = isResolving;
        isActive = false;
        isResolving = false;
        hasStartedRun = false;

        if (finishingFromEndPage)
        {
            OnMinigameComplete?.Invoke(pendingPollutionReduction);
        }
        else
        {
            OnMinigameExited?.Invoke();
        }
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

    private void UpdateTimerDisplay()
    {
        if (timerLabel != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timerLabel.text = $"Time: {seconds}s";
            timerLabel.style.color = timeRemaining <= 2f ? new Color(1f, 0.3f, 0.3f) : Color.white;
        }
    }

    private void UpdateTreesCountDisplay()
    {
        if (treesCountLabel != null)
        {
            treesCountLabel.text = $"Trees: {clickCount}";
        }
    }

    private void AddSeedlingPlaceholder()
    {
        if (gardenGround == null)
        {
            return;
        }

        VisualElement plant = CreatePlantPlaceholder("pt-seedling");
        PlacePlantRandomly(plant);
        gardenGround.Add(plant);
        plantedItems.Add(plant);
    }

    private void GrowGardenIntoTrees()
    {
        for (int i = 0; i < plantedItems.Count; i++)
        {
            VisualElement plant = plantedItems[i];
            if (plant == null)
            {
                continue;
            }

            plant.RemoveFromClassList("pt-seedling");
            plant.AddToClassList("pt-tree-placeholder");

        }
    }

    private VisualElement CreatePlantPlaceholder(string stateClass)
    {
        VisualElement plant = new();
        plant.AddToClassList("pt-plant");
        plant.AddToClassList(stateClass);
        plant.pickingMode = PickingMode.Ignore;

        Label top = new() { name = "plant_top_label", text = string.Empty };
        top.AddToClassList("pt-plant-top");

        Label plantBase = new() { name = "plant_base_label", text = string.Empty };
        plantBase.AddToClassList("pt-plant-base");

        plant.Add(top);
        plant.Add(plantBase);
        return plant;
    }

    private void PlacePlantRandomly(VisualElement plant)
    {
        float leftPercent = UnityEngine.Random.Range(MinPlantLeftPercent, MaxPlantLeftPercent);
        float bottomPercent = UnityEngine.Random.Range(MinPlantBottomPercent, MaxPlantBottomPercent);
        float scale = UnityEngine.Random.Range(0.82f, 1.16f);

        plant.style.left = Length.Percent(leftPercent);
        plant.style.bottom = Length.Percent(bottomPercent);
        plant.style.scale = new Scale(new Vector3(scale, scale, 1f));
    }

    private void ClearGarden()
    {
        for (int i = 0; i < plantedItems.Count; i++)
        {
            plantedItems[i]?.RemoveFromHierarchy();
        }

        plantedItems.Clear();
        gardenGround?.Clear();
    }

    private void HideUI()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}

