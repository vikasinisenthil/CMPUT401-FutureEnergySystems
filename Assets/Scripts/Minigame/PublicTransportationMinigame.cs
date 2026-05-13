using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// MG.03 - Public Transportation.
/// The player switches between the car, bus, and metro lanes with up/down controls,
/// avoids obstacles moving right-to-left, and the lane they survive in becomes the chosen transport.
/// </summary>
public class PublicTransportationMinigame : MonoBehaviour, IMinigame
{
    public enum TransportChoice
    {
        Car,
        Bus,
        Metro
    }

    public struct TransportOutcome
    {
        public TransportOutcome(
            TransportChoice choice,
            int pollutionReduction,
            string headline,
            string message,
            string fact)
        {
            Choice = choice;
            PollutionReduction = pollutionReduction;
            Headline = headline;
            Message = message;
            Fact = fact;
        }

        public TransportChoice Choice { get; }
        public int PollutionReduction { get; }
        public string Headline { get; }
        public string Message { get; }
        public string Fact { get; }
    }

    private sealed class ObstacleData
    {
        public int LaneIndex;
        public float Progress;
        public VisualElement Element;
    }

    public string MinigameName => "Public Transportation";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => isActive;

    public UIDocument uiDocument;
    public float survivalDuration = 10f;
    public float obstacleSpawnInterval = 0.7f;
    public float obstacleTravelDuration = 2.2f;
    public int busReduction = 1;
    public int bikeReduction = 2;
    public int resultDisplayMilliseconds = 1800;

    private static readonly string[] LaneNames = { "Car", "Bus", "Metro" };
    private static readonly string[] PlayerLaneClasses = { "transport-player-car", "transport-player-bus", "transport-player-metro" };
    private static readonly string[] ObstacleClasses =
    {
        "transport-obstacle-stroller",
        "transport-obstacle-tree",
        "transport-obstacle-human",
        "transport-obstacle-trash",
        "transport-obstacle-wheel"
    };
    private const float PlayerLeftPercent = 6f;
    private const float ObstacleStartLeftPercent = 75f;
    private const float ObstacleEndLeftPercent = 6f;

    private bool isActive;
    private bool isResolving;
    private bool hasStartedRun;
    private VisualElement root;
    private VisualElement introContainer;
    private VisualElement hudRow;
    private VisualElement laneHeaderRow;
    private VisualElement laneArena;
    private VisualElement[] laneElements;
    private VisualElement playerMarker;
    private VisualElement resultContainer;
    private VisualElement rulesPopup;
    private Label titleLabel;
    private Label descriptionLabel;
    private Label timerLabel;
    private Label laneLabel;
    private Label resultHeadlineLabel;
    private Label resultMessageLabel;
    private Label resultFactLabel;
    private Label resultScoreLabel;
    private Button beginButton;
    private Button rulesButton;
    private Button retryButton;
    private Button backButton;
    private Button exitButton;

    private readonly List<ObstacleData> activeObstacles = new();
    private float timeRemaining;
    private float spawnTimer;
    private int currentLaneIndex = 1;
    private int pendingPollutionReduction;
    private Vector2 swipeStartPosition;
    private bool swipeTrackingActive;

    private const float SwipeThresholdPixels = 60f;

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

        HandleKeyboardInput();
        HandleSwipeInput();

        timeRemaining -= Time.deltaTime;
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnObstacle();
            spawnTimer = obstacleSpawnInterval;
        }

        UpdateObstacles(Time.deltaTime);
        UpdateHud();

        if (HasCollision())
        {
            ResolveCrash();
            return;
        }

        if (timeRemaining <= 0f)
        {
            ResolveSuccess();
        }
    }

    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("PublicTransportationMinigame: UIDocument is not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("PublicTransportationMinigame: rootVisualElement is null.");
            OnMinigameExited?.Invoke();
            return;
        }

        isActive = true;
        isResolving = false;
        hasStartedRun = false;
        CacheUi();
        ShowIntroState();
        RegisterCallbacks();

        root.style.display = DisplayStyle.Flex;
    }

    public void MovePlayerLane(int delta)
    {
        currentLaneIndex = Mathf.Clamp(currentLaneIndex + delta, 0, 2);
        UpdatePlayerVisual();
        UpdateHud();
    }

    public int GetCurrentLaneIndex()
    {
        return currentLaneIndex;
    }

    public TransportChoice GetChoiceForLane(int laneIndex)
    {
        return laneIndex switch
        {
            0 => TransportChoice.Car,
            1 => TransportChoice.Bus,
            _ => TransportChoice.Metro
        };
    }

    public int CalculatePollutionReduction(TransportChoice choice)
    {
        return choice switch
        {
            TransportChoice.Bus => busReduction,
            TransportChoice.Metro => bikeReduction,
            _ => 0
        };
    }

    public TransportOutcome BuildOutcome(TransportChoice choice)
    {
        return choice switch
        {
            TransportChoice.Car => new TransportOutcome(
                choice,
                0,
                "Car ride done",
                "You made it. Cars can make the air dirtier.",
                "Cars can add lots of dirty air."),
            TransportChoice.Bus => new TransportOutcome(
                choice,
                busReduction,
                "Bus ride done",
                "Nice choice. Riding together helps keep air cleaner.",
                "One bus can take many people at once."),
            _ => new TransportOutcome(
                choice,
                bikeReduction,
                "Metro ride done",
                "Great choice. Metro rides help keep air cleaner.",
                "Metro trains can move lots of people with less dirty air.")
        };
    }

    private void CacheUi()
    {
        titleLabel = root.Q<Label>("minigame_title");
        descriptionLabel = root.Q<Label>("minigame_description");
        timerLabel = root.Q<Label>("timer_label");
        laneLabel = root.Q<Label>("lane_label");
        introContainer = root.Q<VisualElement>("intro_container");
        hudRow = root.Q<VisualElement>("hud_row");
        laneHeaderRow = root.Q<VisualElement>("lane_header_row");
        laneArena = root.Q<VisualElement>("lane_arena");
        laneElements = new[]
        {
            root.Q<VisualElement>("lane_car"),
            root.Q<VisualElement>("lane_bus"),
            root.Q<VisualElement>("lane_metro")
        };
        playerMarker = root.Q<VisualElement>("player_marker");
        resultContainer = root.Q<VisualElement>("result_container");
        rulesPopup = root.Q<VisualElement>("rules_popup");
        resultHeadlineLabel = root.Q<Label>("result_headline");
        resultMessageLabel = root.Q<Label>("result_message");
        resultFactLabel = root.Q<Label>("result_fact");
        resultScoreLabel = root.Q<Label>("result_score");
        beginButton = root.Q<Button>("begin_button");
        rulesButton = root.Q<Button>("rules_button");
        retryButton = root.Q<Button>("retry_button");
        backButton = root.Q<Button>("back_button");
        exitButton = root.Q<Button>("exit_button");
    }

    private void ShowIntroState()
    {
        if (titleLabel != null)
        {
            titleLabel.text = "Welcome to Public Transportation";
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Rules:\n1. Move up or down to switch lanes.\n2. Do not hit the blocks.\n3. Try to stay in the green lane.";
            descriptionLabel.style.display = DisplayStyle.Flex;
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

        if (playerMarker != null)
        {
            playerMarker.style.display = DisplayStyle.None;
        }

        if (laneArena != null)
        {
            laneArena.style.display = DisplayStyle.None;
        }

        if (hudRow != null)
        {
            hudRow.style.display = DisplayStyle.None;
        }

        if (laneHeaderRow != null)
        {
            laneHeaderRow.style.display = DisplayStyle.None;
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
            exitButton.style.display = DisplayStyle.Flex;
        }

        if (retryButton != null)
        {
            retryButton.style.display = DisplayStyle.None;
        }

        if (timerLabel != null)
        {
            timerLabel.text = $"Time: {Mathf.CeilToInt(survivalDuration)}s";
        }

        if (laneLabel != null)
        {
            laneLabel.text = "Current lane: Bus";
        }

        ClearObstacles();
    }

    private void StartRun()
    {
        hasStartedRun = true;
        timeRemaining = survivalDuration;
        spawnTimer = obstacleSpawnInterval;
        currentLaneIndex = 1;

        if (titleLabel != null)
        {
            titleLabel.text = "Public Transportation";
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Move up or down to switch lanes.";
            descriptionLabel.style.display = DisplayStyle.Flex;
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

        if (laneArena != null)
        {
            laneArena.style.display = DisplayStyle.Flex;
        }

        if (playerMarker != null)
        {
            playerMarker.style.display = DisplayStyle.Flex;
        }

        if (hudRow != null)
        {
            hudRow.style.display = DisplayStyle.Flex;
        }

        if (laneHeaderRow != null)
        {
            laneHeaderRow.style.display = DisplayStyle.Flex;
        }

        if (exitButton != null)
        {
            exitButton.style.display = DisplayStyle.None;
        }

        if (resultContainer != null)
        {
            resultContainer.style.display = DisplayStyle.None;
        }

        if (rulesPopup != null)
        {
            rulesPopup.style.display = DisplayStyle.None;
        }

        if (retryButton != null)
        {
            retryButton.style.display = DisplayStyle.None;
        }

        ClearObstacles();
        UpdatePlayerVisual();
        UpdateHud();
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

    private void HandleBeginClicked(ClickEvent evt)
    {
        if (!isActive || isResolving || hasStartedRun)
        {
            return;
        }

        if (beginButton != null)
        {
            beginButton.UnregisterCallback<ClickEvent>(HandleBeginClicked);
            beginButton.SetEnabled(false);
        }

        StartRun();
    }

    private void HandleRulesClicked(ClickEvent evt)
    {
        if (!isResolving || rulesPopup == null || resultContainer == null)
        {
            return;
        }

        resultContainer.style.display = DisplayStyle.None;
        rulesPopup.style.display = DisplayStyle.Flex;

        if (playerMarker != null)
        {
            playerMarker.style.display = DisplayStyle.None;
        }
    }

    private void HandleBackClicked(ClickEvent evt)
    {
        if (!isResolving || rulesPopup == null || resultContainer == null)
        {
            return;
        }

        rulesPopup.style.display = DisplayStyle.None;
        resultContainer.style.display = DisplayStyle.Flex;

        if (playerMarker != null)
        {
            playerMarker.style.display = DisplayStyle.None;
        }
    }

    private void HandleRetryClicked(ClickEvent evt)
    {
        if (!isResolving)
        {
            return;
        }

        isResolving = false;
        isActive = true;
        StartRun();
    }

    private void HandleKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            MovePlayerLane(-1);
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            MovePlayerLane(1);
        }
    }

    private void HandleSwipeInput()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            return;
        }

        var primaryTouch = touchscreen.primaryTouch;

        if (primaryTouch.press.wasPressedThisFrame)
        {
            swipeStartPosition = primaryTouch.position.ReadValue();
            swipeTrackingActive = true;
            return;
        }

        if (!swipeTrackingActive)
        {
            return;
        }

        if (primaryTouch.press.wasReleasedThisFrame)
        {
            Vector2 swipeEndPosition = primaryTouch.position.ReadValue();
            Vector2 swipeDelta = swipeEndPosition - swipeStartPosition;
            swipeTrackingActive = false;

            if (Mathf.Abs(swipeDelta.y) < SwipeThresholdPixels || Mathf.Abs(swipeDelta.y) <= Mathf.Abs(swipeDelta.x))
            {
                return;
            }

            MovePlayerLane(swipeDelta.y > 0f ? -1 : 1);
        }
    }

    private void SpawnObstacle()
    {
        if (laneArena == null)
        {
            return;
        }

        var obstacle = new VisualElement();
        obstacle.AddToClassList("transport-obstacle");
        obstacle.AddToClassList(ObstacleClasses[UnityEngine.Random.Range(0, ObstacleClasses.Length)]);

        int laneIndex = UnityEngine.Random.Range(0, 3);
        obstacle.style.left = Length.Percent(ObstacleStartLeftPercent);

        VisualElement targetLane = laneElements[laneIndex];
        targetLane?.Add(obstacle);
        CenterElementInLane(obstacle, targetLane);
        activeObstacles.Add(new ObstacleData
        {
            LaneIndex = laneIndex,
            Progress = 0f,
            Element = obstacle
        });
    }

    private void UpdateObstacles(float deltaTime)
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            ObstacleData obstacle = activeObstacles[i];
            obstacle.Progress += deltaTime / Mathf.Max(0.01f, obstacleTravelDuration);

            if (obstacle.Progress >= 1f)
            {
                obstacle.Element?.RemoveFromHierarchy();
                activeObstacles.RemoveAt(i);
                continue;
            }

            float leftPercent = Mathf.Lerp(ObstacleStartLeftPercent, ObstacleEndLeftPercent, obstacle.Progress);
            obstacle.Element.style.left = Length.Percent(leftPercent);
        }
    }

    private bool HasCollision()
    {
        for (int i = 0; i < activeObstacles.Count; i++)
        {
            ObstacleData obstacle = activeObstacles[i];
            if (obstacle.LaneIndex == currentLaneIndex && obstacle.Progress >= 0.83f && obstacle.Progress <= 0.96f)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveCrash()
    {
        if (isResolving)
        {
            return;
        }

        isResolving = true;
        isActive = false;
        pendingPollutionReduction = 0;
        ShowResult(
            "No safe lane",
            "You bumped into traffic before time was up.",
            "Try switching lanes sooner next time.",
            0);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

    }

    private void ResolveSuccess()
    {
        if (isResolving)
        {
            return;
        }

        isResolving = true;
        isActive = false;

        TransportChoice choice = GetChoiceForLane(currentLaneIndex);
        TransportOutcome outcome = BuildOutcome(choice);
        pendingPollutionReduction = outcome.PollutionReduction;
        ShowResult(
            $"You chose {choice}",
            $"You got {outcome.PollutionReduction} clean-air points.",
            outcome.Fact,
            outcome.PollutionReduction);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

    }

    private void ShowResult(string headline, string message, string fact, int pollutionReduction)
    {
        if (descriptionLabel != null)
        {
            descriptionLabel.text = string.Empty;
            descriptionLabel.style.display = DisplayStyle.None;
        }

        if (playerMarker != null)
        {
            playerMarker.style.display = DisplayStyle.None;
        }

        if (laneArena != null)
        {
            laneArena.style.display = DisplayStyle.None;
        }

        if (laneHeaderRow != null)
        {
            laneHeaderRow.style.display = DisplayStyle.None;
        }

        if (hudRow != null)
        {
            hudRow.style.display = DisplayStyle.None;
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
        SetLabelText(resultMessageLabel, message);
        SetLabelText(resultFactLabel, $"Fact: {fact}");
        SetLabelText(resultScoreLabel, $"Pollution reduction: {pollutionReduction}");
    }

    private void UpdatePlayerVisual()
    {
        if (playerMarker == null)
        {
            return;
        }

        VisualElement targetLane = laneElements != null && currentLaneIndex >= 0 && currentLaneIndex < laneElements.Length
            ? laneElements[currentLaneIndex]
            : null;

        if (targetLane == null)
        {
            return;
        }

        if (playerMarker.parent != targetLane)
        {
            playerMarker.RemoveFromHierarchy();
            targetLane.Add(playerMarker);
        }

        playerMarker.style.left = Length.Percent(PlayerLeftPercent);
        UpdatePlayerMarkerVisual();
        CenterElementInLane(playerMarker, targetLane);
    }

    private void UpdateHud()
    {
        if (timerLabel != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timerLabel.text = $"Time: {seconds}s";
            timerLabel.style.color = timeRemaining <= 4f ? new Color(1f, 0.3f, 0.3f) : Color.white;
        }

        if (laneLabel != null)
        {
            laneLabel.text = $"Current lane: {LaneNames[currentLaneIndex]}";
        }
    }

    private void HandleExitClicked(ClickEvent evt)
    {
        if (!isActive && !isResolving)
        {
            return;
        }

        DisableButtons();

        ClearObstacles();
        HideUI();
        bool finishingFromEndPage = isResolving;
        isActive = false;
        isResolving = false;

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

    private void ClearObstacles()
    {
        for (int i = 0; i < activeObstacles.Count; i++)
        {
            activeObstacles[i].Element?.RemoveFromHierarchy();
        }

        activeObstacles.Clear();
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

    private void CenterElementInLane(VisualElement element, VisualElement lane)
    {
        if (element == null || lane == null)
        {
            return;
        }

        root.schedule.Execute(() =>
        {
            if (element == null || lane == null)
            {
                return;
            }

            float laneHeight = lane.resolvedStyle.height;
            float elementHeight = element.resolvedStyle.height;
            if (laneHeight <= 0f || elementHeight <= 0f)
            {
                return;
            }

            float top = Mathf.Max(0f, (laneHeight - elementHeight) * 0.5f);
            element.style.top = top;
        }).StartingIn(0);
    }

    private void UpdatePlayerMarkerVisual()
    {
        if (playerMarker == null)
        {
            return;
        }

        for (int i = 0; i < PlayerLaneClasses.Length; i++)
        {
            playerMarker.RemoveFromClassList(PlayerLaneClasses[i]);
        }

        if (currentLaneIndex >= 0 && currentLaneIndex < PlayerLaneClasses.Length)
        {
            playerMarker.AddToClassList(PlayerLaneClasses[currentLaneIndex]);
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

