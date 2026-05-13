using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class FinalScorePopup : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public UIDocument finalScoreUiDocument;
    public UIDocument inGameUiDocument;

    private VisualElement root;
    private Label finalAqhiLabel;
    private Label finalMessageLabel;
    private Label summaryLabel;
    private ScrollView movesScroll;
    private ScrollView aqhiScroll;
    private Button movesTabButton;
    private Button aqhiTabButton;
    private Button closeButton;

    private const string ActiveTabClass = "tab-button-active";
    private const string HiddenScrollClass = "history-scroll-hidden";

    void Awake()
    {
        if (finalScoreUiDocument == null) return;

        root = finalScoreUiDocument.rootVisualElement;
        finalAqhiLabel = root.Q<Label>("final_aqhi");
        finalMessageLabel = root.Q<Label>("final_message");
        summaryLabel = root.Q<Label>("final_summary");
        movesScroll = root.Q<ScrollView>("moves_scroll");
        aqhiScroll = root.Q<ScrollView>("aqhi_scroll");
        movesTabButton = root.Q<Button>("moves_tab_button");
        aqhiTabButton = root.Q<Button>("aqhi_tab_button");
        closeButton = root.Q<Button>("close_button");

        root.style.display = DisplayStyle.None;

        if (movesTabButton != null)
            movesTabButton.clicked += () => ShowTab(true);

        if (aqhiTabButton != null)
            aqhiTabButton.clicked += () => ShowTab(false);

        if (closeButton != null)
        {
            closeButton.clicked += () =>
            {
                SceneManager.LoadScene("MainMenu");
            };
        }
    }

    public void ShowFinalScores(int finalAqhi, int correct, int incorrect)
    {
        Debug.Log($"FinalScorePopup.ShowFinalScores called. Root is null: {root == null}");
        if (root == null) return;

        // Ensure this UI document appears on top by setting sorting order
        UIDocument thisDocument = GetComponent<UIDocument>();
        if (thisDocument != null)
        {
            thisDocument.sortingOrder = 100;
            Debug.Log("Set FinalScorePopup sorting order to 100");
        }

        Debug.Log("Displaying final score popup...");
        if (finalAqhiLabel != null)
            finalAqhiLabel.text = $"Final AQHI: {finalAqhi}";

        if (finalMessageLabel != null)
            finalMessageLabel.text = GetFinalMessage(finalAqhi);

        int moves = FinalScoreLogger.Instance != null ? FinalScoreLogger.Instance.GetMoveCount() : 0;

        if (summaryLabel != null)
            summaryLabel.text = $"Moves: {moves}   |   Correct: {correct}   |   Incorrect: {incorrect}";

        if (movesScroll != null)
            movesScroll.Clear();

        if (aqhiScroll != null)
            aqhiScroll.Clear();

        var logger = FinalScoreLogger.Instance;
        if (logger != null)
        {
            if (movesScroll != null)
            {
                foreach (var roll in logger.GetRolls())
                {
                    movesScroll.Add(MakeLine($"Move {roll.moveNumber}: Rolled {roll.roll} (Square {roll.startSquare} -> {roll.endSquare})"));
                }
            }

            if (aqhiScroll != null)
            {
                foreach (var scoreChange in logger.GetScoreChanges())
                {
                    string sign = scoreChange.delta >= 0 ? "+" : "";
                    aqhiScroll.Add(MakeLine($"AQHI: {scoreChange.from} -> {scoreChange.to} ({sign}{scoreChange.delta}) - {scoreChange.reason}"));
                }
            }
        }

        if (movesScroll != null && movesScroll.childCount == 0)
            movesScroll.Add(MakeLine("No move history found."));

        if (aqhiScroll != null && aqhiScroll.childCount == 0)
            aqhiScroll.Add(MakeLine("No AQHI history found."));

        ShowTab(true);

        root.style.display = DisplayStyle.Flex;

        if (inGameUiDocument != null)
            inGameUiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private VisualElement MakeLine(string text)
    {
        var label = new Label(text);
        label.AddToClassList("history-line");
        return label;
    }

    private void ShowTab(bool showMoves)
    {
        SetScrollVisible(movesScroll, showMoves);
        SetScrollVisible(aqhiScroll, !showMoves);
        SetTabActive(movesTabButton, showMoves);
        SetTabActive(aqhiTabButton, !showMoves);
    }

    private void SetScrollVisible(VisualElement element, bool isVisible)
    {
        if (element == null) return;

        if (isVisible)
            element.RemoveFromClassList(HiddenScrollClass);
        else
            element.AddToClassList(HiddenScrollClass);
    }

    private void SetTabActive(VisualElement element, bool isActive)
    {
        if (element == null) return;

        if (isActive)
            element.AddToClassList(ActiveTabClass);
        else
            element.RemoveFromClassList(ActiveTabClass);
    }

    private string GetFinalMessage(int aqhi)
    {
        if (aqhi <= 3)
            return "Great job, Air Hero! You helped clean the air!";
        if (aqhi <= 6)
            return "Nice effort! The air is a little cleaner thanks to you.";
        return "The air still needs your help -- try again and clean it up!";
    }
}
