using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MG.07 - Save Energy.
/// Gameplay is unchanged:
/// - NPCs turn lights on.
/// - Player turns lit lights off.
/// - Waste points accumulate while lights remain on.
/// UI flow matches CleanEnergyMinigame:
/// intro -> gameplay -> end page (rules/retry/exit).
/// </summary>
public class SaveEnergyMinigame : MonoBehaviour, IMinigame
{
    public string MinigameName => "Save Energy!";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => isActive;

    public UIDocument uiDocument;

    public float timeLimit = 30f;
    public int pointsThreshold = 25;
    public float npcActivationMin = 3f;
    public float npcActivationMax = 7f;

    private const int LIGHT_COUNT = 6;

    private bool isActive;
    private bool isResolving;
    private bool hasStartedRound;
    private int pendingPollutionReduction;

    private VisualElement root;
    private VisualElement introContainer;
    private VisualElement gameplayContainer;
    private VisualElement resultContainer;
    private VisualElement rulesPopup;
    private VisualElement minigameCard;

    private float timeRemaining;
    private float accumulatedPoints;
    private float npcTimer;
    private float nextNpcActivation;

    private readonly bool[] lightsOn = new bool[LIGHT_COUNT];
    private readonly VisualElement[] lightElements = new VisualElement[LIGHT_COUNT];

    private Label descriptionLabel;
    private Label timerLabel;
    private Label pointsLabel;
    private Label npcMessageLabel;
    private Label resultHeadlineLabel;
    private Label resultLabel;
    private Label resultFactLabel;
    private Label resultScoreLabel;

    private Button beginButton;
    private Button rulesButton;
    private Button retryButton;
    private Button backButton;
    private Button exitButton;

    private EventCallback<ClickEvent>[] lightCallbacks;

    private void Start()
    {
        HideUI();
    }

    private void LateUpdate()
    {
        if (!isActive && !isResolving
            && uiDocument != null
            && uiDocument.rootVisualElement != null
            && uiDocument.rootVisualElement.style.display != DisplayStyle.None)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void Update()
    {
        if (!isActive || isResolving || !hasStartedRound)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;

        int litCount = CountLitLights();
        accumulatedPoints += litCount * Time.deltaTime;

        npcTimer += Time.deltaTime;
        if (npcTimer >= nextNpcActivation)
        {
            npcTimer = 0f;
            nextNpcActivation = UnityEngine.Random.Range(npcActivationMin, npcActivationMax);
            TryActivateNpc();
        }

        UpdateDisplays();

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            HandleTimerEnd();
        }
    }

    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("SaveEnergyMinigame: UIDocument is not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("SaveEnergyMinigame: rootVisualElement is null.");
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
        minigameCard = root.Q<VisualElement>("minigame_card");

        timerLabel = root.Q<Label>("timer_label");
        pointsLabel = root.Q<Label>("points_label");
        npcMessageLabel = root.Q<Label>("npc_message_label");
        resultHeadlineLabel = root.Q<Label>("result_headline");
        resultLabel = root.Q<Label>("result_label");
        resultFactLabel = root.Q<Label>("result_fact");
        resultScoreLabel = root.Q<Label>("result_score");

        beginButton = root.Q<Button>("begin_button");
        rulesButton = root.Q<Button>("rules_button");
        retryButton = root.Q<Button>("retry_button");
        backButton = root.Q<Button>("back_button");
        exitButton = root.Q<Button>("exit_button");

        if (lightCallbacks == null || lightCallbacks.Length != LIGHT_COUNT)
        {
            lightCallbacks = new EventCallback<ClickEvent>[LIGHT_COUNT];
        }

        for (int i = 0; i < LIGHT_COUNT; i++)
        {
            lightElements[i] = root.Q<VisualElement>($"light_{i}");
        }
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
        ApplyTransportTheme(true);
        ApplyGameplayAlertTheme(false);

        if (descriptionLabel != null)
        {
            descriptionLabel.text =
                "Rules:\n1. Some room lights turn on.\n2. Tap bright rooms to switch them off.\n3. Turn off as many as you can before time is up.";
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
        ApplyTransportTheme(false);

        hasStartedRound = true;
        isActive = true;
        isResolving = false;
        pendingPollutionReduction = 0;

        timeRemaining = timeLimit;
        accumulatedPoints = 0f;
        npcTimer = 0f;
        nextNpcActivation = UnityEngine.Random.Range(npcActivationMin, npcActivationMax);

        for (int i = 0; i < LIGHT_COUNT; i++)
        {
            lightsOn[i] = false;
            UpdateLightVisual(i);
        }
        ApplyGameplayAlertTheme(false);

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Turn off lit rooms before energy waste gets too high.";
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

        if (npcMessageLabel != null)
        {
            npcMessageLabel.text = string.Empty;
            npcMessageLabel.style.display = DisplayStyle.None;
        }

        RegisterLightCallbacks();
        UpdateDisplays();
    }

    private void RegisterLightCallbacks()
    {
        for (int i = 0; i < LIGHT_COUNT; i++)
        {
            if (lightElements[i] == null)
            {
                continue;
            }

            if (lightCallbacks[i] != null)
            {
                lightElements[i].UnregisterCallback(lightCallbacks[i]);
            }

            int index = i;
            lightCallbacks[i] = _ => HandleLightClicked(index);
            lightElements[i].RegisterCallback(lightCallbacks[i]);
            lightElements[i].SetEnabled(true);
        }
    }

    private void DisableLightInteractions()
    {
        for (int i = 0; i < LIGHT_COUNT; i++)
        {
            if (lightElements[i] == null)
            {
                continue;
            }

            if (lightCallbacks != null && i < lightCallbacks.Length && lightCallbacks[i] != null)
            {
                lightElements[i].UnregisterCallback(lightCallbacks[i]);
            }

            lightElements[i].SetEnabled(false);
        }
    }

    private void TryActivateNpc()
    {
        if (!isActive || isResolving || !hasStartedRound)
        {
            return;
        }

        List<int> offLights = new List<int>();
        for (int i = 0; i < LIGHT_COUNT; i++)
        {
            if (!lightsOn[i])
            {
                offLights.Add(i);
            }
        }

        if (offLights.Count == 0)
        {
            return;
        }

        int chosen = offLights[UnityEngine.Random.Range(0, offLights.Count)];
        lightsOn[chosen] = true;
        UpdateLightVisual(chosen);
        UpdateGameplayAlertTheme();

        ShowNpcMessage();
        Debug.Log($"SaveEnergyMinigame: NPC turned on light_{chosen}!");
    }

    public void ForceActivateNpc()
    {
        TryActivateNpc();
    }

    private void HandleLightClicked(int index)
    {
        if (!isActive || isResolving || !hasStartedRound)
        {
            return;
        }

        if (!lightsOn[index])
        {
            return;
        }

        lightsOn[index] = false;
        UpdateLightVisual(index);
        UpdateGameplayAlertTheme();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

        Debug.Log($"SaveEnergyMinigame: Player turned off light_{index}.");
    }

    private void HandleTimerEnd()
    {
        if (!isActive || isResolving)
        {
            return;
        }

        DisableLightInteractions();

        int pollutionReduction = ComputePollutionReductionForPoints(accumulatedPoints, pointsThreshold);
        pendingPollutionReduction = pollutionReduction;
        isActive = false;
        isResolving = true;
        hasStartedRound = false;

        ShowResult(pollutionReduction);
    }

    public static int ComputePollutionReductionForPoints(float points, float threshold)
    {
        return points < threshold ? 1 : -1;
    }

    private void ShowResult(int pollutionReduction)
    {
        ApplyTransportTheme(true);
        ApplyGameplayAlertTheme(false);

        string headline = pollutionReduction > 0 ? "You saved energy" : "Too many lights stayed on";
        string message = pollutionReduction > 0
            ? "Great job! You turned lights off quickly."
            : "Try to turn lights off faster next time.";
        string fact = "Turning off extra lights helps keep air cleaner.";

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
        SetLabelText(resultScoreLabel, $"Pollution reduction: {pollutionReduction}");
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

    private void HandleExitClicked(ClickEvent evt)
    {
        if (!isActive && !isResolving)
        {
            return;
        }

        DisableLightInteractions();
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

    private void ShowNpcMessage()
    {
        if (npcMessageLabel == null)
        {
            return;
        }

        npcMessageLabel.text = "An NPC turned on a light!";
        npcMessageLabel.style.display = DisplayStyle.Flex;

        root.schedule.Execute(() =>
        {
            if (npcMessageLabel != null)
            {
                npcMessageLabel.style.display = DisplayStyle.None;
            }
        }).StartingIn(1500);
    }

    private void UpdateLightVisual(int index)
    {
        VisualElement light = lightElements[index];
        if (light == null)
        {
            return;
        }

        if (lightsOn[index])
        {
            light.RemoveFromClassList("se-light-off");
            light.AddToClassList("se-light-on");
        }
        else
        {
            light.RemoveFromClassList("se-light-on");
            light.AddToClassList("se-light-off");
        }
    }

    private void UpdateDisplays()
    {
        if (timerLabel != null)
        {
            int secs = Mathf.CeilToInt(timeRemaining);
            timerLabel.text = $"Time: {secs}s";
            timerLabel.style.color = timeRemaining <= 5f
                ? new StyleColor(new Color(1f, 0.3f, 0.3f))
                : new StyleColor(Color.white);
        }

        if (pointsLabel != null)
        {
            pointsLabel.text = $"Waste: {(int)accumulatedPoints} pts";
            pointsLabel.style.color = accumulatedPoints >= pointsThreshold
                ? new StyleColor(new Color(1f, 0.3f, 0.3f))
                : new StyleColor(Color.white);
        }
    }

    private int CountLitLights()
    {
        int count = 0;
        for (int i = 0; i < LIGHT_COUNT; i++)
        {
            if (lightsOn[i])
            {
                count++;
            }
        }

        return count;
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

    private void ApplyTransportTheme(bool enabled)
    {
        if (minigameCard == null)
        {
            return;
        }

        minigameCard.EnableInClassList("se-card-transport-theme", enabled);
    }

    private void UpdateGameplayAlertTheme()
    {
        ApplyGameplayAlertTheme(CountLitLights() > 0);
    }

    private void ApplyGameplayAlertTheme(bool enabled)
    {
        if (minigameCard == null)
        {
            return;
        }

        minigameCard.EnableInClassList("se-card-live-alert", enabled);
    }

    private void HideUI()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    public float GetPoints() => accumulatedPoints;
    public float GetTimeRemaining() => timeRemaining;
    public bool IsLightOn(int index) => index >= 0 && index < LIGHT_COUNT && lightsOn[index];
    public int GetLitCount() => CountLitLights();
}

