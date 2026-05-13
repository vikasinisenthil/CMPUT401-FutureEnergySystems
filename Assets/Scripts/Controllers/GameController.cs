using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class Player
{
    public GameObject gameObject;
    public float movementSpeed = 6.0f;

    public bool moving { get; set; }
    public int boardSquareIndex { get; set; }
    public int nextBoardSquareIndex { get; set; }
    public int finalBoardSquareIndex { get; set; }

    public int change_direction { get; set; } = 1;
    public bool trigger_when_done { get; set; } = true;
    public bool show_dice_countdown { get; set; } = true;
    public int pollutionScore;

    public HeroType heroType;
    public int forecastingUses = 2;

    public Label scoreLabel;
    public VisualElement progressBar;

    public bool hasFinished { get; set; } = false;
    public int finishOrder { get; set; } = 0;
}

public class GameController : MonoBehaviour
{
    // TODO: For now set all of this up in the editor, in future these can all be instantiated with a script
    public List<GameObject> boardSquares;

    public Player player;
    public List<Player> players = new List<Player>();
    public int currentPlayerIndex = 0;

    public UIDocument inGameUiDocument;

    [Header("Final Score Popups")]
    public FinalScorePopup singlePlayerFinalScorePopup;
    public MultiplayerFinalScorePopup multiPlayerFinalScorePopup;
    public FinalScorePopup finalScorePopup;

    public const string PREF_SKIP_DICE_ANIMATION_FOR_TESTS = "SkipDiceAnimationForTests";

    private bool gameEnded = false;
    private int finishCounter = 0;
    private bool _isDiceRolling = false;

    public UIDocument blueCardUiDocument;
    public UIDocument grayCardUiDocument;

    private EventCallback<ClickEvent> grayCloseButtonCallback;
    private EventCallback<ClickEvent> closeButtonCallback;

    /// <summary>ABE.11 Traffic Weaver: set when opening grey overlay; Cyclist skips +1 penalty if true.</summary>
    private GreySpotType currentGraySpotType = GreySpotType.Generic;
    private GameObject currentGreenMinigameObject;

    public int triviaCorrectCount = 0;
    public int triviaIncorrectCount = 0;

    private void SetRollDiceButtonTextMode(Button rollDiceButton, string text = "Roll\nDice")
    {
        if (rollDiceButton == null) return;

        rollDiceButton.text = text;
        rollDiceButton.style.fontSize = new StyleLength(StyleKeyword.Null);
        rollDiceButton.RemoveFromClassList("dice-number-mode");
        rollDiceButton.AddToClassList("dice-text-mode");
    }

    private void SetRollDiceButtonNumberMode(Button rollDiceButton, string value)
    {
        if (rollDiceButton == null) return;

        rollDiceButton.text = value;
        rollDiceButton.style.fontSize = new StyleLength(StyleKeyword.Null);
        rollDiceButton.RemoveFromClassList("dice-text-mode");
        rollDiceButton.AddToClassList("dice-number-mode");
    }

    private void ApplyLargeTextToBoardDocuments()
    {
        AccessibilitySettingsManager.ApplyLargeTextToDocument(inGameUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(grayCardUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(blueCardUiDocument);

        UIDocument factDoc = GameObject.Find("BlueFactCardUIDocument")?.GetComponent<UIDocument>();
        UIDocument mcqDoc = GameObject.Find("BlueMCQCardUIDocument")?.GetComponent<UIDocument>();
        UIDocument bossDoc = GameObject.Find("BossUIDocument")?.GetComponent<UIDocument>();

        AccessibilitySettingsManager.ApplyLargeTextToDocument(factDoc);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(mcqDoc);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(bossDoc);
    }

    public void LandedOnBlueSquare()
    {
        BlueCard card = GetComponent<BlueCardManager>().DrawCard();
        if (card == null)
        {
            Debug.LogWarning("No more blue cards to draw");
            return;
        }

        blueCardUiDocument = card.GetUiDocument();
        AccessibilitySettingsManager.ApplyLargeTextToDocument(blueCardUiDocument);
        blueCardUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        inGameUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        inGameUiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        blueCardUiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        Debug.Log("Blue card opened");
    }

    /// <summary>
    /// ABE.01: Called when the player lands on a green square.
    /// Triggers the specific minigame assigned to that square via MinigameManager.
    /// The player cannot continue their turn until the minigame is completed or exited.
    /// - If completed: pollution score is reduced based on minigame performance result.
    /// - If exited or failed to load: no score change, game resumes.
    /// The updated pollution score is displayed after the minigame ends.
    /// </summary>
    /// <param name="minigameObject">The specific minigame GameObject assigned to this green square.
    /// If null, falls back to launching a random minigame from the pool.</param>
    public void LandedOnGreenSquare(GameObject minigameObject = null)
    {
        currentGreenMinigameObject = minigameObject;
        MinigameManager minigameManager = GetComponent<MinigameManager>();

        if (minigameManager == null)
        {
            Debug.LogWarning("GameController: No MinigameManager found. Skipping minigame for green square.");
            return;
        }

        // Hide the dice button to block the player from rolling during the minigame
        var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
        if (rollDiceButton != null)
        {
            rollDiceButton.SetEnabled(false);
        }

        // Subscribe to minigame result events
        minigameManager.OnMinigameResult += HandleMinigameCompleted;
        minigameManager.OnMinigameNoResult += HandleMinigameExited;

        // Launch the specific minigame for this square, or fall back to random
        bool launched;
        if (minigameObject != null)
        {
            launched = minigameManager.LaunchMinigame(minigameObject);
        }
        else
        {
            launched = minigameManager.LaunchRandomMinigame();
        }

        if (!launched)
        {
            // If launch failed, MinigameManager already invoked OnMinigameNoResult,
            // which calls HandleMinigameExited to resume the game.
            Debug.Log("GameController: Minigame failed to launch. Game will resume.");
        }
    }

    /// <summary>
    /// ABE.01: Handles successful minigame completion.
    /// Reduces the player's pollution score by the returned amount (via ScoreManager, clamped to >= 0).
    /// ABE.11 (Cyclist Speed Boost): When the Cyclist lands on any green square (minigame), they move +1 extra space after the minigame completes.
    /// </summary>
    private void HandleMinigameCompleted(int pollutionReduction)
    {
        UnsubscribeFromMinigameEvents();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayConfirm();

        int finalReduction = pollutionReduction;

        bool isPlantTrees =
            currentGreenMinigameObject != null &&
            currentGreenMinigameObject.name.ToLower().Contains("plant");

        if (player.heroType == HeroType.Ranger && isPlantTrees)
        {
            finalReduction *= 2;
            Debug.Log("Ranger Photosynthesis Boost: double pollution removal on Plant Trees.");
        }

        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer) {
            ScoreManager.Instance.AddScoreToPlayer(player, -finalReduction, "Green square minigame completed");
        } else {
            ScoreManager.Instance.AddScore(-finalReduction, "Green square minigame completed");
        }
    
        Debug.Log(
            $"GameController: Minigame completed. Pollution reduced by {finalReduction}. " +
            $"New score: {ScoreManager.Instance.GetScore()}"
        );

        currentGreenMinigameObject = null;

        // Cyclist still gets +1 on green squares, but this bonus move should
        // only advance position and must not trigger the destination tile effect.
        if (player.heroType == HeroType.Cyclist)
        {
            bool moved = MovePlayer(1, triggerLandOn: false, showCountdown: false);
            if (!moved)
                ResumeGameAfterMinigame();

            return;
        }

        ResumeGameAfterMinigame();
    }

    /// <summary>
    /// ABE.01: Handles minigame exit or failure to load.
    /// No score change occurs. Normal gameplay resumes.
    /// </summary>
    private void HandleMinigameExited()
    {
        UnsubscribeFromMinigameEvents();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayConfirm();

        currentGreenMinigameObject = null;

        Debug.Log("GameController: Minigame exited or failed to load. No score change. Resuming game.");

        ResumeGameAfterMinigame();
    }

    /// <summary>
    /// ABE.01: Unsubscribes from MinigameManager events to prevent duplicate event handling.
    /// </summary>
    private void UnsubscribeFromMinigameEvents()
    {
        MinigameManager minigameManager = GetComponent<MinigameManager>();
        if (minigameManager != null)
        {
            minigameManager.OnMinigameResult -= HandleMinigameCompleted;
            minigameManager.OnMinigameNoResult -= HandleMinigameExited;
        }
    }

    /// <summary>
    /// ABE.01: Re-enables the dice button after a minigame ends, allowing the player to continue.
    /// Also used when closing blue/gray UI after a Cyclist bonus move landed on blue or gray.
    /// </summary>
    public void ResumeGameAfterMinigame()
    {
        var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
        if (rollDiceButton != null)
        {
            rollDiceButton.SetEnabled(true);
        }

        // In multiplayer, end turn after minigame/card
        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
        {
            if (!player.moving)
            {
                EndTurn();
            }
        }
    }

    /// <summary>
    /// ABE.11 Traffic Weaver: when isCarTraffic is true and the hero is Cyclist, no pollution penalty is applied.
    /// Spots 13 and 14 (map) are Car/Traffic grey; BoardSquare passes this based on greySpotType.
    /// </summary>
    public void LandedOnGraySquare(bool isCarTraffic)
    {
        LandedOnGraySquare(isCarTraffic ? GreySpotType.CarTraffic : GreySpotType.Generic, "");
    }
    public void LandedOnGraySquare(GreySpotType greySpotType = GreySpotType.Generic, string customText = "")
    {
        currentGraySpotType = greySpotType;

        AccessibilitySettingsManager.ApplyLargeTextToDocument(grayCardUiDocument);

        var root = grayCardUiDocument.rootVisualElement.Q<VisualElement>("overlay");
        root.style.display = DisplayStyle.Flex;

        // Set custom text or default
        var cardText = root.Q<Label>("card_text");
        if (cardText != null)
        {
            if (!string.IsNullOrEmpty(customText))
            {
                cardText.text = customText;
            }
            else
            {
                cardText.text = "You are near a pollution source";
            }
        }

        Debug.Log($"Gray card opened ({greySpotType})");

        var closeButton = root.Q<Button>("close_button");
        if (grayCloseButtonCallback != null)
        {
            closeButton.UnregisterCallback(grayCloseButtonCallback);
        }

        grayCloseButtonCallback = e =>
        {
            root.style.display = DisplayStyle.None;

            bool cyclistTrafficImmunity =
                currentGraySpotType == GreySpotType.CarTraffic &&
                player.heroType == HeroType.Cyclist;

            bool rangerWildfireImmunity =
                currentGraySpotType == GreySpotType.Wildfire &&
                player.heroType == HeroType.Ranger;

            bool skipPenalty = cyclistTrafficImmunity || rangerWildfireImmunity;

            if (!skipPenalty)
            {
                if (GameManager.Instance.Mode == GameMode.Multiplayer) {
                    ScoreManager.Instance.AddScoreToPlayer(player, 1, "Gray square penalty");
                } else {
                    ScoreManager.Instance.AddScore(1, "Gray square penalty");
                }
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayCough();
            }
            else
            {
                if (cyclistTrafficImmunity)
                    Debug.Log("Cyclist Traffic Weaver: no pollution on Car/Traffic grey.");

                if (rangerWildfireImmunity)
                    Debug.Log("Ranger Fire Resistance: no pollution on Wildfire grey.");
            }

            Debug.Log("Gray card closed");
            ResumeGameAfterMinigame();
        };

        closeButton.RegisterCallback(grayCloseButtonCallback);
    }

    public void MCQQuestionAnsweredCorrectly()
    {
        triviaCorrectCount++;
        Debug.Log($"MCQ answered correctly. Total correct: {triviaCorrectCount}");
    }

    public void MCQQuestionAnsweredIncorrectly()
    {
        triviaIncorrectCount++;
        Debug.Log($"MCQ answered incorrectly. Total incorrect: {triviaIncorrectCount}");
    }

    void Start()
    {
        // Determine game mode
        bool isMultiplayer = GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer;

        // Show appropriate UI container
        var uiContainer = inGameUiDocument.rootVisualElement.Q<VisualElement>("ui-container");
        if (uiContainer != null)
        {
            uiContainer.style.display = DisplayStyle.Flex;
        }
        else
        {
            inGameUiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        // Show/hide appropriate player GameObjects
        if (isMultiplayer)
        {
            // Hide singleplayer Player
            GameObject singlePlayer = GameObject.Find("Player");
            if (singlePlayer != null)
                singlePlayer.SetActive(false);
            
            // Show and activate multiplayer players based on PlayerCount
            for (int i = 1; i <= 3; i++)
            {
                GameObject playerObj = GameObject.Find($"Player{i}");
                if (playerObj != null)
                {
                    playerObj.SetActive(i <= GameManager.Instance.PlayerCount);
                }
            }
        }
        else
        {
            // Show singleplayer Player
            GameObject singlePlayer = GameObject.Find("Player");
            if (singlePlayer != null)
                singlePlayer.SetActive(true);
            
            // Hide all multiplayer players
            for (int i = 1; i <= 3; i++)
            {
                GameObject playerObj = GameObject.Find($"Player{i}");
                if (playerObj != null)
                    playerObj.SetActive(false);
            }
        }

        Vector3 startPos = boardSquares[0].GetComponent<Transform>().position;
        startPos.z = -0.5f;
        player.gameObject.GetComponent<Transform>().position = startPos;

        player.boardSquareIndex = 0;
        player.moving = false;
        player.pollutionScore = 0;

        // Grey spot type setup
        if (boardSquares != null && boardSquares.Count > 14)
        {
            var sq13 = boardSquares[12].GetComponent<BoardSquare>();
            var sq14 = boardSquares[13].GetComponent<BoardSquare>();

            if (sq13 != null) sq13.greySpotType = GreySpotType.CarTraffic;
            if (sq14 != null) sq14.greySpotType = GreySpotType.CarTraffic;
        }
        
        if (boardSquares != null && boardSquares.Count > 37)
        {
            var sq2 = boardSquares[1].GetComponent<BoardSquare>();
            var sq3 = boardSquares[2].GetComponent<BoardSquare>();
            var sq37 = boardSquares[36].GetComponent<BoardSquare>();

            if (sq2 != null) sq2.greySpotType = GreySpotType.Wildfire;
            if (sq3 != null) sq3.greySpotType = GreySpotType.Wildfire;
            if (sq37 != null) sq37.greySpotType = GreySpotType.Wildfire;
        }

        // Setup players based on mode
        if (isMultiplayer)
        {
            SetupMultiplayer();
        }
        else
        {
            SetupSingleplayer();
        }
        
        ApplyCharacter();

        var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
        rollDiceButton.RegisterCallback<ClickEvent>(RollDice);
        SetRollDiceButtonTextMode(rollDiceButton);

        ApplyLargeTextToBoardDocuments();

        // Blue card UI
        GameObject.Find("BlueFactCardUIDocument").GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;
        GameObject.Find("BlueMCQCardUIDocument").GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;

        finishCounter = 0;
        FinalScoreLogger.Instance?.ResetLog();
        gameEnded = false;

        UpdateTurnIndicator();
    }

    private void SetupSingleplayer()
    {
        Vector3 startPos = boardSquares[0].GetComponent<Transform>().position;
        startPos.z = -0.5f;
        player.gameObject.GetComponent<Transform>().position = startPos;

        player.boardSquareIndex = 0;
        player.moving = false;
        player.pollutionScore = 0;

        player.scoreLabel = null;
        player.progressBar = null;
    }

    private void SetupMultiplayer()
    {
        players.Clear();
        
        if (inGameUiDocument != null)
        {
            for (int i = 1; i <= 3; i++)
            {
                var scoreBox = inGameUiDocument.rootVisualElement.Q<VisualElement>($"score-{i}");
                if (scoreBox != null)
                {
                    scoreBox.style.display = i <= GameManager.Instance.PlayerCount ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        for (int i = 0; i < GameManager.Instance.PlayerCount; i++)
        {
            GameObject playerObj = GameObject.Find($"Player{i + 1}");
            if (playerObj == null)
            {
                Debug.LogError($"Player{i + 1} GameObject not found!");
                continue;
            }

            // Get UI elements for this player
            var scoreBox = inGameUiDocument.rootVisualElement.Q<VisualElement>($"score-{i + 1}");
            Label scoreLabel = scoreBox?.Q<Label>("score_label");
            VisualElement progressBar = scoreBox?.Q<VisualElement>("progress_bar");
            
            // Set character avatar
            var characterAvatar = scoreBox?.Q<Image>("character_avatar");
            if (characterAvatar != null)
            {
                var sprites = GameManager.Instance.GetCharacterSprites(GameManager.Instance.SelectedHeroes[i], i);
                if (sprites != null && sprites.idleSprite != null)
                {
                    characterAvatar.image = sprites.idleSprite.texture;
                }
            }

            Player p = new Player
            {
                gameObject = playerObj,
                boardSquareIndex = 0,
                moving = false,
                pollutionScore = 0,
                heroType = GameManager.Instance.SelectedHeroes[i],
                forecastingUses = GameManager.Instance.SelectedHeroes[i] == HeroType.Scientist ? 2 : 0,
                scoreLabel = scoreLabel,
                progressBar = progressBar
            };

            Vector3 startPos = boardSquares[0].GetComponent<Transform>().position;
            startPos.z = -0.5f;
            playerObj.GetComponent<Transform>().position = startPos;

            players.Add(p);
            ScoreManager.Instance.AddScoreToPlayer(p, 0, "Initialize");
        }

        currentPlayerIndex = 0;
        player = players[currentPlayerIndex];
    }

    private void EndTurn()
    {
        if (GameManager.Instance == null || GameManager.Instance.Mode != GameMode.Multiplayer)
            return;

        Debug.Log($"Player {currentPlayerIndex + 1} turn ended");

        // Find next player who hasn't finished
        int startIndex = currentPlayerIndex;
        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            player = players[currentPlayerIndex];
            
            // If we've looped back to start and everyone is finished, end game
            if (currentPlayerIndex == startIndex && player.hasFinished)
            {
                // All players finished
                EndGame();
                return;
            }
        }
        while (player.hasFinished); // Skip finished players

        Debug.Log($"Now Player {currentPlayerIndex + 1}'s turn");
        UpdateTurnIndicator();
    }

    private void UpdateTurnIndicator()
    {
        var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
        if (rollDiceButton == null) return;

        // Check if current player has finished
        if (player.hasFinished)
        {
            rollDiceButton.SetEnabled(false);
            SetRollDiceButtonTextMode(rollDiceButton, "Finished!");
            return;
        }
        
        if (!player.moving)
        {
            if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
            {
                SetRollDiceButtonTextMode(rollDiceButton, $"P{currentPlayerIndex + 1}\nRoll\nDice");
            }
            else
            {
                SetRollDiceButtonTextMode(rollDiceButton, "Roll\nDice");
            }
            
            rollDiceButton.SetEnabled(true);
        }
    }

    void Update()
    {
        if (player == null || player.gameObject == null) return;

        PlayerAnimator animator = player.gameObject.GetComponent<PlayerAnimator>();
        SpriteRenderer spriteRenderer = player.gameObject.GetComponent<SpriteRenderer>();

        if (player.moving)
        {
            if (animator != null) animator.SetWalking(true);

            Vector3 playerPos = player.gameObject.GetComponent<Transform>().position;
            Vector3 targetPos = boardSquares[player.nextBoardSquareIndex].GetComponent<Transform>().position;
            targetPos.z = -0.5f;

            Vector3 playerToTarget = targetPos - playerPos;
            Vector3 playerToTargetDirection = Vector3.Normalize(playerToTarget);

            if (spriteRenderer != null)
            {
                if (playerToTargetDirection.x < 0)
                {
                    spriteRenderer.flipX = true;
                }
                else if (playerToTargetDirection.x > 0)
                {
                    spriteRenderer.flipX = false;
                }
            }

            float distanceRemaining = Vector3.Distance(targetPos, playerPos);
            Vector3 distanceToMove = playerToTargetDirection * player.movementSpeed * Time.deltaTime;
            distanceToMove = Vector3.ClampMagnitude(distanceToMove, distanceRemaining);

            player.gameObject.GetComponent<Transform>().Translate(distanceToMove);

            if (Vector3.Magnitude(playerToTarget) < 0.01f)
            {
                player.gameObject.GetComponent<Transform>().position = targetPos;
                
                var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");

                if (player.nextBoardSquareIndex == player.finalBoardSquareIndex)
                {
                    player.moving = false;
                    if (animator != null) animator.SetWalking(false);

                    player.boardSquareIndex = player.nextBoardSquareIndex;
                    
                    if (player.boardSquareIndex >= boardSquares.Count - 1)
                    {
                        // Player reached the end - start boss battle for this player
                        StartBossBattleForPlayer();
                        return;
                    }

                    UpdateTurnIndicator();

                    // Force camera to recalculate overview when movement stops
                    var cameraController = Camera.main?.GetComponent<CameraController>();
                    if (cameraController != null)
                    {
                        cameraController.UpdateOverviewPosition();
                    }

                    if (player.trigger_when_done)
                    {
                        boardSquares[player.boardSquareIndex].GetComponent<BoardSquare>().LandOn();
                    }
                    else
                    {
                        // Movement without landing still needs to resume the board flow.
                        ResumeGameAfterMinigame();
                    }

                    return;
                }

                player.nextBoardSquareIndex += player.change_direction;

                if (player.show_dice_countdown && int.TryParse(rollDiceButton.text, out int lastRollNumber))
                {
                    lastRollNumber = Mathf.Max(0, lastRollNumber - 1);
                    SetRollDiceButtonNumberMode(rollDiceButton, lastRollNumber.ToString());
                }

                return;
            }
        }
    }

    private void ApplyCharacter()
    {
        if (GameManager.Instance == null || 
            GameManager.Instance.SelectedHeroes == null || 
            GameManager.Instance.SelectedHeroes.Length == 0)
        {
            Debug.LogWarning("No character selected or GameManager not found!");
            return;
        }

        // Get player count based on game mode
        int playerCount = GameManager.Instance.PlayerCount;
        
        // Apply character for each player
        for (int i = 0; i < playerCount; i++)
        {
            if (i >= GameManager.Instance.SelectedHeroes.Length)
            {
                Debug.LogWarning($"No hero selected for player {i + 1}");
                continue;
            }

            // Find the player GameObject by name (Player1, Player2, Player3)
            GameObject playerObj;
            if (playerCount == 1) {
                playerObj = GameObject.Find("Player");
            } else {
                playerObj = GameObject.Find($"Player{i + 1}");
            }
            
            if (playerObj == null) {
                Debug.LogWarning($"Player GameObject not found in scene! Looking for: {(playerCount == 1 ? "Player" : $"Player{i + 1}")}");
                continue;
            }

            HeroType selectedHero = GameManager.Instance.SelectedHeroes[i];
            CharacterSprites sprites = GameManager.Instance.GetCharacterSprites(selectedHero, i);
            
            if (sprites == null) {
                Debug.LogError($"Character sprites not found for {selectedHero}!");
                continue;
            }

            // Remove mesh components if they exist
            var meshRenderer = playerObj.GetComponent<MeshRenderer>();
            if (meshRenderer != null) DestroyImmediate(meshRenderer);
            
            var meshFilter = playerObj.GetComponent<MeshFilter>();
            if (meshFilter != null) DestroyImmediate(meshFilter);

            // Setup sprite renderer
            SpriteRenderer spriteRenderer = playerObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = playerObj.AddComponent<SpriteRenderer>();
            }

            if (sprites.idleSprite != null)
            {
                spriteRenderer.sprite = sprites.idleSprite;
                spriteRenderer.sortingOrder = 10 + i; // Different sorting order per player
                spriteRenderer.flipX = true;
                playerObj.transform.localScale = new Vector3(0.03f, 0.03f, 1f);
            }
            else
            {
                Debug.LogError($"Idle sprite for {selectedHero} is not assigned!");
            }

            // Setup animator
            PlayerAnimator animator = playerObj.GetComponent<PlayerAnimator>();
            if (animator == null)
            {
                animator = playerObj.AddComponent<PlayerAnimator>();
            }
            
            animator.playerIndex = i;
            animator.Initialize(sprites);

            Debug.Log($"Applied character: {selectedHero} to Player{i + 1}");
        }
        
        // Set player reference to Player1 for backwards compatibility
        if (player != null && GameManager.Instance.SelectedHeroes.Length > 0)
        {
            player.heroType = GameManager.Instance.SelectedHeroes[0];
            if (player.heroType == HeroType.Scientist)
            {
                player.forecastingUses = 2;
            }
        }
    }

    void RollDice(ClickEvent e)
    {
        if (gameEnded) return;
        if (player.hasFinished) return;
        if (player.moving) return;
        if (_isDiceRolling) return;

        // Already at end of board (e.g. after losing boss): show final stats so the roll does something.
        if (player.boardSquareIndex >= boardSquares.Count - 1)
        {
            ShowFinalScore();
            return;
        }

        int roll = UnityEngine.Random.Range(1, 7);
        int startSquare = player.boardSquareIndex;
        int endSquare = Mathf.Clamp(startSquare + roll, 0, boardSquares.Count - 1);

        _isDiceRolling = true;
        var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
        rollDiceButton.SetEnabled(false);

        // Tests that assert synchronous movement set this flag to skip the animation.
        if (PlayerPrefs.GetInt(PREF_SKIP_DICE_ANIMATION_FOR_TESTS, 0) == 1)
        {
            _isDiceRolling = false;
            FinalScoreLogger.Instance?.LogRoll(roll, startSquare, endSquare);
            if (MovePlayer(roll, triggerLandOn: true, showCountdown: true))
                SetRollDiceButtonNumberMode(rollDiceButton, roll.ToString());
            else
                rollDiceButton.SetEnabled(true);
            return;
        }

        AudioManager.Instance?.PlayDiceRoll();
        StartCoroutine(DiceRollAnimation(rollDiceButton, roll, startSquare, endSquare));
    }

    private IEnumerator DiceRollAnimation(Button rollDiceButton, int roll, int startSquare, int endSquare)
    {
        // Phase 1: rapidly cycle random numbers to simulate the dice tumbling.
        float rollDuration = 0.7f;
        float frameInterval = 0.075f;
        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            SetRollDiceButtonNumberMode(rollDiceButton, UnityEngine.Random.Range(1, 7).ToString());
            yield return new WaitForSeconds(frameInterval);
            elapsed += frameInterval;
        }

        // Phase 2: show the final result for a beat before moving.
        SetRollDiceButtonNumberMode(rollDiceButton, roll.ToString());
        rollDiceButton.SetEnabled(true);
        yield return new WaitForSeconds(0.5f);

        // Phase 3: begin movement.
        _isDiceRolling = false;
        FinalScoreLogger.Instance?.LogRoll(roll, startSquare, endSquare);
        if (!MovePlayer(roll, triggerLandOn: true, showCountdown: true))
        {
            // Edge case: movement not possible — restore button.
            rollDiceButton.SetEnabled(true);
            SetRollDiceButtonTextMode(rollDiceButton);
        }
    }

    public bool MovePlayer(int steps, bool triggerLandOn = true, bool showCountdown = true)
    {
        if (steps == 0) return false;

        int target = Mathf.Clamp(player.boardSquareIndex + steps, 0, boardSquares.Count - 1);
        if (target == player.boardSquareIndex) return false;

        player.finalBoardSquareIndex = target;
        player.change_direction = (steps > 0) ? 1 : -1;
        player.nextBoardSquareIndex = player.boardSquareIndex + player.change_direction;
        player.trigger_when_done = triggerLandOn;
        player.show_dice_countdown = showCountdown;
        player.moving = true;
        return true;
    }

    public void MovePlayerOneStepBackwards()
    {
        if (player.moving) return;
        MovePlayer(-1, triggerLandOn: false, showCountdown: false);
    }

    private void EndGame()
    {
        Debug.Log("ENDGAME CALLED");

        if (gameEnded) return;

        // Mark current player as finished
        player.hasFinished = true;
        finishCounter++;
        player.finishOrder = finishCounter;
        Debug.Log($"Player {currentPlayerIndex + 1} has finished the game! (Finish order: {finishCounter})");

        // Arrival Bonus
        if (player.finishOrder == 1)
        {
            Debug.Log($"Player {currentPlayerIndex + 1} arrived first! -1 Pollution Bonus");
            if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
            {
                ScoreManager.Instance.AddScoreToPlayer(player, -1, "First to finish bonus");
            }
        }
        
        // Visual indicator - mark player as finished
        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
        {
            var scoreBox = inGameUiDocument.rootVisualElement.Q<VisualElement>($"score-{currentPlayerIndex + 1}");
            if (scoreBox != null)
            {
                var scoreLabel = scoreBox.Q<Label>("score_label");
                if (scoreLabel != null)
                {
                    scoreLabel.text = $"P{currentPlayerIndex + 1} Done!";
                }
            }
            
            // Check if all players have finished
            bool allFinished = true;
            foreach (var p in players)
            {
                if (!p.hasFinished)
                {
                    allFinished = false;
                    break;
                }
            }
            
            if (!allFinished)
            {
                Debug.Log("Not all players finished yet. Continuing game.");
                EndTurn();
                return;
            }
            
            Debug.Log("All players have finished! Showing final scores.");
        }
        
        gameEnded = true;

        // Disable dice
        var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
        if (rollDiceButton != null)
            rollDiceButton.SetEnabled(false);

        // Lower the game UI sorting order so final score can appear on top
        if (inGameUiDocument != null)
        {
            inGameUiDocument.sortingOrder = 0;
            Debug.Log("Set InGameUIDocument sorting order to 0");
        }

        ShowFinalScore();
    }

    private void StartBossBattleForPlayer()
    {
        Debug.Log($"Player {currentPlayerIndex + 1} reached the end!");

        // Store the player who is doing the boss battle
        Player bossPlayer = player;
        int bossPlayerIndex = currentPlayerIndex;
        
        // Disable dice while boss battle is active
        var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
        if (rollDiceButton != null)
            rollDiceButton.SetEnabled(false);

        BossManager bossManager = GetComponent<BossManager>();
        if (bossManager != null && bossManager.StartBossFight(
            () => OnBossWin(bossPlayer, bossPlayerIndex), 
            () => OnBossLose(bossPlayer, bossPlayerIndex)))
        {
            return;
        }

        // No boss battle - mark player as finished
        EndGame();
    }

    private void OnBossWin(Player bossPlayer, int bossPlayerIndex)
    {
        Debug.Log($"Player {bossPlayerIndex + 1} won boss battle!");
        
        // Temporarily set the context to the boss player
        Player originalPlayer = player;
        int originalIndex = currentPlayerIndex;
        
        player = bossPlayer;
        currentPlayerIndex = bossPlayerIndex;
        
        // Score already handled by Boss.OnBossSuccess()
        Debug.Log("OnBossWin: Calling EndGame()");
        EndGame();
        Debug.Log("OnBossWin: EndGame() completed");
    }

    private void OnBossLose(Player bossPlayer, int bossPlayerIndex)
    {
        Debug.Log($"Player {bossPlayerIndex + 1} lost boss battle!");
        
        // Temporarily set the context to the boss player
        player = bossPlayer;
        currentPlayerIndex = bossPlayerIndex;
        
        // Score already handled by Boss.OnBossFailure()
        
        // In singleplayer (or when GameManager is null in tests), losing the boss does NOT end the game
        // Player can continue playing
        if (GameManager.Instance == null || GameManager.Instance.Mode == GameMode.Singleplayer)
        {
            // Re-enable dice button so player can continue
            var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            if (rollDiceButton != null)
            {
                rollDiceButton.SetEnabled(true);
            }
            
            Debug.Log("Boss battle lost in singleplayer - game continues");
        }
        else
        {
            // In multiplayer, losing still marks the player as finished
            EndGame();
        }
    }

    private void ShowFinalScore()
    {
        Debug.Log($"ShowFinalScore called. GameMode: {(GameManager.Instance != null ? GameManager.Instance.Mode.ToString() : "GameManager is null")}");
        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
        {
            Debug.Log("Showing multiplayer final scores");
            ShowMultiplayerFinalScores();
        }
        else
        {
            Debug.Log("Showing singleplayer final scores");
            ShowSingleplayerFinalScore();
        }
    }

    private void ShowSingleplayerFinalScore()
    {
        Debug.Log("ShowSingleplayerFinalScore called");
        int finalAqhi = ScoreManager.Instance != null ? ScoreManager.Instance.GetScore() : 0;

        // Find singleplayer final score popup
        GameObject finalScoreObj = GameObject.Find("FinalScoreUIDocument");
        if (finalScoreObj != null)
        {
            Debug.Log("Found FinalScoreUIDocument GameObject");
            if (!finalScoreObj.activeSelf)
            {
                Debug.Log("FinalScoreUIDocument was inactive, enabling it");
                finalScoreObj.SetActive(true);
            }
        }
        
        FinalScorePopup singleScorePopup = finalScoreObj?.GetComponent<FinalScorePopup>();
        
        if (singleScorePopup != null)
        {
            Debug.Log("Found FinalScorePopup via GameObject.Find");
            singleScorePopup.ShowFinalScores(finalAqhi, triviaCorrectCount, triviaIncorrectCount);
        }
        else if (finalScorePopup != null)
        {
            // Fallback to inspector-assigned popup
            Debug.Log("Using finalScorePopup from inspector");
            finalScorePopup.ShowFinalScores(finalAqhi, triviaCorrectCount, triviaIncorrectCount);
        }
        else
        {
            Debug.LogWarning("FinalScoreUIDocument not found for singleplayer.");
        }
    }

    private void ShowMultiplayerFinalScores()
    {
        if (players.Count == 0) return;

        // Find multiplayer final score popup
        MultiplayerFinalScorePopup multiScorePopup = GameObject.Find("FinalScoreMPUIDocument")?.GetComponent<MultiplayerFinalScorePopup>();
        
        if (multiScorePopup != null)
        {
            multiScorePopup.ShowFinalScores(players);
        }
        else
        {
            Debug.LogWarning("FinalScoreMPUIDocument not found in scene");
        }
    }
}
