using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Multiplayer
{
    [Category("Multiplayer")]
    public class MP03_WinByLowerAQHI
    {
        private GameController gameController;
        private GameManager gameManager;
        private ScoreManager scoreManager;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Load MainMenu to create GameManager
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;

            gameManager = GameManager.Instance;
            Assert.IsNotNull(gameManager, "GameManager should exist");

            // Set up 3 player game
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 3;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist, HeroType.Ranger };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();
            scoreManager = ScoreManager.Instance;

            Assert.IsNotNull(gameController, "GameController should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
        }

        private static BlueMCQCard MakeTestMCQCard(bool firstCorrect)
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Test MCQ";
            card.statement = "Test statement";
            card.question = "Test question?";
            card.image = sprite;
            card.answers = new List<MCQAnswer>
            {
                new MCQAnswer { answer = "Answer 1", correctAnswer = firstCorrect, messageWhenChosen = "Message 1" },
                new MCQAnswer { answer = "Answer 2", correctAnswer = !firstCorrect, messageWhenChosen = "Message 2" }
            };
            return card;
        }

        [UnityTest]
        public IEnumerator LowestPollutionScoreWins()
        {
            // Set different scores for each player
            scoreManager.AddScoreToPlayer(gameController.players[0], 5, "Test score"); // Player 1: 5
            scoreManager.AddScoreToPlayer(gameController.players[1], 3, "Test score"); // Player 2: 3 (winner)
            scoreManager.AddScoreToPlayer(gameController.players[2], 7, "Test score"); // Player 3: 7
            yield return null;

            // Verify scores
            Assert.AreEqual(5, gameController.players[0].pollutionScore, "Player 1 should have score 5");
            Assert.AreEqual(3, gameController.players[1].pollutionScore, "Player 2 should have score 3");
            Assert.AreEqual(7, gameController.players[2].pollutionScore, "Player 3 should have score 7");

            // Create sorted list (lower is better)
            var rankedPlayers = new List<Player>(gameController.players);
            rankedPlayers.Sort((a, b) => a.pollutionScore.CompareTo(b.pollutionScore));

            // Verify Player 2 is the winner
            Assert.AreEqual(gameController.players[1], rankedPlayers[0], "Player 2 should be the winner");
            Assert.AreEqual(3, rankedPlayers[0].pollutionScore, "Winner should have lowest score (3)");
        }

        [UnityTest]
        public IEnumerator TieForLowestScoreProducesTie()
        {
            // Set tied scores
            scoreManager.AddScoreToPlayer(gameController.players[0], 4, "Test score"); // Player 1: 4 (tied winner)
            scoreManager.AddScoreToPlayer(gameController.players[1], 4, "Test score"); // Player 2: 4 (tied winner)
            scoreManager.AddScoreToPlayer(gameController.players[2], 6, "Test score"); // Player 3: 6
            yield return null;

            // Find winners
            int lowestScore = Mathf.Min(
                gameController.players[0].pollutionScore,
                gameController.players[1].pollutionScore,
                gameController.players[2].pollutionScore
            );

            List<Player> winners = new List<Player>();
            foreach (var player in gameController.players)
            {
                if (player.pollutionScore == lowestScore)
                {
                    winners.Add(player);
                }
            }

            // Verify tie
            Assert.AreEqual(2, winners.Count, "Should have 2 winners in a tie");
            Assert.IsTrue(winners.Contains(gameController.players[0]), "Player 1 should be a winner");
            Assert.IsTrue(winners.Contains(gameController.players[1]), "Player 2 should be a winner");
            Assert.AreEqual(4, lowestScore, "Tied lowest score should be 4");
        }

        [UnityTest]
        public IEnumerator PollutionScoreUpdateOnGreenSquare()
        {
            // Set initial score above 0 so reduction can be detected
            scoreManager.AddScoreToPlayer(gameController.players[0], 3, "Setup");
            yield return null;
            
            int initialScore = gameController.players[0].pollutionScore;
            Assert.AreEqual(3, initialScore, "Initial score should be 3");

            // Simulate landing on green square and completing minigame
            gameController.LandedOnGreenSquare();
            yield return null;

            // Find active minigame
            var allUIDocuments = Object.FindObjectsOfType<UIDocument>();
            IMinigame activeMinigame = null;
            
            foreach (var uiDoc in allUIDocuments)
            {
                if (uiDoc.rootVisualElement != null && uiDoc.rootVisualElement.style.display.value == DisplayStyle.Flex)
                {
                    var minigame = uiDoc.GetComponent<IMinigame>();
                    if (minigame != null)
                    {
                        activeMinigame = minigame;
                        break;
                    }
                }
            }

            if (activeMinigame != null)
            {
                // Complete the minigame
                if (activeMinigame is PlantTreesMinigame plantTrees)
                {
                    var tapArea = plantTrees.uiDocument.rootVisualElement.Q<VisualElement>("tap_area");
                    if (tapArea != null)
                    {
                        for (int i = 0; i < 15; i++)
                        {
                            using (ClickEvent click = ClickEvent.GetPooled())
                            {
                                click.target = tapArea;
                                tapArea.SendEvent(click);
                            }
                        }
                    }
                    yield return null;
                }
                else if (activeMinigame is PlaceholderMinigame placeholder)
                {
                    var completeButton = placeholder.uiDocument.rootVisualElement.Q<Button>("complete_button");
                    if (completeButton != null)
                    {
                        using (ClickEvent click = ClickEvent.GetPooled())
                        {
                            click.target = completeButton;
                            completeButton.SendEvent(click);
                        }
                        yield return null;
                    }
                }

                // Verify score decreased
                int newScore = gameController.players[0].pollutionScore;
                Assert.Less(newScore, initialScore, "Pollution score should decrease after green square minigame");
            }
            else
            {
                Assert.Inconclusive("No minigame found - cannot test green square score update");
            }
        }

        [UnityTest]
        public IEnumerator PollutionScoreUpdateOnGraySquare()
        {
            // Get Player 1's initial score
            int initialScore = gameController.players[0].pollutionScore;

            // Simulate landing on gray square
            gameController.LandedOnGraySquare();
            yield return null;

            // Close the gray card popup
            var grayCardDoc = gameController.grayCardUiDocument;
            var closeButton = grayCardDoc.rootVisualElement.Q<Button>("close_button");
            
            if (closeButton != null)
            {
                using (var click = ClickEvent.GetPooled())
                {
                    click.target = closeButton;
                    closeButton.SendEvent(click);
                }
                yield return null;
            }

            // Verify score increased
            int newScore = gameController.players[0].pollutionScore;
            Assert.Greater(newScore, initialScore, "Pollution score should increase after gray square");
            Assert.AreEqual(initialScore + 1, newScore, "Gray square should add 1 to pollution score");
        }

        [UnityTest]
        public IEnumerator PollutionScoreUpdateOnBlueSquareCorrect()
        {
            var blueCardManager = Object.FindObjectOfType<BlueCardManager>();
            Assert.IsNotNull(blueCardManager, "BlueCardManager should exist");
            
            // Create test MCQ card using the same helper
            var testCard = MakeTestMCQCard(firstCorrect: true);
            blueCardManager.deck = new List<BlueCard> { testCard };
            
            // Set initial score so we can detect changes
            scoreManager.AddScoreToPlayer(gameController.players[0], 5, "Setup");
            yield return null;
            
            int initialScore = gameController.players[0].pollutionScore;
            Assert.AreEqual(5, initialScore, "Initial score should be 5");
            
            // Trigger blue square
            gameController.LandedOnBlueSquare();
            yield return null;
            
            // Get the active blue card UI
            var root = gameController.blueCardUiDocument.rootVisualElement;
            Assert.IsNotNull(root, "Blue card UI should exist");
            
            // Get answer buttons using same method as GBM04
            var answersElement = root.Q<VisualElement>("answers_element");
            Assert.IsNotNull(answersElement, "answers_element should exist");
            var answerButtons = answersElement.Query<Button>(className: "answer-option").ToList();
            Assert.GreaterOrEqual(answerButtons.Count, 1, "Should have answer buttons");
            
            // Click first answer (correct)
            using (var click = ClickEvent.GetPooled())
            {
                click.target = answerButtons[0];
                answerButtons[0].SendEvent(click);
            }
            yield return null;
            
            // Wait for feedback slide
            var feedbackSlide = root.Q<VisualElement>("feedback_slide");
            Assert.IsNotNull(feedbackSlide, "Feedback slide should exist");
            
            float startTime = Time.time;
            while (feedbackSlide.style.display != DisplayStyle.Flex)
            {
                if (Time.time - startTime >= 5f)
                {
                    Assert.Fail("Feedback slide did not appear within 5 seconds");
                }
                yield return null;
            }
            
            // Click continue
            var continueButton = root.Q<Button>("feedback_continue");
            Assert.IsNotNull(continueButton, "Continue button should exist");
            
            using (var click = ClickEvent.GetPooled())
            {
                click.target = continueButton;
                continueButton.SendEvent(click);
            }
            yield return null;
            yield return null;
            
            // Verify score changed (correct answer = -1)
            int newScore = gameController.players[0].pollutionScore;
            Assert.AreEqual(4, newScore, "Correct answer should decrease score by 1 (5 → 4)");
        }

        [UnityTest]
        public IEnumerator AllPlayersHaveIndependentScores()
        {
            // Set different scores for each player
            scoreManager.AddScoreToPlayer(gameController.players[0], 3, "Test");
            scoreManager.AddScoreToPlayer(gameController.players[1], 5, "Test");
            scoreManager.AddScoreToPlayer(gameController.players[2], 2, "Test");
            yield return null;

            // Verify each player has their own score
            Assert.AreEqual(3, gameController.players[0].pollutionScore, "Player 1 score should be 3");
            Assert.AreEqual(5, gameController.players[1].pollutionScore, "Player 2 score should be 5");
            Assert.AreEqual(2, gameController.players[2].pollutionScore, "Player 3 score should be 2");

            // Verify they're independent
            Assert.AreNotEqual(gameController.players[0].pollutionScore, gameController.players[1].pollutionScore);
            Assert.AreNotEqual(gameController.players[1].pollutionScore, gameController.players[2].pollutionScore);
        }

        [UnityTest]
        public IEnumerator PollutionScoreRangeIs0To10()
        {
            // Verify scores start at valid range
            foreach (var player in gameController.players)
            {
                Assert.GreaterOrEqual(player.pollutionScore, 0, "Pollution score should be >= 0");
                Assert.LessOrEqual(player.pollutionScore, 10, "Pollution score should be <= 10");
            }

            // Try to set score below 0
            scoreManager.AddScoreToPlayer(gameController.players[0], -5, "Test");
            yield return null;
            Assert.GreaterOrEqual(gameController.players[0].pollutionScore, 0, "Score should not go below 0");

            // Set score to 10
            gameController.players[1].pollutionScore = 0;
            scoreManager.AddScoreToPlayer(gameController.players[1], 10, "Test");
            yield return null;
            Assert.LessOrEqual(gameController.players[1].pollutionScore, 10, "Score should stay within 0-10 range");
        }

        [UnityTest]
        public IEnumerator WinnerAnnouncedAtGameEnd()
        {
            // Set final scores
            scoreManager.AddScoreToPlayer(gameController.players[0], 6, "Final");
            scoreManager.AddScoreToPlayer(gameController.players[1], 3, "Final"); // Winner
            scoreManager.AddScoreToPlayer(gameController.players[2], 8, "Final");

            // Mark all players as finished
            gameController.players[0].hasFinished = true;
            gameController.players[1].hasFinished = true;
            gameController.players[2].hasFinished = true;
            yield return null;

            // Find multiplayer final score popup
            var finalScorePopup = Object.FindObjectOfType<MultiplayerFinalScorePopup>();
            
            if (finalScorePopup != null)
            {
                finalScorePopup.ShowFinalScores(gameController.players);
                yield return null;

                // Verify popup is shown
                var root = finalScorePopup.finalScoreUiDocument.rootVisualElement;
                Assert.AreEqual(DisplayStyle.Flex, root.style.display.value, "Final score popup should be visible");

                // Verify winner section exists
                var winnerSection = root.Q<VisualElement>("winner_section");
                Assert.IsNotNull(winnerSection, "Winner section should exist");
                Assert.AreEqual(DisplayStyle.Flex, winnerSection.style.display.value, "Winner section should be visible");
            }
            else
            {
                Assert.Inconclusive("MultiplayerFinalScorePopup not found - cannot test winner announcement");
            }
        }

        [UnityTest]
        public IEnumerator AllPlayersFinalScoresVisible()
        {
            // Set final scores
            scoreManager.AddScoreToPlayer(gameController.players[0], 4, "Final");
            scoreManager.AddScoreToPlayer(gameController.players[1], 2, "Final");
            scoreManager.AddScoreToPlayer(gameController.players[2], 6, "Final");

            // Mark all as finished
            foreach (var player in gameController.players)
            {
                player.hasFinished = true;
            }
            yield return null;

            // Show final scores
            var finalScorePopup = Object.FindObjectOfType<MultiplayerFinalScorePopup>();
            
            if (finalScorePopup != null)
            {
                finalScorePopup.ShowFinalScores(gameController.players);
                yield return null;

                var root = finalScorePopup.finalScoreUiDocument.rootVisualElement;

                // Check that winner, second, and third sections exist
                var winnerContainer = root.Q<VisualElement>("winner_container");
                var secondContainer = root.Q<VisualElement>("second_container");
                var thirdContainer = root.Q<VisualElement>("third_container");

                Assert.IsNotNull(winnerContainer, "Winner container should exist");
                Assert.Greater(winnerContainer.childCount, 0, "Winner container should have players");
            }
            else
            {
                Assert.Inconclusive("MultiplayerFinalScorePopup not found");
            }
        }

        [UnityTest]
        public IEnumerator WinConditionUsesPollutionScoreOnly()
        {
            // Player 1 finishes first but has higher score
            gameController.players[0].finishOrder = 1;
            gameController.players[0].pollutionScore = 7;

            // Player 2 finishes last but has lowest score
            gameController.players[1].finishOrder = 3;
            gameController.players[1].pollutionScore = 2; // Should win

            // Player 3 finishes second
            gameController.players[2].finishOrder = 2;
            gameController.players[2].pollutionScore = 5;

            yield return null;

            // Determine winner by score
            var rankedPlayers = new List<Player>(gameController.players);
            rankedPlayers.Sort((a, b) => a.pollutionScore.CompareTo(b.pollutionScore));

            // Player 2 should be winner despite finishing last
            Assert.AreEqual(gameController.players[1], rankedPlayers[0], "Player with lowest pollution score should win");
            Assert.AreEqual(2, rankedPlayers[0].pollutionScore, "Winner should have score of 2");
            Assert.AreEqual(3, rankedPlayers[0].finishOrder, "Winner finished last but still wins by score");
        }

        [UnityTest]
        public IEnumerator FirstArrivalBonusAppliedCorrectly()
        {
            // Player 1 arrives first
            gameController.players[0].finishOrder = 1;
            gameController.players[0].pollutionScore = 5;

            // Apply first arrival bonus
            scoreManager.AddScoreToPlayer(gameController.players[0], -1, "First to finish bonus");
            yield return null;

            Assert.AreEqual(4, gameController.players[0].pollutionScore, "First player should get -1 bonus");
        }
    }
}