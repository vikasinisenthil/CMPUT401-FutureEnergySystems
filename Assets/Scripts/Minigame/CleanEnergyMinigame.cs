using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MG.04 - Clean Energy.
/// Flow mirrors PublicTransportationMinigame:
/// - Intro page with Begin/Exit.
/// - Existing gameplay page where the player picks one source.
/// - End page with Rules/Retry/Continue.
/// </summary>
public class CleanEnergyMinigame : MonoBehaviour, IMinigame
{
    public string MinigameName => "Clean Energy!";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => isActive;

    public UIDocument uiDocument;

    private static readonly string[] CleanSources = { "Solar Panel", "Wind Turbine", "Hydroelectric" };
    private static readonly string[] DirtySources = { "Coal Plant", "Oil Rig", "Natural Gas" };

    private bool isActive;
    private bool isResolving;
    private bool hasStartedRound;
    private bool choiceMade;
    private int pendingPollutionReduction;
    private int correctChoiceIndex;

    private readonly string[] choiceNames = new string[3];

    private VisualElement root;
    private VisualElement introContainer;
    private VisualElement choicesContainer;
    private VisualElement resultContainer;
    private VisualElement rulesPopup;

    private Label titleLabel;
    private Label descriptionLabel;
    private Label resultHeadlineLabel;
    private Label resultLabel;
    private Label resultFactLabel;
    private Label resultScoreLabel;

    private Button beginButton;
    private Button rulesButton;
    private Button retryButton;
    private Button backButton;
    private Button exitButton;

    private readonly Button[] choiceButtons = new Button[3];
    private readonly Label[] choiceLabels = new Label[3];
    private readonly VisualElement[] choiceImages = new VisualElement[3];

    private static readonly Dictionary<string, string> SourceImagePaths = new Dictionary<string, string>
    {
        { "Solar Panel",    "CleanEnergy/Solar" },
        { "Wind Turbine",   "CleanEnergy/Wind-Turbine-Close-up" },
        { "Hydroelectric",  "CleanEnergy/Hydro" },
        { "Coal Plant",     "CleanEnergy/Coals" },
        { "Oil Rig",        "CleanEnergy/OilDrum" },
        { "Natural Gas",    "CleanEnergy/Natural-Gas" },
    };

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

    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("CleanEnergyMinigame: UIDocument is not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("CleanEnergyMinigame: rootVisualElement is null.");
            OnMinigameExited?.Invoke();
            return;
        }

        isActive = true;
        isResolving = false;
        hasStartedRound = false;
        choiceMade = false;
        pendingPollutionReduction = 0;

        CacheUi();
        ShowIntroState();
        RegisterCallbacks();

        root.style.display = DisplayStyle.Flex;
    }

    private void CacheUi()
    {
        titleLabel = root.Q<Label>("minigame_title");
        descriptionLabel = root.Q<Label>("minigame_description");
        introContainer = root.Q<VisualElement>("intro_container");
        choicesContainer = root.Q<VisualElement>("choices_container");
        resultContainer = root.Q<VisualElement>("result_container");
        rulesPopup = root.Q<VisualElement>("rules_popup");

        resultHeadlineLabel = root.Q<Label>("result_headline");
        resultLabel = root.Q<Label>("result_label");
        resultFactLabel = root.Q<Label>("result_fact");
        resultScoreLabel = root.Q<Label>("result_score");

        beginButton = root.Q<Button>("begin_button");
        rulesButton = root.Q<Button>("rules_button");
        retryButton = root.Q<Button>("retry_button");
        backButton = root.Q<Button>("back_button");
        exitButton = root.Q<Button>("exit_button");

        for (int i = 0; i < 3; i++)
        {
            choiceButtons[i] = root.Q<Button>($"choice_{i}");
            choiceLabels[i] = root.Q<Label>($"choice_label_{i}");
            choiceImages[i] = root.Q<VisualElement>($"choice_image_{i}");
        }
    }

    private void ShowIntroState()
    {
        if (titleLabel != null)
        {
            titleLabel.text = "Welcome to Clean Energy";
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Rules:\n1. Tap one energy picture.\n2. Find the one that helps the air.\n3. Pick it to earn clean-air points.";
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

        if (choicesContainer != null)
        {
            choicesContainer.style.display = DisplayStyle.None;
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

        SetLabelText(resultLabel, string.Empty);
    }

    private void StartRound()
    {
        hasStartedRound = true;
        choiceMade = false;
        pendingPollutionReduction = 0;

        BuildChoices();
        ResetChoices();

        if (titleLabel != null)
        {
            titleLabel.text = "Clean Energy!";
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Choose the clean energy source!";
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

        if (choicesContainer != null)
        {
            choicesContainer.style.display = DisplayStyle.Flex;
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

        if (choiceButtons[0] != null)
        {
            choiceButtons[0].UnregisterCallback<ClickEvent>(HandleChoice0Clicked);
            choiceButtons[0].RegisterCallback<ClickEvent>(HandleChoice0Clicked);
        }

        if (choiceButtons[1] != null)
        {
            choiceButtons[1].UnregisterCallback<ClickEvent>(HandleChoice1Clicked);
            choiceButtons[1].RegisterCallback<ClickEvent>(HandleChoice1Clicked);
        }

        if (choiceButtons[2] != null)
        {
            choiceButtons[2].UnregisterCallback<ClickEvent>(HandleChoice2Clicked);
            choiceButtons[2].RegisterCallback<ClickEvent>(HandleChoice2Clicked);
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

    private void HandleChoice0Clicked(ClickEvent evt) => HandleChoiceClicked(0);
    private void HandleChoice1Clicked(ClickEvent evt) => HandleChoiceClicked(1);
    private void HandleChoice2Clicked(ClickEvent evt) => HandleChoiceClicked(2);

    private void HandleChoiceClicked(int index)
    {
        if (!isActive || isResolving || !hasStartedRound || choiceMade)
        {
            return;
        }

        choiceMade = true;
        DisableAllChoices();

        bool isCorrect = index == correctChoiceIndex;
        if (isCorrect)
        {
            HandleCorrectChoice(index);
        }
        else
        {
            HandleWrongChoice(index);
        }
    }

    private void HandleCorrectChoice(int index)
    {
        if (choiceButtons[index] != null)
        {
            choiceButtons[index].AddToClassList("ce-choice-success");
        }

        pendingPollutionReduction = 1;
        ShowResult(
            "Great pick!",
            $"Nice! {choiceNames[index]} is a clean energy choice.",
            $"{choiceNames[index]} helps keep the air cleaner.",
            pendingPollutionReduction,
            new Color(0.3f, 1f, 0.3f));

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }
    }

    private void HandleWrongChoice(int index)
    {
        if (choiceButtons[index] != null)
        {
            choiceButtons[index].AddToClassList("ce-choice-fail");
        }

        if (choiceButtons[correctChoiceIndex] != null)
        {
            choiceButtons[correctChoiceIndex].AddToClassList("ce-choice-success");
        }

        pendingPollutionReduction = -1;
        ShowResult(
            "Try again next time",
            $"{choiceNames[index]} is not the clean one. {choiceNames[correctChoiceIndex]} was the clean choice.",
            "Some energy choices make more dirty air.",
            pendingPollutionReduction,
            new Color(1f, 0.35f, 0.35f));

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCough();
        }
    }

    private void ShowResult(string headline, string message, string fact, int pollutionReduction, Color messageColor)
    {
        isResolving = true;
        isActive = false;
        hasStartedRound = false;

        if (descriptionLabel != null)
        {
            descriptionLabel.text = string.Empty;
            descriptionLabel.style.display = DisplayStyle.None;
        }

        if (introContainer != null)
        {
            introContainer.style.display = DisplayStyle.None;
        }

        if (choicesContainer != null)
        {
            choicesContainer.style.display = DisplayStyle.None;
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
        if (resultLabel != null)
        {
            resultLabel.style.color = new StyleColor(messageColor);
        }

        SetLabelText(resultFactLabel, $"Fact: {fact}");
        SetLabelText(resultScoreLabel, $"Pollution reduction: {pollutionReduction}");
    }

    private void DisableAllChoices()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null)
            {
                continue;
            }

            choiceButtons[i].SetEnabled(false);
        }
    }

    private void ResetChoices()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceLabels[i] != null)
            {
                choiceLabels[i].text = choiceNames[i];
            }

            if (choiceImages[i] != null && SourceImagePaths.TryGetValue(choiceNames[i], out string imagePath))
            {
                Sprite sprite = Resources.Load<Sprite>(imagePath);
                if (sprite != null)
                {
                    choiceImages[i].style.backgroundImage = new StyleBackground(sprite);
                }
            }

            if (choiceButtons[i] == null)
            {
                continue;
            }

            choiceButtons[i].SetEnabled(true);
            choiceButtons[i].RemoveFromClassList("ce-choice-success");
            choiceButtons[i].RemoveFromClassList("ce-choice-fail");
        }

        SetLabelText(resultHeadlineLabel, string.Empty);
        SetLabelText(resultLabel, string.Empty);
        SetLabelText(resultFactLabel, string.Empty);
        SetLabelText(resultScoreLabel, string.Empty);
    }

    private void BuildChoices()
    {
        string clean = CleanSources[UnityEngine.Random.Range(0, CleanSources.Length)];

        List<string> dirtyPool = new List<string>(DirtySources);
        int firstDirtyIndex = UnityEngine.Random.Range(0, dirtyPool.Count);
        string dirty1 = dirtyPool[firstDirtyIndex];
        dirtyPool.RemoveAt(firstDirtyIndex);
        string dirty2 = dirtyPool[UnityEngine.Random.Range(0, dirtyPool.Count)];

        string[] sources = { clean, dirty1, dirty2 };
        int[] order = { 0, 1, 2 };
        for (int i = 2; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tmp = order[i];
            order[i] = order[j];
            order[j] = tmp;
        }

        for (int slot = 0; slot < 3; slot++)
        {
            choiceNames[slot] = sources[order[slot]];
            if (sources[order[slot]] == clean)
            {
                correctChoiceIndex = slot;
            }
        }
    }

    private void HandleExitClicked(ClickEvent evt)
    {
        if (!isActive && !isResolving)
        {
            return;
        }

        DisableButtons();
        HideUI();

        bool finishingFromEndPage = isResolving;
        isActive = false;
        isResolving = false;
        hasStartedRound = false;
        choiceMade = false;

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

        if (choiceButtons[0] != null)
        {
            choiceButtons[0].UnregisterCallback<ClickEvent>(HandleChoice0Clicked);
        }

        if (choiceButtons[1] != null)
        {
            choiceButtons[1].UnregisterCallback<ClickEvent>(HandleChoice1Clicked);
        }

        if (choiceButtons[2] != null)
        {
            choiceButtons[2].UnregisterCallback<ClickEvent>(HandleChoice2Clicked);
        }

        DisableAllChoices();
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

    public int GetCorrectChoiceIndex() => correctChoiceIndex;

    public string GetChoiceName(int index)
    {
        if (index < 0 || index >= 3)
        {
            return null;
        }

        return choiceNames[index];
    }

    public bool IsChoiceCorrect(int index) => index == correctChoiceIndex;
}

