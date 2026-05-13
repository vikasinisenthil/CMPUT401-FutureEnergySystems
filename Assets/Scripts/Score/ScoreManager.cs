using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public UIDocument inGameUiDocument;
    
    // Singleplayer references
    private Label singleplayerScoreLabel;
    private VisualElement singleplayerProgressBar;
    private int singleplayerScore = 0;
    
    private const int MAX_DISPLAY_SCORE = 10;

    void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject);
        } 
        else 
        {
            Instance = this;
        }
    }

    void Start()
    {
        StartCoroutine(InitializeAfterGameManager());
    }

    private IEnumerator InitializeAfterGameManager()
    {
        while (GameManager.Instance == null) yield return null;
        yield return null;

        if (inGameUiDocument != null) 
        {
            // Setup for singleplayer
            if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Singleplayer)
            {
                // Query inside the score-1 container
                var scoreBox = inGameUiDocument.rootVisualElement.Q<VisualElement>("score-1");
                if (scoreBox != null)
                {
                    singleplayerScoreLabel = scoreBox.Q<Label>("score_label");
                    singleplayerProgressBar = scoreBox.Q<VisualElement>("progress_bar");
                }
                
                Debug.Log($"Singleplayer setup - scoreLabel: {singleplayerScoreLabel != null}, progressBar: {singleplayerProgressBar != null}");
                
                // Set character avatar
                var characterAvatar = scoreBox?.Q<Image>("character_avatar");
                if (characterAvatar != null && 
                    GameManager.Instance.SelectedHeroes != null && 
                    GameManager.Instance.SelectedHeroes.Length > 0)
                {
                    var heroType = GameManager.Instance.SelectedHeroes[0];
                    var sprites = GameManager.Instance.GetCharacterSprites(heroType, 0);
                    if (sprites != null && sprites.idleSprite != null)
                    {
                        characterAvatar.image = sprites.idleSprite.texture;
                    }
                }

                // Hide score-2 and score-3 in singleplayer
                var score2Box = inGameUiDocument.rootVisualElement.Q<VisualElement>("score-2");
                var score3Box = inGameUiDocument.rootVisualElement.Q<VisualElement>("score-3");
                if (score2Box != null) score2Box.style.display = DisplayStyle.None;
                if (score3Box != null) score3Box.style.display = DisplayStyle.None;
                
                UpdateSingleplayerScore();
            }
        }
    }

    // For singleplayer
    public int GetScore() 
    {
        return singleplayerScore;
    }

    public void SetScore(int newScore) 
    {
        singleplayerScore = Mathf.Max(newScore, 0);
        UpdateSingleplayerScore();
    }

    // For singleplayer
    public void AddScore(int amount, string reason = "") 
    {
        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
        {
            Debug.LogWarning("Use AddScoreToPlayer for multiplayer mode");
            return;
        }
        
        int before = singleplayerScore;
        singleplayerScore = Mathf.Max(singleplayerScore + amount, 0);
        UpdateSingleplayerScore();
        
        Debug.Log("Player AQHI changed by " + amount + ", New AQHI: " + singleplayerScore);
        
        if (FinalScoreLogger.Instance != null && before != singleplayerScore) 
        {
            FinalScoreLogger.Instance.LogScoreChange(before, singleplayerScore, singleplayerScore - before, reason ?? $"Score change ({amount})");
        }
    }

    // For multiplayer - add score to specific player
    public void AddScoreToPlayer(Player player, int amount, string reason = "")
    {
        if (player == null) 
        {
            Debug.LogWarning("AddScoreToPlayer: player is null");
            return;
        }
        
        int before = player.pollutionScore;
        player.pollutionScore = Mathf.Max(player.pollutionScore + amount, 0);
        
        Debug.Log($"Player AQHI changed by {amount}, New AQHI: {player.pollutionScore}");
        
        UpdatePlayerScore(player);
        
        if (FinalScoreLogger.Instance != null && before != player.pollutionScore)
        {
            FinalScoreLogger.Instance.LogScoreChange(before, player.pollutionScore, player.pollutionScore - before, reason ?? $"Score change ({amount})");
        }
    }

    public int GetPlayerScore(Player player)
    {
        return player != null ? player.pollutionScore : 0;
    }

    // Update singleplayer score UI
    private void UpdateSingleplayerScore() 
    {
        if (singleplayerScoreLabel == null || singleplayerProgressBar == null)
        {
            Debug.LogWarning($"Cannot update singleplayer UI - scoreLabel: {singleplayerScoreLabel != null}, progressBar: {singleplayerProgressBar != null}");
            return;
        }

        if (singleplayerScore >= MAX_DISPLAY_SCORE) 
        {
            singleplayerScoreLabel.text = "AQHI: Too High!";
        } 
        else 
        {
            singleplayerScoreLabel.text = $"AQHI: {singleplayerScore}";
        }

        float percentage = Mathf.Min((float)singleplayerScore / MAX_DISPLAY_SCORE * 100f, 100f);
        singleplayerProgressBar.style.width = Length.Percent(percentage);

        if (singleplayerScore <= 3) 
        {
            singleplayerProgressBar.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        } 
        else if (singleplayerScore <= 6) 
        {
            singleplayerProgressBar.style.backgroundColor = new Color(0.9f, 0.9f, 0.2f, 0.5f);
        } 
        else 
        {
            singleplayerProgressBar.style.backgroundColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);
        }
        
        Debug.Log($"Updated singleplayer UI - text: {singleplayerScoreLabel.text}, width: {percentage}%");
        
        UpdatePlayerSprite();
    }

    // Update multiplayer player score UI
    private void UpdatePlayerScore(Player player)
    {
        if (player == null || player.scoreLabel == null || player.progressBar == null) 
        {
            Debug.LogWarning($"Cannot update player UI - player: {player != null}, scoreLabel: {player?.scoreLabel != null}, progressBar: {player?.progressBar != null}");
            return;
        }

        GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
        int playerIndex = -1;
        if (gc != null)
        {
            playerIndex = gc.players.IndexOf(player);
        }

        if (player.pollutionScore >= MAX_DISPLAY_SCORE) 
        {
            player.scoreLabel.text = $"P{playerIndex + 1} AQHI: Too High!";
        } 
        else 
        {
            player.scoreLabel.text = $"P{playerIndex + 1} AQHI: {player.pollutionScore}";
        }

        float percentage = Mathf.Min((float)player.pollutionScore / MAX_DISPLAY_SCORE * 100f, 100f);
        player.progressBar.style.width = Length.Percent(percentage);

        if (player.pollutionScore <= 3) 
        {
            player.progressBar.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        } 
        else if (player.pollutionScore <= 6) 
        {
            player.progressBar.style.backgroundColor = new Color(0.9f, 0.9f, 0.2f, 0.5f);
        } 
        else 
        {
            player.progressBar.style.backgroundColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);
        }

        UpdatePlayerSpriteForPlayer(player);
    }

    private void UpdatePlayerSprite() 
    {
        GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
        if (gc != null && gc.player != null && gc.player.gameObject != null) 
        {
            PlayerAnimator animator = gc.player.gameObject.GetComponent<PlayerAnimator>();
            if (animator != null && !gc.player.moving) 
            {
                animator.SetWalking(false);
            }
        }
    }

    private void UpdatePlayerSpriteForPlayer(Player player)
    {
        if (player != null && player.gameObject != null)
        {
            PlayerAnimator animator = player.gameObject.GetComponent<PlayerAnimator>();
            if (animator != null && !player.moving)
            {
                animator.SetWalking(false);
            }
        }
    }
}