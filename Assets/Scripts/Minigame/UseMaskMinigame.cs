using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UseMaskMinigame : MonoBehaviour, IMinigame
{
    public string MinigameName => "Use a Mask";

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => isActive;

    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Gameplay")]
    public int maxAttempts = 2;
    public int successPollutionReduction = 1;
    public float timeLimit = 8f;
    public float successResultDelay = 2f;

    private bool isActive;
    private bool isResolving;
    private bool hasStartedRound;
    private int pendingPollutionReduction;

    private VisualElement root;
    private VisualElement introContainer;
    private VisualElement gameplayContainer;
    private VisualElement resultContainer;
    private VisualElement rulesPopup;
    private VisualElement playArea;
    private VisualElement faceDropZone;

    private VisualElement correctMask;
    private VisualElement wrongMask1;
    private VisualElement wrongMask2;

    private Label descriptionLabel;
    private Label timerLabel;
    private Label resultHeadlineLabel;
    private Label resultLabel;
    private Label resultFactLabel;
    private Label resultScoreLabel;
    private Label attemptsLabel;
    private Label roundFeedbackLabel;

    private readonly List<VisualElement> maskElements = new();
    private readonly Dictionary<VisualElement, Vector2> startPositions = new();
    private bool startPositionsInitialized;

    private VisualElement activeMask;
    private int activePointerId = -1;
    private Vector2 dragOffset;
    private int attemptsLeft;
    private float timeRemaining;
    private Coroutine delayedResultRoutine;

    private Button beginButton;
    private Button rulesButton;
    private Button retryButton;
    private Button backButton;
    private Button exitButton;

    private void Start()
    {
        HideUI();
    }

    private void LateUpdate()
    {
        if (!isActive && !isResolving &&
            uiDocument != null &&
            uiDocument.rootVisualElement != null &&
            uiDocument.rootVisualElement.style.display != DisplayStyle.None)
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
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            UpdateTimerDisplay();
            HandleTimeExpired();
            return;
        }

        UpdateTimerDisplay();
    }

    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UseMaskMinigame: UIDocument is not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("UseMaskMinigame: rootVisualElement is null.");
            OnMinigameExited?.Invoke();
            return;
        }

        CacheUi();
        if (playArea == null || faceDropZone == null || correctMask == null || wrongMask1 == null || wrongMask2 == null)
        {
            Debug.LogError("UseMaskMinigame: Missing required UI elements.");
            OnMinigameExited?.Invoke();
            return;
        }

        isActive = true;
        isResolving = false;
        hasStartedRound = false;
        pendingPollutionReduction = 0;
        activeMask = null;
        activePointerId = -1;

        root.style.display = DisplayStyle.Flex;

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

        playArea = root.Q<VisualElement>("play_area");
        faceDropZone = root.Q<VisualElement>("face_drop_zone");
        correctMask = root.Q<VisualElement>("mask_correct");
        wrongMask1 = root.Q<VisualElement>("mask_wrong_1");
        wrongMask2 = root.Q<VisualElement>("mask_wrong_2");

        attemptsLabel = root.Q<Label>("attempts_label");
        timerLabel = root.Q<Label>("timer_label");
        roundFeedbackLabel = root.Q<Label>("round_result_label");

        resultHeadlineLabel = root.Q<Label>("result_headline");
        resultLabel = root.Q<Label>("result_label");
        resultFactLabel = root.Q<Label>("result_fact");
        resultScoreLabel = root.Q<Label>("result_score");

        beginButton = root.Q<Button>("begin_button");
        rulesButton = root.Q<Button>("rules_button");
        retryButton = root.Q<Button>("retry_button");
        backButton = root.Q<Button>("back_button");
        exitButton = root.Q<Button>("exit_button");

        maskElements.Clear();
        if (correctMask != null) maskElements.Add(correctMask);
        if (wrongMask1 != null) maskElements.Add(wrongMask1);
        if (wrongMask2 != null) maskElements.Add(wrongMask2);
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
            descriptionLabel.text =
                "Rules:\n1. Drag one mask to the face.\n2. Pick the safest one.\n3. Choose before your turns run out.";
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
        if (delayedResultRoutine != null)
        {
            StopCoroutine(delayedResultRoutine);
            delayedResultRoutine = null;
        }

        isActive = true;
        isResolving = false;
        hasStartedRound = true;
        pendingPollutionReduction = 0;
        attemptsLeft = maxAttempts;
        timeRemaining = timeLimit;
        activeMask = null;
        activePointerId = -1;

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Drag the best mask onto the face";
            descriptionLabel.style.display = DisplayStyle.Flex;
            descriptionLabel.style.fontSize = 18f;
            descriptionLabel.style.marginTop = 0f;
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

        if (startPositionsInitialized)
        {
            ResetAllMasksToStartPositions();
        }

        if (gameplayContainer != null)
        {
            gameplayContainer.style.display = DisplayStyle.Flex;
            gameplayContainer.style.visibility = Visibility.Hidden;
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

        foreach (VisualElement mask in maskElements)
        {
            mask.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            mask.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
            mask.UnregisterCallback<PointerUpEvent>(HandlePointerUp);

            mask.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            mask.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            mask.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            mask.SetEnabled(true);
            mask.pickingMode = PickingMode.Position;
        }

        if (playArea != null)
        {
            playArea.pickingMode = PickingMode.Position;
        }

        if (roundFeedbackLabel != null)
        {
            roundFeedbackLabel.text = string.Empty;
            roundFeedbackLabel.style.display = DisplayStyle.None;
            roundFeedbackLabel.pickingMode = PickingMode.Ignore;
        }

        UpdateAttemptsLabel();
        UpdateTimerDisplay();
        root.schedule.Execute(InitializeMaskPositions).StartingIn(0);
    }

    private void InitializeMaskPositions()
    {
        if (playArea == null)
        {
            return;
        }

        float areaWidth = playArea.contentRect.width;
        float areaHeight = playArea.contentRect.height;

        if (areaWidth <= 0 || areaHeight <= 0)
        {
            root.schedule.Execute(InitializeMaskPositions).StartingIn(50);
            return;
        }

        if (!startPositionsInitialized)
        {
            startPositions.Clear();

            CacheStartPosition(correctMask);
            CacheStartPosition(wrongMask1);
            CacheStartPosition(wrongMask2);
            startPositionsInitialized = true;
            ResetAllMasksToStartPositions();
        }

        if (roundFeedbackLabel != null)
        {
            roundFeedbackLabel.text = string.Empty;
            roundFeedbackLabel.style.display = DisplayStyle.None;
            roundFeedbackLabel.pickingMode = PickingMode.Ignore;
        }

        if (gameplayContainer != null)
        {
            gameplayContainer.style.visibility = Visibility.Visible;
        }
    }

    private void CacheStartPosition(VisualElement mask)
    {
        if (mask == null)
        {
            return;
        }

        float left = mask.layout.x;
        float top = mask.layout.y;

        if (float.IsNaN(left)) left = 0f;
        if (float.IsNaN(top)) top = 0f;

        startPositions[mask] = new Vector2(left, top);
    }

    private void HandlePointerDown(PointerDownEvent evt)
    {
        if (!isActive || isResolving || !hasStartedRound)
        {
            return;
        }

        VisualElement mask = evt.currentTarget as VisualElement;
        if (mask == null || playArea == null)
        {
            return;
        }

        activeMask = mask;
        activePointerId = evt.pointerId;

        Vector2 pointerInPlayArea = playArea.WorldToLocal(evt.position);
        Vector2 maskPosition = GetMaskPosition(mask);
        dragOffset = pointerInPlayArea - maskPosition;

        mask.CapturePointer(activePointerId);
        mask.BringToFront();
        evt.StopPropagation();
    }

    private void HandlePointerMove(PointerMoveEvent evt)
    {
        if (!isActive || isResolving || !hasStartedRound)
        {
            return;
        }

        if (activeMask == null || playArea == null)
        {
            return;
        }

        if (evt.pointerId != activePointerId || !activeMask.HasPointerCapture(activePointerId))
        {
            return;
        }

        Vector2 pointerInPlayArea = playArea.WorldToLocal(evt.position);

        float newLeft = pointerInPlayArea.x - dragOffset.x;
        float newTop = pointerInPlayArea.y - dragOffset.y;

        float maskWidth = activeMask.resolvedStyle.width;
        float maskHeight = activeMask.resolvedStyle.height;

        float maxLeft = Mathf.Max(0, playArea.contentRect.width - maskWidth);
        float maxTop = Mathf.Max(0, playArea.contentRect.height - maskHeight);

        newLeft = Mathf.Clamp(newLeft, 0, maxLeft);
        newTop = Mathf.Clamp(newTop, 0, maxTop);

        activeMask.style.left = newLeft;
        activeMask.style.top = newTop;
        evt.StopPropagation();
    }

    private void HandlePointerUp(PointerUpEvent evt)
    {
        if (!isActive || isResolving || !hasStartedRound)
        {
            return;
        }

        if (activeMask == null || evt.pointerId != activePointerId)
        {
            return;
        }

        VisualElement releasedMask = activeMask;
        if (releasedMask.HasPointerCapture(activePointerId))
        {
            releasedMask.ReleasePointer(activePointerId);
        }

        activeMask = null;
        activePointerId = -1;

        bool droppedOnFace = releasedMask.worldBound.Overlaps(faceDropZone.worldBound);
        if (!droppedOnFace)
        {
            ResetMaskPosition(releasedMask);
            evt.StopPropagation();
            return;
        }

        if (releasedMask == correctMask)
        {
            HandleCorrectMaskDropped();
        }
        else
        {
            HandleIncorrectMaskDropped(releasedMask);
        }

        evt.StopPropagation();
    }

    private Vector2 GetMaskPosition(VisualElement mask)
    {
        float left = mask.resolvedStyle.left;
        float top = mask.resolvedStyle.top;

        if (float.IsNaN(left)) left = 0f;
        if (float.IsNaN(top)) top = 0f;

        return new Vector2(left, top);
    }

    private void HandleCorrectMaskDropped()
    {
        SnapMaskToFace(correctMask);

        if (roundFeedbackLabel != null)
        {
            roundFeedbackLabel.style.display = DisplayStyle.Flex;
            roundFeedbackLabel.text = "Correct mask selected!";
            roundFeedbackLabel.style.color = new StyleColor(new Color(0.3f, 1f, 0.3f));
            roundFeedbackLabel.BringToFront();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

        DisableInteraction(keepVisualState: true);
        isActive = false;
        isResolving = true;
        hasStartedRound = false;
        pendingPollutionReduction = successPollutionReduction;
        delayedResultRoutine = StartCoroutine(ShowSuccessResultAfterDelay());
    }

    private IEnumerator ShowSuccessResultAfterDelay()
    {
        yield return new WaitForSeconds(successResultDelay);

        delayedResultRoutine = null;

        ShowResult(
            "Nice choice!",
            "That mask helps protect you.",
            "The right mask helps block dirty air.",
            pendingPollutionReduction);
    }

    private void HandleIncorrectMaskDropped(VisualElement wrongMask)
    {
        attemptsLeft--;
        ResetMaskPosition(wrongMask);

        if (roundFeedbackLabel != null)
        {
            roundFeedbackLabel.style.display = DisplayStyle.Flex;
            roundFeedbackLabel.style.color = new StyleColor(new Color(0.85f, 0.2f, 0.2f));
            roundFeedbackLabel.text = attemptsLeft > 0 ? "Incorrect. Try again." : "Out of attempts.";
            roundFeedbackLabel.BringToFront();
        }

        UpdateAttemptsLabel();

        if (attemptsLeft > 0)
        {
            return;
        }

        DisableInteraction();
        isActive = false;
        isResolving = true;
        hasStartedRound = false;
        pendingPollutionReduction = 0;

        ShowResult(
            "No safe mask picked",
            "You ran out of tries before picking the best mask.",
            "Picking the right mask helps keep you safer.",
            pendingPollutionReduction);
    }

    private void ShowResult(string headline, string message, string fact, int pollutionReduction)
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

        if (delayedResultRoutine != null)
        {
            StopCoroutine(delayedResultRoutine);
            delayedResultRoutine = null;
        }

        DisableInteraction();
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

    private void SnapMaskToFace(VisualElement mask)
    {
        if (playArea == null || faceDropZone == null || mask == null)
        {
            return;
        }

        Rect faceRect = faceDropZone.layout;
        float maskWidth = mask.resolvedStyle.width;
        float maskHeight = mask.resolvedStyle.height;
        float playAreaBorderLeft = playArea.resolvedStyle.borderLeftWidth;
        float playAreaBorderTop = playArea.resolvedStyle.borderTopWidth;

        if (float.IsNaN(playAreaBorderLeft)) playAreaBorderLeft = 0f;
        if (float.IsNaN(playAreaBorderTop)) playAreaBorderTop = 0f;

        float snapLeft = faceRect.x + (faceRect.width - maskWidth) * 0.5f - playAreaBorderLeft;
        float snapTop = faceRect.y + faceRect.height * 0.62f - maskHeight * 0.5f - playAreaBorderTop;

        mask.style.left = snapLeft;
        mask.style.top = snapTop;
    }

    private void ResetMaskPosition(VisualElement mask)
    {
        if (mask == null || !startPositions.ContainsKey(mask))
        {
            return;
        }

        Vector2 startPos = startPositions[mask];
        mask.style.right = StyleKeyword.Auto;
        mask.style.bottom = StyleKeyword.Auto;
        mask.style.left = startPos.x;
        mask.style.top = startPos.y;
    }

    private void ResetAllMasksToStartPositions()
    {
        foreach (VisualElement mask in maskElements)
        {
            ResetMaskPosition(mask);
        }
    }

    private void UpdateAttemptsLabel()
    {
        if (attemptsLabel != null)
        {
            attemptsLabel.text = $"Attempts Left: {attemptsLeft}";
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerLabel == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
        timerLabel.text = $"Time: {seconds}s";
        timerLabel.style.color = timeRemaining <= 4f
            ? new StyleColor(new Color(1f, 0.3f, 0.3f))
            : new StyleColor(Color.white);
    }

    private void HandleTimeExpired()
    {
        if (!isActive || isResolving)
        {
            return;
        }

        DisableInteraction();
        isActive = false;
        isResolving = true;
        hasStartedRound = false;
        pendingPollutionReduction = 0;

        ShowResult(
            "Time up",
            "You ran out of time before picking the best mask.",
            "Picking quickly can help keep you safer.",
            pendingPollutionReduction);
    }

    private void DisableInteraction(bool keepVisualState = false)
    {
        if (activeMask != null && activePointerId >= 0 && activeMask.HasPointerCapture(activePointerId))
        {
            activeMask.ReleasePointer(activePointerId);
        }

        foreach (VisualElement mask in maskElements)
        {
            if (mask == null)
            {
                continue;
            }

            for (int pointerId = 0; pointerId <= 9; pointerId++)
            {
                if (mask.HasPointerCapture(pointerId))
                {
                    mask.ReleasePointer(pointerId);
                }
            }

            mask.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            mask.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
            mask.UnregisterCallback<PointerUpEvent>(HandlePointerUp);

            if (!keepVisualState)
            {
                mask.SetEnabled(false);
            }

            mask.pickingMode = PickingMode.Ignore;
        }

        if (playArea != null)
        {
            playArea.pickingMode = PickingMode.Ignore;
        }

        activeMask = null;
        activePointerId = -1;
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
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}

