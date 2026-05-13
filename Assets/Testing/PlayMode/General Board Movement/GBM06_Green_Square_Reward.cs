using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    [Category("GeneralBoardMovement")]
    public class GBM06_Green_Square_Reward {
        private GameController gameController;
        private ScoreManager scoreManager;
        private MinigameManager minigameManager;
        private Button rollDiceButton;

        [UnitySetUp]
        public IEnumerator UnitySetUp() 
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            
            // Set singleplayer mode
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist");
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;
            
            for (int i = 0; i < 5; i++) yield return null;

            gameController = GameObject.Find("GameController").GetComponent<GameController>();
            scoreManager = ScoreManager.Instance;
            minigameManager = gameController.GetComponent<MinigameManager>();
            rollDiceButton = gameController.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            
            Assert.IsNotNull(gameController, "GameController should exist in the scene");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist in the scene");
            Assert.IsNotNull(scoreManager.inGameUiDocument, "ScoreManager UI Document should exist");
            Assert.IsNotNull(minigameManager, "MinigameManager should exist");
            Assert.IsNotNull(rollDiceButton, "Dice button should exist");
            
            var scoreBox = scoreManager.inGameUiDocument.rootVisualElement.Q<VisualElement>("score-1");
            Assert.IsNotNull(scoreBox, "score-1 container should exist");
            
            var scoreLabel = scoreBox.Q<Label>("score_label");
            var progressBar = scoreBox.Q<VisualElement>("progress_bar");
            
            Assert.IsNotNull(scoreLabel, "Score label should exist in UI");
            Assert.IsNotNull(progressBar, "Progress bar should exist in UI");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator GreenSquareLaunchesMinigame() {
            if (NoMinigamesConfigured()) yield break;
            
            gameController.LandedOnGreenSquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            Assert.IsNotNull(activeMinigame, "Should find an active minigame");
        }

        [UnityTest]
        public IEnumerator MinigameCompletionDecreasesScore() {
            if (NoMinigamesConfigured()) yield break;
            
            scoreManager.SetScore(5);
            int oldScore = scoreManager.GetScore();
            
            gameController.LandedOnGreenSquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            if (activeMinigame == null) {
                Assert.Inconclusive("No active minigame found");
                yield break;
            }
            
            yield return CompleteMinigame(activeMinigame);
            
            int newScore = scoreManager.GetScore();
            Assert.Less(newScore, oldScore, "Score should decrease after completing minigame");
        }

        [UnityTest]
        public IEnumerator MinigameExitDoesNotChangeScore() {
            if (NoMinigamesConfigured()) yield break;
            
            scoreManager.SetScore(5);
            int oldScore = scoreManager.GetScore();
            
            gameController.LandedOnGreenSquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            if (activeMinigame == null) 
            {
                Assert.Inconclusive("No active minigame found");
                yield break;
            }
            
            yield return ExitMinigame(activeMinigame);
            
            int newScore = scoreManager.GetScore();
            Assert.AreEqual(oldScore, newScore, "Score should not change when exiting minigame");
        }

        [UnityTest]
        public IEnumerator DiceButtonDisabledDuringMinigame() {
            if (NoMinigamesConfigured()) yield break;
            
            gameController.LandedOnGreenSquare();
            yield return null;
            
            Assert.IsFalse(rollDiceButton.enabledSelf, "Dice button should be disabled during minigame");
        }

        [UnityTest]
        public IEnumerator DiceButtonEnabledAfterMinigameCompletion() {
            if (NoMinigamesConfigured()) yield break;

            gameController.ResumeGameAfterMinigame();
            
            yield return null;
            Assert.IsTrue(rollDiceButton.enabledSelf, "Dice button should be re-enabled after minigame");
        }

        [UnityTest]
        public IEnumerator ScoreDoesNotGoBelowZero() {
            if (NoMinigamesConfigured()) yield break;
            
            scoreManager.SetScore(1);
            gameController.LandedOnGreenSquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            if (activeMinigame != null){
                yield return CompleteMinigame(activeMinigame);
            }
            
            yield return null;
            int finalScore = scoreManager.GetScore();
            Assert.GreaterOrEqual(finalScore, 0, "Score should not go below 0");
        }

        [UnityTest]
        public IEnumerator UpdatedScoreIsReflectedInUI() 
        {
            if (NoMinigamesConfigured()) yield break;

            for (int i = 0; i < 5; i++) yield return null;
            
            // Query inside score-1 container
            var scoreBox = scoreManager.inGameUiDocument.rootVisualElement.Q<VisualElement>("score-1");
            Assert.IsNotNull(scoreBox, "score-1 container should exist");
            
            var scoreLabel = scoreBox.Q<Label>("score_label");
            Assert.IsNotNull(scoreLabel, "score_label should exist inside score-1");
            
            // Set score AFTER scene initialization completes
            yield return null;
            
            scoreManager.SetScore(5);
            yield return null; // Wait for UI to update
            
            Debug.Log($"Score after SetScore(5): {scoreManager.GetScore()}, Label text: {scoreLabel.text}");
            
            gameController.LandedOnGreenSquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            if (activeMinigame != null)
            {
                yield return CompleteMinigame(activeMinigame);
            }
            yield return null;
            
            int newScore = scoreManager.GetScore();
            Debug.Log($"Final score: {newScore}, Label text: {scoreLabel.text}");
            
            string expectedText = newScore >= 10 ? "AQHI: Too High!" : $"AQHI: {newScore}";
            Assert.AreEqual(expectedText, scoreLabel.text, "UI should reflect updated score");
        }

        [UnityTest]
        public IEnumerator GraySquareDoesNotLaunchMinigame() {
            gameController.LandedOnGraySquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            Assert.IsNull(activeMinigame, "Gray square should not launch a minigame");
        }

        [UnityTest]
        public IEnumerator BlueSquareDoesNotLaunchMinigame() {
            gameController.LandedOnBlueSquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            Assert.IsNull(activeMinigame, "Blue square should not launch a minigame");
        }

        [UnityTest]
        public IEnumerator GreenSquareDoesNotIncreasePollution() {
            if (NoMinigamesConfigured()) yield break;

            scoreManager.SetScore(5);
            int oldScore = scoreManager.GetScore();
            gameController.LandedOnGreenSquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            if (activeMinigame == null) {
                Assert.Inconclusive("No active minigame found");
                yield break;
            }
            yield return CompleteMinigame(activeMinigame);
            
            int newScore = scoreManager.GetScore();
            Assert.LessOrEqual(newScore, oldScore, "Green square should not increase pollution");
        }

        [UnityTest]
            public IEnumerator MultipleButtonPressesDoNotOverlapJarringly() {
            if (NoMinigamesConfigured()) yield break;
            gameController.LandedOnGreenSquare();
            yield return null;
            
            IMinigame activeMinigame = FindActiveMinigame();
            if (activeMinigame == null) {
                Assert.Inconclusive("No active minigame found");
                yield break;
            }
            UIDocument uiDoc = null;
            if (activeMinigame is PlantTreesMinigame plantTrees) uiDoc = plantTrees.uiDocument;
            else if (activeMinigame is StayInsideCleanAirMinigame stayInside) uiDoc = stayInside.uiDocument;
            else if (activeMinigame is PlaceholderMinigame placeholder) uiDoc = placeholder.uiDocument;
            
            if (uiDoc == null) {
                Assert.Inconclusive("Could not find minigame UIDocument");
                yield break;
            }
            
            var exitButton = uiDoc.rootVisualElement.Q<Button>("exit_button");
            Assert.IsNotNull(exitButton, "Exit button should exist");
            
            for (int i = 0; i < 5; i++) {
                using (ClickEvent click = ClickEvent.GetPooled()) {
                    click.target = exitButton;
                    exitButton.SendEvent(click);
                }
                yield return null;
            }
            
            yield return null;
            Assert.Pass("Multiple button presses completed without errors");
        }

        // Helper methods
        private bool NoMinigamesConfigured() {
            if (minigameManager.minigameObjects == null || minigameManager.minigameObjects.Count == 0) {
                Assert.Inconclusive("No minigames configured in scene for testing");
                return true;
            }
            return false;
        }

        private IMinigame FindActiveMinigame() {
            var allUIDocuments = GameObject.FindObjectsOfType<UIDocument>();
            
            foreach (var uiDoc in allUIDocuments) {
                if (uiDoc.rootVisualElement != null && uiDoc.rootVisualElement.style.display.value == DisplayStyle.Flex) {
                    var minigame = uiDoc.GetComponent<IMinigame>();
                    if (minigame != null) return minigame;
                }
            }
            
            return null;
        }

        private IEnumerator CompleteMinigame(IMinigame minigame)
        {
            if (minigame is PlantTreesMinigame plantTrees) {
                var tapArea = plantTrees.uiDocument.rootVisualElement.Q<VisualElement>("tap_area");
                if (tapArea != null) {
                    for (int i = 0; i < 15; i++) {
                        using (ClickEvent click = ClickEvent.GetPooled()) {
                            click.target = tapArea;
                            tapArea.SendEvent(click);
                        }
                    }
                }
                yield return null;
            }
            else if (minigame is StayInsideCleanAirMinigame stayInside) {
                var playArea = stayInside.uiDocument.rootVisualElement.Q<VisualElement>("room_frame");
                if (playArea != null) {
                    float savedBurst = stayInside.tapWindBurst;
                    stayInside.tapWindBurst = 0.12f;
                    for (int i = 0; i < 24; i++) {
                        using (ClickEvent click = ClickEvent.GetPooled()) {
                            click.target = playArea;
                            playArea.SendEvent(click);
                        }
                    }
                    stayInside.tapWindBurst = savedBurst;
                }
                /* Stay Inside runs until timeLimit (12s); no early win — wait for timer + result delay. */
                yield return new WaitForSeconds(14f);
            }
            else if (minigame is PlaceholderMinigame placeholder) {
                var completeButton = placeholder.uiDocument.rootVisualElement.Q<Button>("complete_button");
                if (completeButton != null) {
                    using (ClickEvent click = ClickEvent.GetPooled()) {
                        click.target = completeButton;
                        completeButton.SendEvent(click);
                    }
                }
                yield return null;
            }
        }

        private IEnumerator ExitMinigame(IMinigame minigame) {
            UIDocument uiDoc = null;
            if (minigame is PlantTreesMinigame plantTrees) uiDoc = plantTrees.uiDocument;
            else if (minigame is StayInsideCleanAirMinigame stayInside) uiDoc = stayInside.uiDocument;
            else if (minigame is PlaceholderMinigame placeholder) uiDoc = placeholder.uiDocument;
            
            if (uiDoc != null) {
                var exitButton = uiDoc.rootVisualElement.Q<Button>("exit_button");
                if (exitButton != null) {
                    using (ClickEvent click = ClickEvent.GetPooled()) {
                        click.target = exitButton;
                        exitButton.SendEvent(click);
                    }
                }
            }
            
            yield return null;
        }
    }
}