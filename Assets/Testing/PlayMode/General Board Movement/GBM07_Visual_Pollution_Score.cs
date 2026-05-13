using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    [Category("General Board Movement")]
    public class GBM07_Visual_Pollution_Score {
        private GameController gameController;
        private ScoreManager scoreManager;
        private Label scoreLabel;
        private VisualElement progressBar;

        [UnitySetUp]
        public IEnumerator UnitySetUp() {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist");
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;

            gameController = GameObject.Find("GameController").GetComponent<GameController>();
            scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(gameController, "GameController should exist in the scene");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist in the scene");
            Assert.IsNotNull(scoreManager.inGameUiDocument, "ScoreManager UI Document should exist");
            
            var scoreBox = scoreManager.inGameUiDocument.rootVisualElement.Q<VisualElement>("score-1");
            Assert.IsNotNull(scoreBox, "score-1 container should exist");
            
            scoreLabel = scoreBox.Q<Label>("score_label");
            progressBar = scoreBox.Q<VisualElement>("progress_bar");
            
            Assert.IsNotNull(scoreLabel, "Score label should exist in UI");
            Assert.IsNotNull(progressBar, "Progress bar should exist in UI");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator PollutionScoreIsAlwaysVisible() {
            Assert.IsNotNull(scoreLabel, "Score label should exist");
            Assert.AreNotEqual(DisplayStyle.None, scoreLabel.style.display.value, "Score should be visible");
            yield return null;
            Assert.AreNotEqual(DisplayStyle.None, scoreLabel.style.display.value, "Score should remain visible");
        }

        [UnityTest]
        public IEnumerator ScoreIsShownUsingAQHIBar() {
            Assert.IsNotNull(progressBar, "AQHI progress bar should exist");
            var barWidth = progressBar.style.width;
            Assert.IsTrue(barWidth.value.value >= 0, "Progress bar should have a valid width");
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisualUpdatesWhenScoreIncreases() {
            scoreManager.SetScore(3);
            yield return null;
            
            var initialWidth = progressBar.style.width.value.value;
            var initialColor = progressBar.style.backgroundColor.value;
            scoreManager.AddScore(2);
            yield return null;
            
            var newWidth = progressBar.style.width.value.value;
            Assert.Greater(newWidth, initialWidth, "Progress bar should increase when score increases");
            var newScore = scoreManager.GetScore();
            Assert.AreEqual(5, newScore);
        }

        [UnityTest]
        public IEnumerator VisualUpdatesWhenScoreDecreases() {
            scoreManager.SetScore(7);
            yield return null;
            
            var initialWidth = progressBar.style.width.value.value;
            scoreManager.AddScore(-2);
            yield return null;
            
            var newWidth = progressBar.style.width.value.value;
            Assert.Less(newWidth, initialWidth, "Progress bar should decrease when score decreases");
            var newScore = scoreManager.GetScore();
            Assert.AreEqual(5, newScore);
        }

        [UnityTest]
        public IEnumerator BarColorChangesWithPollutionLevel() {
            scoreManager.SetScore(2);
            yield return null;
            var lowColor = progressBar.style.backgroundColor.value;
            
            scoreManager.SetScore(5);
            yield return null;
            var mediumColor = progressBar.style.backgroundColor.value;
            
            scoreManager.SetScore(8);
            yield return null;
            var highColor = progressBar.style.backgroundColor.value;
            
            Assert.AreNotEqual(lowColor, highColor, "Bar color should change between low and high pollution");
        }

        [UnityTest]
        public IEnumerator ScoreNeverExceedsValidRange() {
            scoreManager.SetScore(0);
            yield return null;
            Assert.AreEqual(0, scoreManager.GetScore());
            
            scoreManager.AddScore(-5);
            yield return null;
            Assert.GreaterOrEqual(scoreManager.GetScore(), 0, "Score should not go below 0");
            
            scoreManager.SetScore(10);
            yield return null;
            Assert.AreEqual(10, scoreManager.GetScore());
        }

        [UnityTest]
        public IEnumerator ProgressBarWidthMatchesScore() {
            for (int score = 0; score <= 10; score++) {
                scoreManager.SetScore(score);
                yield return null;
                
                float expectedPercentage = (score / 10f) * 100f;
                float actualPercentage = progressBar.style.width.value.value;
                Assert.AreEqual(expectedPercentage, actualPercentage, 1f, $"Progress bar should be {expectedPercentage}% at score {score}");
            }
        }

        [UnityTest]
        public IEnumerator ScoreLabelIsReadableAndUnderstandable() {
            scoreManager.SetScore(5);
            yield return null;
            
            string labelText = scoreLabel.text;
            Assert.IsTrue(labelText.Contains("AQHI"), "Label should mention AQHI");
            Assert.IsTrue(labelText.Contains("5"), "Label should show the score value");
            scoreManager.SetScore(10);
            yield return null;
            
            labelText = scoreLabel.text;
            Assert.IsTrue(labelText.Contains("Too High") || labelText.Contains("10"), "Label should show warning at max score");
        }

        [UnityTest]
        public IEnumerator VisualFeedbackOccursWithinShortTime() {
            scoreManager.SetScore(3);
            yield return null;
            
            var initialWidth = progressBar.style.width.value.value;
            scoreManager.AddScore(3);

            bool updated = false;
            for (int i = 0; i < 60; i++) {
                if (progressBar.style.width.value.value != initialWidth) {
                    updated = true;
                    break;
                }
                yield return null;
            }

            Assert.IsTrue(updated, "Visual update should occur within 60 frames");
            var newWidth = progressBar.style.width.value.value;
            Assert.AreNotEqual(initialWidth, newWidth, "Progress bar should have updated");
        }

        [UnityTest]
        public IEnumerator ScoreDisplayIsConsistentAcrossUpdates() {
            for (int i = 0; i < 5; i++) {
                int randomScore = Random.Range(0, 11);
                scoreManager.SetScore(randomScore);
                yield return null;
                
                string labelText = scoreLabel.text;
                float barWidth = progressBar.style.width.value.value;
                int displayedScore = scoreManager.GetScore();
                Assert.AreEqual(randomScore, displayedScore, "Internal score should match set score");
                
                float expectedWidth = (randomScore / 10f) * 100f;
                Assert.AreEqual(expectedWidth, barWidth, 1f, "Bar width should match score");
            }
        }
    }
}
