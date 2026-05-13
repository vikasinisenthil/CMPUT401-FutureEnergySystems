using System.Collections.Generic;
using UnityEngine;

public class BlueCardManager : MonoBehaviour
{
    [Header("Runtime deck (auto-filled)")]
    public List<BlueCard> deck = new List<BlueCard>();

    void Start()
    {
        if (GameManager.Instance != null)
        {
            SetDifficultyAndBuildDeck(GameManager.Instance.difficulty);
        }
        else
        {
            Debug.LogWarning("GameManager.Instance is null. Defaulting BlueCard deck to Easy.");
            SetDifficultyAndBuildDeck(Difficulty.Easy);
        }
    }

    public void SetDifficultyAndBuildDeck(Difficulty difficulty)
    {
        string path = difficulty switch
        {
            Difficulty.Easy => "BlueCards/Easy",
            Difficulty.Medium => "BlueCards/Medium",
            Difficulty.Hard => "BlueCards/Hard",
            _ => "BlueCards/Easy"
        };

        deck = new List<BlueCard>(Resources.LoadAll<BlueCard>(path));
        Debug.Log($"Loaded {deck.Count} blue cards from Resources/{path}");
    }

    public BlueCard DrawCard()
    {
        if (deck == null || deck.Count == 0)
        {
            SetDifficultyAndBuildDeck(GameManager.Instance.difficulty);
            return DrawCard();
        }

        int cardIndex = Random.Range(0, deck.Count);
        BlueCard c = deck[cardIndex];
        deck.RemoveAt(cardIndex);
        return c;
    }
}