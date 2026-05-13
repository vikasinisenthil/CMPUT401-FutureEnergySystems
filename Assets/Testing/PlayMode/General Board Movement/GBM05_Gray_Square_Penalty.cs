using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    [Category("General Board Movement")]
    public class GBM05_Gray_Square_Penalty {
        private GameController gameController;
        private ScoreManager scoreManager;
        private GameObject player;

        [UnitySetUp]
        public IEnumerator UnitySetUp() {
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;
            gameController = GameObject.Find("GameController").GetComponent<GameController>();
            scoreManager = ScoreManager.Instance;

            yield return null;

            Assert.IsNotNull(gameController, "GameController should exist in the scene");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist in the scene");
        }

        [UnityTest]
        public IEnumerator GraySquareIncreasesScoreByOne() {
            Assert.IsNotNull(gameController, "GameController should exist in the scene");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist in the scene");
            int oldScore = scoreManager.GetScore();
            gameController.LandedOnGraySquare();
            yield return null;

            // Click the close button to apply the penalty
            var grayCardRoot = gameController.grayCardUiDocument.rootVisualElement.Q<VisualElement>("card_box");
            var closeButton = grayCardRoot.Q<Button>("close_button");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = closeButton;
                closeButton.SendEvent(click);
            }
            yield return null;

            int newScore = scoreManager.GetScore();
            Assert.AreEqual(oldScore + 1, newScore);
        }

        [UnityTest]
        public IEnumerator GreySquareAppliesOncePerLanding() {
            int oldScore = scoreManager.GetScore();
            gameController.LandedOnGraySquare();
            yield return null;

            // Click the close button to apply the penalty
            var grayCardRoot = gameController.grayCardUiDocument.rootVisualElement.Q<VisualElement>("card_box");
            var closeButton = grayCardRoot.Q<Button>("close_button");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = closeButton;
                closeButton.SendEvent(click);
            }
            yield return null;

            int newScore = scoreManager.GetScore();
            Assert.AreEqual(oldScore + 1, newScore, "Score should increase by 1 when close button is clicked");

            // Close the card again (should not increase score again)
            grayCardRoot.style.display = DisplayStyle.None;
            yield return null;

            int finalScore = scoreManager.GetScore();
            Assert.AreEqual(newScore, finalScore, "Score should not change when manually hiding the card");
        }

        [UnityTest]
        public IEnumerator UpdatedScoreIsReflectedInUI() {
            var scoreLabel = scoreManager.inGameUiDocument.rootVisualElement.Q<Label>("score_label");
            int oldScore = scoreManager.GetScore();
            gameController.LandedOnGraySquare();
            yield return null;
            
            int newScore = scoreManager.GetScore();
            string expectedText = newScore >= 10 ? "AQHI: Too High!" : $"AQHI: {newScore}";
            Assert.AreEqual(expectedText, scoreLabel.text);
        }

        [UnityTest]
        public IEnumerator AppropriateFeedbackIsShown() {
            gameController.LandedOnGraySquare();
            yield return null;
            var grayCardRoot = gameController.grayCardUiDocument.rootVisualElement.Q<VisualElement>("card_box");
            Assert.AreEqual(DisplayStyle.Flex, grayCardRoot.style.display.value);
            
            var cardTitle = grayCardRoot.Q<Label>("card_title");
            var cardText = grayCardRoot.Q<Label>("card_text");
            Assert.IsNotNull(cardTitle, "Card should have a title");
            Assert.IsNotNull(cardText, "Card should have descriptive text");
        }

        [UnityTest]
        public IEnumerator BlueSquareDoesNotIncreasePollutionByDefault() {
            int oldScore = scoreManager.GetScore();
            gameController.LandedOnBlueSquare();
            yield return null;
            
            int newScore = scoreManager.GetScore();
            Assert.AreEqual(oldScore, newScore);
        }
    }
}