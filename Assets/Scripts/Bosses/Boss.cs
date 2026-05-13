using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A boss encounter triggered at the end of the board.
/// Create a new asset via Create > Bosses > Boss for each boss.
/// Assign a name, image, and 3 BlueMCQCard questions in the Inspector — no code needed.
///
/// Victory (all correct): -3 pollution.
/// Defeat (at least one wrong): +1 pollution per wrong answer.
/// </summary>
[CreateAssetMenu(fileName = "Boss", menuName = "Bosses/Boss")]
public class Boss : ScriptableObject
{
    public string bossName;
    public Sprite bossImage;

    [Tooltip("The MCQ questions this boss asks the player (recommended: 3).")]
    public List<BlueMCQCard> questions;

    private Player currentBossPlayer;
    private int currentBossPlayerIndex;

    public void SetBossPlayer(Player player, int playerIndex)
    {
        currentBossPlayer = player;
        currentBossPlayerIndex = playerIndex;
    }

    public void OnBossSuccess()
    {
        if (currentBossPlayer == null)
        {
            Debug.LogWarning($"{bossName}: No boss player set!");
            return;
        }
        
        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
        {
            ScoreManager.Instance.AddScoreToPlayer(currentBossPlayer, -3, $"{bossName} defeated!");
        }
        else
        {
            ScoreManager.Instance.AddScore(-3, $"{bossName} defeated!");
        }
        
        Debug.Log($"{bossName} defeated by Player {currentBossPlayerIndex + 1}! Pollution -3.");
    }
    
    public void OnBossFailure(int wrongCount)
    {
        if (currentBossPlayer == null)
        {
            Debug.LogWarning($"{bossName}: No boss player set!");
            return;
        }
        
        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
        {
            ScoreManager.Instance.AddScoreToPlayer(currentBossPlayer, wrongCount, $"{bossName} penalty ({wrongCount} wrong)");
        }
        else
        {
            ScoreManager.Instance.AddScore(wrongCount, $"{bossName} penalty ({wrongCount} wrong)");
        }
        
        Debug.Log($"{bossName} not fully defeated by Player {currentBossPlayerIndex + 1}. Pollution +{wrongCount}.");
    }
}
