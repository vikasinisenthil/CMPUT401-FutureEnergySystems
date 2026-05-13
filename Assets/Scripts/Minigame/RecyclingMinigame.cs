using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MG.08 - Recycling Sorting.
/// Player drags each center trash item into one of four bins.
/// Correct bin: +1 score. Wrong bin: -1 score.
/// End rewards:
/// score >= 7 => reduction 2
/// score >= 4 => reduction 1
/// otherwise    reduction 0
/// </summary>
public class RecyclingMinigame : MonoBehaviour, IMinigame
{
    public enum BinType
    {
        LeftTop,
        LeftBottom,
        RightTop,
        RightBottom
    }

    [Serializable]
    public struct TrashItemData
    {
        public string label;
        public BinType correctBin;
        public string resourcePath;

        public TrashItemData(string label, BinType correctBin, string resourcePath)
        {
            this.label = label;
            this.correctBin = correctBin;
            this.resourcePath = resourcePath;
        }
    }

    public string MinigameName => "Recycling!";
    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;
    public bool IsActive => isActive;

    [Header("UI")]
    public UIDocument uiDocument;

    private bool isActive;
    private bool isResolving;
    private bool roundStarted;

    private int score;
    private int currentItemIndex;
    private int pendingPollutionReduction;

    private Vector2 dragStart;
    private bool trackingDrag;
    private bool draggingTrash;
    private int dragPointerId = -1;

    private VisualElement root;
    private VisualElement introContainer;
    private VisualElement gameplayContainer;
    private VisualElement resultContainer;

    private Label titleLabel;
    private Label descriptionLabel;
    private Label scoreLabel;
    private Label progressLabel;
    private Label feedbackLabel;
    private Label currentTrashLabel;
    private VisualElement binLeftTop;
    private VisualElement binLeftBottom;
    private VisualElement binRightTop;
    private VisualElement binRightBottom;

    private Label resultHeadlineLabel;
    private Label resultLabel;
    private Label resultScoreLabel;
    private Label resultReductionLabel;

    private Button beginButton;
    private Button continueButton;
    private readonly Dictionary<string, Texture2D> textureCache = new();
    private const float ItemMaxWidth = 200f;
    private const float ItemMaxHeight = 130f;
    private const string DefaultFeedbackText = "Drag each item into a bin.";
    private Coroutine feedbackResetRoutine;

    private readonly List<TrashItemData> items = new()
    {
        new TrashItemData("Paper", BinType.LeftTop, "RecyclingMinigame/paper_recycling"),
        new TrashItemData("Plastic Bottle", BinType.RightTop, "RecyclingMinigame/water_bottle_recycling"),
        new TrashItemData("Banana Peel", BinType.LeftBottom, "RecyclingMinigame/banana_peel_recycling"),
        new TrashItemData("Soda Can", BinType.RightBottom, "RecyclingMinigame/soda_can_recycling"),
        new TrashItemData("Cardboard", BinType.LeftTop, "RecyclingMinigame/box_recycling"),
        new TrashItemData("Milk Jug", BinType.RightTop, "RecyclingMinigame/milk_recycling"),
        new TrashItemData("Food Scraps", BinType.LeftBottom, "RecyclingMinigame/food_scraps_recycling"),
        new TrashItemData("Tin Can", BinType.RightBottom, "RecyclingMinigame/tin_recycling")
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
            Debug.LogError("RecyclingMinigame: UIDocument is not assigned.");
            OnMinigameExited?.Invoke();
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("RecyclingMinigame: rootVisualElement is null.");
            OnMinigameExited?.Invoke();
            return;
        }

        isActive = true;
        isResolving = false;
        roundStarted = false;
        score = 0;
        currentItemIndex = 0;
        pendingPollutionReduction = 0;
        trackingDrag = false;

        CacheUI();
        RegisterCallbacks();
        ShowIntroState();

        root.style.display = DisplayStyle.Flex;
    }

    public int ComputePollutionReduction(int finalScore)
    {
        if (finalScore >= 7) return 2;
        if (finalScore >= 4) return 1;
        return 0;
    }

    public int GetScore() => score;
    public int GetItemsSortedCount() => currentItemIndex;
    public int GetTotalItems() => items.Count;
    public string GetCurrentItemLabel() => currentItemIndex < items.Count ? items[currentItemIndex].label : string.Empty;
    public BinType GetCurrentItemTarget() => currentItemIndex < items.Count ? items[currentItemIndex].correctBin : BinType.LeftTop;
    public bool HasStartedRound() => roundStarted;

    public void ForceSortCurrentItemTo(BinType chosenBin)
    {
        if (!isActive || isResolving || !roundStarted || currentItemIndex >= items.Count)
        {
            return;
        }

        ApplySortResult(chosenBin);
    }

    private void CacheUI()
    {
        titleLabel = root.Q<Label>("minigame_title");
        descriptionLabel = root.Q<Label>("minigame_description");

        introContainer = root.Q<VisualElement>("intro_container");
        gameplayContainer = root.Q<VisualElement>("gameplay_container");
        resultContainer = root.Q<VisualElement>("result_container");

        scoreLabel = root.Q<Label>("score_label");
        progressLabel = root.Q<Label>("progress_label");
        feedbackLabel = root.Q<Label>("feedback_label");
        currentTrashLabel = root.Q<Label>("current_trash_label");
        binLeftTop = root.Q<VisualElement>("bin_left_top");
        binLeftBottom = root.Q<VisualElement>("bin_left_bottom");
        binRightTop = root.Q<VisualElement>("bin_right_top");
        binRightBottom = root.Q<VisualElement>("bin_right_bottom");

        resultHeadlineLabel = root.Q<Label>("result_headline");
        resultLabel = root.Q<Label>("result_label");
        resultScoreLabel = root.Q<Label>("result_score");
        resultReductionLabel = root.Q<Label>("result_reduction");

        beginButton = root.Q<Button>("begin_button");
        continueButton = root.Q<Button>("exit_button");
    }

    private void RegisterCallbacks()
    {
        if (beginButton != null)
        {
            beginButton.UnregisterCallback<ClickEvent>(HandleBeginClicked);
            beginButton.RegisterCallback<ClickEvent>(HandleBeginClicked);
        }

        if (continueButton != null)
        {
            continueButton.UnregisterCallback<ClickEvent>(HandleContinueClicked);
            continueButton.RegisterCallback<ClickEvent>(HandleContinueClicked);
        }

        if (currentTrashLabel != null)
        {
            currentTrashLabel.UnregisterCallback<PointerDownEvent>(HandleTrashPointerDown);
            currentTrashLabel.UnregisterCallback<PointerMoveEvent>(HandleTrashPointerMove);
            currentTrashLabel.UnregisterCallback<PointerUpEvent>(HandleTrashPointerUp);
            currentTrashLabel.RegisterCallback<PointerDownEvent>(HandleTrashPointerDown);
            currentTrashLabel.RegisterCallback<PointerMoveEvent>(HandleTrashPointerMove);
            currentTrashLabel.RegisterCallback<PointerUpEvent>(HandleTrashPointerUp);
        }
    }

    private void ShowIntroState()
    {
        if (titleLabel != null)
        {
            titleLabel.text = "Recycling!";
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text =
                "Rules:\n1. Drag each trash item into a bin.\n2. Correct bin: +1 point. Wrong bin: -1 point.\n3. Get 4 points to clean the air by 1. Get 7 points to clean the air by 2.";
            descriptionLabel.style.display = DisplayStyle.Flex;
            descriptionLabel.style.fontSize = 20f;
            descriptionLabel.style.marginTop = 0f;
            descriptionLabel.style.marginBottom = 18f;
            descriptionLabel.style.maxWidth = 760f;
            descriptionLabel.style.paddingLeft = 72f;
            descriptionLabel.style.paddingRight = 72f;
            descriptionLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        }

        if (introContainer != null) introContainer.style.display = DisplayStyle.Flex;
        if (gameplayContainer != null) gameplayContainer.style.display = DisplayStyle.None;
        if (resultContainer != null) resultContainer.style.display = DisplayStyle.None;
    }

    private void StartRound()
    {
        roundStarted = true;
        isResolving = false;
        score = 0;
        currentItemIndex = 0;
        pendingPollutionReduction = 0;
        trackingDrag = false;

        if (feedbackLabel != null)
        {
            feedbackLabel.text = DefaultFeedbackText;
            feedbackLabel.style.color = Color.white;
        }

        if (titleLabel != null)
        {
            titleLabel.text = "Recycling!";
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = "Drag each trash item into the correct bin.";
            descriptionLabel.style.display = DisplayStyle.Flex;
            descriptionLabel.style.fontSize = 18f;
            descriptionLabel.style.marginTop = -15f;
            descriptionLabel.style.marginBottom = 10f;
            descriptionLabel.style.maxWidth = 500f;
            descriptionLabel.style.paddingLeft = 0f;
            descriptionLabel.style.paddingRight = 0f;
            descriptionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        if (introContainer != null) introContainer.style.display = DisplayStyle.None;
        if (gameplayContainer != null) gameplayContainer.style.display = DisplayStyle.Flex;
        if (resultContainer != null) resultContainer.style.display = DisplayStyle.None;

        UpdateGameplayText();
    }

    private void UpdateGameplayText()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = $"Score: {score}";
        }

        if (progressLabel != null)
        {
            progressLabel.text = $"Item {Mathf.Min(currentItemIndex + 1, items.Count)} / {items.Count}";
        }

        if (currentTrashLabel != null)
        {
            if (currentItemIndex < items.Count)
            {
                TrashItemData current = items[currentItemIndex];
                currentTrashLabel.text = current.label;

                Texture2D itemTexture = LoadItemTexture(current.resourcePath);
                if (itemTexture != null)
                {
                    currentTrashLabel.style.backgroundImage = new StyleBackground(itemTexture);
                    ApplyFittedItemSize(itemTexture);
                }
                else
                {
                    currentTrashLabel.style.backgroundImage = StyleKeyword.None;
                    currentTrashLabel.style.width = ItemMaxWidth;
                    currentTrashLabel.style.height = ItemMaxHeight;
                }
            }
            else
            {
                currentTrashLabel.text = string.Empty;
                currentTrashLabel.style.backgroundImage = StyleKeyword.None;
                currentTrashLabel.style.width = ItemMaxWidth;
                currentTrashLabel.style.height = ItemMaxHeight;
            }

            currentTrashLabel.style.translate = new Translate(new Length(0f, LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
        }
    }

    private void ApplyFittedItemSize(Texture2D texture)
    {
        if (currentTrashLabel == null || texture == null)
        {
            return;
        }

        // Preserve original image proportions while fitting in a fixed item area.
        float textureWidth = Mathf.Max(1f, texture.width);
        float textureHeight = Mathf.Max(1f, texture.height);
        float scale = Mathf.Min(ItemMaxWidth / textureWidth, ItemMaxHeight / textureHeight);

        currentTrashLabel.style.width = textureWidth * scale;
        currentTrashLabel.style.height = textureHeight * scale;
    }

    private Texture2D LoadItemTexture(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (textureCache.TryGetValue(resourcePath, out Texture2D cached))
        {
            return cached;
        }

        Texture2D loaded = Resources.Load<Texture2D>(resourcePath);
        textureCache[resourcePath] = loaded;
        return loaded;
    }

    private void HandleBeginClicked(ClickEvent evt)
    {
        if (!isActive || isResolving || roundStarted)
        {
            return;
        }

        StartRound();
    }

    private void HandleContinueClicked(ClickEvent evt)
    {
        if (!isResolving)
        {
            return;
        }

        EndWithResult();
    }

    private void HandleTrashPointerDown(PointerDownEvent evt)
    {
        if (!isActive || isResolving || !roundStarted || currentItemIndex >= items.Count)
        {
            return;
        }

        dragStart = (Vector2)evt.position;
        trackingDrag = true;
        draggingTrash = true;
        dragPointerId = evt.pointerId;

        if (currentTrashLabel != null)
        {
            currentTrashLabel.BringToFront();
            currentTrashLabel.CapturePointer(dragPointerId);
        }

        evt.StopPropagation();
    }

    private void HandleTrashPointerMove(PointerMoveEvent evt)
    {
        if (!isActive || isResolving || !roundStarted || !trackingDrag || !draggingTrash || currentTrashLabel == null)
        {
            return;
        }

        if (evt.pointerId != dragPointerId || !currentTrashLabel.HasPointerCapture(dragPointerId))
        {
            return;
        }

        Vector2 delta = (Vector2)evt.position - dragStart;
        currentTrashLabel.style.translate = new Translate(new Length(delta.x, LengthUnit.Pixel), new Length(delta.y, LengthUnit.Pixel));
        evt.StopPropagation();
    }

    private void HandleTrashPointerUp(PointerUpEvent evt)
    {
        if (!isActive || isResolving || !roundStarted || currentItemIndex >= items.Count || !trackingDrag)
        {
            return;
        }

        trackingDrag = false;
        draggingTrash = false;

        if (currentTrashLabel != null && evt.pointerId == dragPointerId && currentTrashLabel.HasPointerCapture(dragPointerId))
        {
            currentTrashLabel.ReleasePointer(dragPointerId);
        }
        dragPointerId = -1;

        if (TryResolveDropBin((Vector2)evt.position, out BinType droppedBin))
        {
            if (currentTrashLabel != null)
            {
                currentTrashLabel.style.translate = new Translate(new Length(0f, LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
            }

            ApplySortResult(droppedBin);
            evt.StopPropagation();
            return;
        }

        if (currentTrashLabel != null)
        {
            currentTrashLabel.style.translate = new Translate(new Length(0f, LengthUnit.Pixel), new Length(0f, LengthUnit.Pixel));
        }

        ShowTemporaryFeedback("Drop it into a bin!", new Color(1f, 0.85f, 0.4f));
        evt.StopPropagation();
    }

    private bool TryResolveDropBin(Vector2 releasePosition, out BinType bin)
    {
        bin = BinType.LeftTop;
        if (binLeftTop != null && binLeftTop.worldBound.Contains(releasePosition))
        {
            bin = BinType.LeftTop;
            return true;
        }

        if (binLeftBottom != null && binLeftBottom.worldBound.Contains(releasePosition))
        {
            bin = BinType.LeftBottom;
            return true;
        }

        if (binRightTop != null && binRightTop.worldBound.Contains(releasePosition))
        {
            bin = BinType.RightTop;
            return true;
        }

        if (binRightBottom != null && binRightBottom.worldBound.Contains(releasePosition))
        {
            bin = BinType.RightBottom;
            return true;
        }

        return false;
    }

    private void ApplySortResult(BinType chosenBin)
    {
        if (currentItemIndex >= items.Count)
        {
            return;
        }

        TrashItemData current = items[currentItemIndex];
        bool correct = chosenBin == current.correctBin;
        score += correct ? 1 : -1;

        if (feedbackLabel != null)
        {
            ShowTemporaryFeedback(
                correct ? "Correct sort! +1" : "Wrong bin! -1",
                correct ? new Color(0.45f, 1f, 0.45f) : new Color(1f, 0.45f, 0.45f));
        }

        currentItemIndex++;

        if (currentItemIndex >= items.Count)
        {
            ResolveRound();
            return;
        }

        UpdateGameplayText();
    }

    private void ResolveRound()
    {
        if (feedbackResetRoutine != null)
        {
            StopCoroutine(feedbackResetRoutine);
            feedbackResetRoutine = null;
        }

        isResolving = true;
        roundStarted = false;
        pendingPollutionReduction = ComputePollutionReduction(score);

        if (gameplayContainer != null) gameplayContainer.style.display = DisplayStyle.None;
        if (resultContainer != null) resultContainer.style.display = DisplayStyle.Flex;

        if (resultHeadlineLabel != null)
        {
            resultHeadlineLabel.text = score >= 7 ? "Amazing sorting!" : score >= 4 ? "Nice sorting!" : "Keep practicing!";
        }

        if (resultLabel != null)
        {
            resultLabel.text = score >= 7
                ? "You sorted lots of trash correctly."
                : score >= 4
                    ? "Good job! You sorted many items right."
                    : "You can do even better next round.";
        }

        if (resultScoreLabel != null)
        {
            resultScoreLabel.text = $"Final score: {score}";
        }

        if (resultReductionLabel != null)
        {
            resultReductionLabel.text = $"AQHI reduction: {pendingPollutionReduction}";
        }
    }

    private void EndWithResult()
    {
        HideUI();
        isActive = false;
        isResolving = false;
        roundStarted = false;
        OnMinigameComplete?.Invoke(pendingPollutionReduction);
    }

    private void ShowTemporaryFeedback(string message, Color color)
    {
        if (feedbackLabel == null)
        {
            return;
        }

        feedbackLabel.text = message;
        feedbackLabel.style.color = color;

        if (feedbackResetRoutine != null)
        {
            StopCoroutine(feedbackResetRoutine);
        }

        feedbackResetRoutine = StartCoroutine(ResetFeedbackRoutine());
    }

    private IEnumerator ResetFeedbackRoutine()
    {
        yield return new WaitForSeconds(2f);

        if (feedbackLabel != null && isActive && !isResolving && roundStarted)
        {
            feedbackLabel.text = DefaultFeedbackText;
            feedbackLabel.style.color = Color.white;
        }

        feedbackResetRoutine = null;
    }

    private void HideUI()
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}
