using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MultiplayerFinalScorePopup : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public UIDocument finalScoreUiDocument;
    public UIDocument inGameUiDocument;

    private VisualElement root;
    private Label resultsTitle;
    private VisualElement winnerSection;
    private Label winnerLabel;
    private VisualElement winnerContainer;
    private VisualElement secondSection;
    private VisualElement secondContainer;
    private VisualElement thirdSection;
    private VisualElement thirdContainer;
    private Button closeButton;

    void Awake()
    {
        if (finalScoreUiDocument == null) return;

        root = finalScoreUiDocument.rootVisualElement;
        resultsTitle = root.Q<Label>("results_title");
        winnerSection = root.Q<VisualElement>("winner_section");
        winnerLabel = root.Q<Label>("winner_label");
        winnerContainer = root.Q<VisualElement>("winner_container");
        secondSection = root.Q<VisualElement>("second_section");
        secondContainer = root.Q<VisualElement>("second_container");
        thirdSection = root.Q<VisualElement>("third_section");
        thirdContainer = root.Q<VisualElement>("third_container");
        closeButton = root.Q<Button>("close_button");

        // Hide at start
        root.style.display = DisplayStyle.None;

        if (closeButton != null)
        {
            closeButton.clicked += () =>
            {
                SceneManager.LoadScene("MainMenu");
            };
        }
    }

    public void ShowFinalScores(List<Player> players)
    {
        if (root == null || players == null || players.Count == 0)
        {
            Debug.LogWarning("Cannot show multiplayer final scores - missing data");
            return;
        }

        AccessibilitySettingsManager.ApplyLargeTextToDocument(finalScoreUiDocument);

        // Sort players by pollution score (lower is better)
        var rankedPlayers = new List<Player>(players);
        rankedPlayers.Sort((a, b) => a.pollutionScore.CompareTo(b.pollutionScore));

        // Clear containers
        if (winnerContainer != null) winnerContainer.Clear();
        if (secondContainer != null) secondContainer.Clear();
        if (thirdContainer != null) thirdContainer.Clear();

        // Find unique score values for placements
        int lowestScore = rankedPlayers[0].pollutionScore;
        int secondLowestScore = int.MaxValue;
        int thirdLowestScore = int.MaxValue;

        for (int i = 1; i < rankedPlayers.Count; i++)
        {
            if (rankedPlayers[i].pollutionScore > lowestScore && secondLowestScore == int.MaxValue)
            {
                secondLowestScore = rankedPlayers[i].pollutionScore;
            }
            if (rankedPlayers[i].pollutionScore > secondLowestScore && thirdLowestScore == int.MaxValue)
            {
                thirdLowestScore = rankedPlayers[i].pollutionScore;
                break;
            }
        }

        // Categorize players
        List<Player> winners = new List<Player>();
        List<Player> secondPlace = new List<Player>();
        List<Player> thirdPlace = new List<Player>();

        foreach (var player in rankedPlayers)
        {
            if (player.pollutionScore == lowestScore)
                winners.Add(player);
            else if (player.pollutionScore == secondLowestScore)
                secondPlace.Add(player);
            else if (player.pollutionScore == thirdLowestScore)
                thirdPlace.Add(player);
        }

        // Populate winner section
        foreach (var player in winners)
        {
            int playerIndex = players.IndexOf(player);
            AddPlayerResult(winnerContainer, player, playerIndex, "winner");
        }

        // Update winner label for ties
        if (winnerLabel != null)
        {
            winnerLabel.text = winners.Count > 1 ? "🏆 WINNERS (TIE)" : "🏆 WINNER";
        }

        // Populate second place section
        if (secondPlace.Count > 0)
        {
            if (secondSection != null) secondSection.style.display = DisplayStyle.Flex;
            foreach (var player in secondPlace)
            {
                int playerIndex = players.IndexOf(player);
                AddPlayerResult(secondContainer, player, playerIndex, "second");
            }
        }
        else
        {
            if (secondSection != null) secondSection.style.display = DisplayStyle.None;
        }

        // Populate third place section
        if (thirdPlace.Count > 0)
        {
            if (thirdSection != null) thirdSection.style.display = DisplayStyle.Flex;
            foreach (var player in thirdPlace)
            {
                int playerIndex = players.IndexOf(player);
                AddPlayerResult(thirdContainer, player, playerIndex, "third");
            }
        }
        else
        {
            if (thirdSection != null) thirdSection.style.display = DisplayStyle.None;
        }

        // Show popup
        root.style.display = DisplayStyle.Flex;
        if (inGameUiDocument != null)
            inGameUiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    private void AddPlayerResult(VisualElement container, Player player, int playerIndex, string placement)
    {
        if (container == null) return;

        var playerResult = new VisualElement();
        playerResult.AddToClassList("player-result");
        playerResult.AddToClassList($"player-result-{placement}");
        playerResult.AddToClassList($"player-{playerIndex + 1}");

        var playerName = new Label($"Player {playerIndex + 1}");
        playerName.AddToClassList("player-name");

        var playerScore = new Label($"AQHI: {player.pollutionScore}");
        playerScore.AddToClassList("player-score");

        playerResult.Add(playerName);
        playerResult.Add(playerScore);

        container.Add(playerResult);
    }
}
