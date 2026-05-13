using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>
/// ABE.01 - Minigame on Green Square
/// Tests that landing on a green square triggers a minigame, and that
/// the pollution score is reduced based on minigame performance.
/// </summary>

namespace AdditionalBoardEvents {
    [Category("Additional Board Events")]
    public class ABE01_Minigame_On_Green_Square
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            
            Assert.IsNotNull(GameManager.Instance, "GameManager should exist");
            Assert.IsNotNull(AudioManager.Instance, "AudioManager should exist");
            
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;

            PlaceholderMinigame placeholder = Object.FindFirstObjectByType<PlaceholderMinigame>(FindObjectsInactive.Include);
            if (placeholder == null)
                placeholder = Object.FindObjectOfType<PlaceholderMinigame>(true);
            GameObject placeholderObj = placeholder != null ? placeholder.gameObject : null;
            MinigameManager mm = GameObject.Find("GameController")?.GetComponent<MinigameManager>();
            if (mm != null && placeholderObj != null)
            {
                mm.minigameObjects.Clear();
                mm.minigameObjects.Add(placeholderObj);
            }

            yield return null;
        }

        /// <summary>
        /// AC1: When a player lands on a green square, a mini game is triggered.
        /// Verifies the minigame UI becomes visible when LandedOnGreenSquare is called.
        /// </summary>
        [UnityTest]
        public IEnumerator LandingOnGreenSquareTriggersMinigame()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            MinigameManager mm = GameObject.Find("GameController").GetComponent<MinigameManager>();

            Assert.NotNull(mm, "MinigameManager should be attached to GameController");
            Assert.Greater(mm.minigameObjects.Count, 0, "At least one minigame should be configured");

            gc.LandedOnGreenSquare();

            // The minigame should now be active
            Assert.True(mm.IsMinigameActive, "Minigame should be active after landing on green square");

            yield return null;
        }

        /// <summary>
        /// AC2: Landing on any non-green square does not trigger the mini game.
        /// Verifies that blue and gray squares do not launch a minigame.
        /// </summary>
        [UnityTest]
        public IEnumerator LandingOnBlueSquareDoesNotTriggerMinigame()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            MinigameManager mm = GameObject.Find("GameController").GetComponent<MinigameManager>();

            gc.LandedOnBlueSquare();

            Assert.False(mm.IsMinigameActive, "Minigame should not be active after landing on blue square");

            yield return null;
        }

        /// <summary>
        /// AC2: Landing on any non-green square does not trigger the mini game.
        /// </summary>
        [UnityTest]
        public IEnumerator LandingOnGraySquareDoesNotTriggerMinigame()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            MinigameManager mm = GameObject.Find("GameController").GetComponent<MinigameManager>();

            gc.LandedOnGraySquare();

            Assert.False(mm.IsMinigameActive, "Minigame should not be active after landing on gray square");

            yield return null;
        }

        /// <summary>
        /// AC3: The player cannot continue their turn until the mini game is completed or exited.
        /// Verifies the dice button is disabled while a minigame is active.
        /// </summary>
        [UnityTest]
        public IEnumerator DiceDisabledDuringMinigame()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();

            Button diceButton = gc.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.True(diceButton.enabledSelf, "Dice button should be enabled before minigame");

            gc.LandedOnGreenSquare();

            Assert.False(diceButton.enabledSelf, "Dice button should be disabled during minigame");

            yield return null;
        }

        /// <summary>
        /// AC5: The pollution score is reduced by the exact amount returned.
        /// AC7: The updated pollution score is displayed after the mini game ends.
        /// Verifies that completing the minigame reduces pollution by the correct amount.
        /// </summary>
        [UnityTest]
        public IEnumerator CompletingMinigameReducesPollutionByCorrectAmount()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            PlaceholderMinigame pm = Object.FindFirstObjectByType<PlaceholderMinigame>() ?? Object.FindObjectOfType<PlaceholderMinigame>();
            Assert.NotNull(pm, "BoardScene must contain a PlaceholderMinigame for this test");

            // Set a known pollution score first
            ScoreManager.Instance.AddScore(5);
            int scoreBefore = ScoreManager.Instance.GetScore();

            int expectedReduction = pm.pollutionReductionAmount;

            gc.LandedOnGreenSquare();

            // Simulate clicking the "Complete" button
            VisualElement root = pm.uiDocument.rootVisualElement;
            Button completeButton = root.Q<Button>("complete_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = completeButton;
                completeButton.SendEvent(click);
            }

            Assert.AreEqual(scoreBefore - expectedReduction, ScoreManager.Instance.GetScore(),
                "Pollution should be reduced by the exact minigame result amount");

            yield return null;
        }

        /// <summary>
        /// AC6: Pollution score cannot go below zero after reduction.
        /// Verifies the score is clamped to 0 when reduction would go negative.
        /// </summary>
        [UnityTest]
        public IEnumerator PollutionScoreCannotGoBelowZero()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();

            // Score starts at 0 by default
            Assert.AreEqual(0, ScoreManager.Instance.GetScore(), "Score should start at 0");

            gc.LandedOnGreenSquare();

            // Simulate clicking the "Complete" button
            PlaceholderMinigame pm = Object.FindFirstObjectByType<PlaceholderMinigame>() ?? Object.FindObjectOfType<PlaceholderMinigame>();
            Assert.NotNull(pm, "BoardScene must contain a PlaceholderMinigame for this test");
            VisualElement root = pm.uiDocument.rootVisualElement;
            Button completeButton = root.Q<Button>("complete_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = completeButton;
                completeButton.SendEvent(click);
            }

            Assert.GreaterOrEqual(ScoreManager.Instance.GetScore(), 0,
                "Pollution score should never go below 0");

            yield return null;
        }

        /// <summary>
        /// AC8: After the mini game finishes, normal gameplay resumes.
        /// Verifies dice button is re-enabled after completing a minigame.
        /// Uses a non-Cyclist hero so that resume happens immediately (Cyclist gets +1 move first, dice re-enabled after that).
        /// </summary>
        [UnityTest]
        public IEnumerator GameResumesAfterMinigameComplete()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();

            // Ensure non-Cyclist so dice is re-enabled immediately after minigame complete (ABE.11 Cyclist delays resume for +1 move).
            if (GameManager.Instance != null && GameManager.Instance.SelectedHeroes != null && GameManager.Instance.SelectedHeroes.Length > 0)
                GameManager.Instance.SelectedHeroes[0] = HeroType.Scientist;
            gc.player.heroType = HeroType.Scientist;

            gc.LandedOnGreenSquare();

            Button diceButton = gc.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.False(diceButton.enabledSelf, "Dice should be disabled during minigame");

            // Simulate clicking the "Complete" button
            PlaceholderMinigame pm = Object.FindFirstObjectByType<PlaceholderMinigame>() ?? Object.FindObjectOfType<PlaceholderMinigame>();
            Assert.NotNull(pm, "BoardScene must contain a PlaceholderMinigame for this test");
            VisualElement root = pm.uiDocument.rootVisualElement;
            Button completeButton = root.Q<Button>("complete_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = completeButton;
                completeButton.SendEvent(click);
            }

            Assert.True(diceButton.enabledSelf, "Dice should be re-enabled after minigame completes");

            yield return null;
        }

        /// <summary>
        /// AC9: If the mini game is exited by user, the score is not changed and game resumes.
        /// Verifies that exiting the minigame leaves the score unchanged.
        /// </summary>
        [UnityTest]
        public IEnumerator ExitingMinigameDoesNotChangeScore()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();

            ScoreManager.Instance.AddScore(5);
            int scoreBefore = ScoreManager.Instance.GetScore();

            gc.LandedOnGreenSquare();

            // Simulate clicking the "Exit" button
            PlaceholderMinigame pm = Object.FindFirstObjectByType<PlaceholderMinigame>() ?? Object.FindObjectOfType<PlaceholderMinigame>();
            Assert.NotNull(pm, "BoardScene must contain a PlaceholderMinigame for this test");
            VisualElement root = pm.uiDocument.rootVisualElement;
            Button exitButton = root.Q<Button>("exit_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = exitButton;
                exitButton.SendEvent(click);
            }

            Assert.AreEqual(scoreBefore, ScoreManager.Instance.GetScore(),
                "Pollution score should not change when exiting minigame");

            yield return null;
        }

        /// <summary>
        /// AC9: If the mini game is exited by user, the game resumes.
        /// Verifies dice button is re-enabled after exiting a minigame.
        /// </summary>
        [UnityTest]
        public IEnumerator GameResumesAfterMinigameExit()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();

            gc.LandedOnGreenSquare();

            Button diceButton = gc.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.False(diceButton.enabledSelf, "Dice should be disabled during minigame");

            // Simulate clicking the "Exit" button
            PlaceholderMinigame pm = Object.FindFirstObjectByType<PlaceholderMinigame>() ?? Object.FindObjectOfType<PlaceholderMinigame>();
            Assert.NotNull(pm, "BoardScene must contain a PlaceholderMinigame for this test");
            VisualElement root = pm.uiDocument.rootVisualElement;
            Button exitButton = root.Q<Button>("exit_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = exitButton;
                exitButton.SendEvent(click);
            }

            Assert.True(diceButton.enabledSelf, "Dice should be re-enabled after exiting minigame");

            yield return null;
        }

        /// <summary>
        /// AC7: The updated pollution score is displayed after the mini game ends.
        /// Verifies the score label updates correctly after minigame completion.
        /// </summary>
        [UnityTest]
        public IEnumerator ScoreUIUpdatesAfterMinigameComplete()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();

            ScoreManager.Instance.AddScore(5);

            gc.LandedOnGreenSquare();

            // Simulate clicking the "Complete" button
            PlaceholderMinigame pm = Object.FindFirstObjectByType<PlaceholderMinigame>() ?? Object.FindObjectOfType<PlaceholderMinigame>();
            Assert.NotNull(pm, "BoardScene must contain a PlaceholderMinigame for this test");
            VisualElement root = pm.uiDocument.rootVisualElement;
            Button completeButton = root.Q<Button>("complete_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = completeButton;
                completeButton.SendEvent(click);
            }

            int expectedScore = 5 - pm.pollutionReductionAmount;
            Label scoreLabel = ScoreManager.Instance.inGameUiDocument.rootVisualElement.Q<Label>("score_label");

            Assert.True(scoreLabel.text.Contains(expectedScore.ToString()),
                $"Score label should display updated score of {expectedScore} after minigame");

            yield return null;
        }
    }
}