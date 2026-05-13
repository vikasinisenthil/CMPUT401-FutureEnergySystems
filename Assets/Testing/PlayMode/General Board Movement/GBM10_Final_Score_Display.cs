using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    public class GBM10_Final_Score_Display
    {
        private GameController gc;
        private BlueCardManager blueCardManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PlayerPrefs.SetInt("SkipBossForTests", 1);
            PlayerPrefs.Save();
            
            SceneManager.LoadScene("BoardScene");
            yield return null;
            yield return null;

            gc = Object.FindFirstObjectByType<GameController>();
            blueCardManager = Object.FindFirstObjectByType<BlueCardManager>();

            Assert.IsNotNull(gc, "GameController not found in BoardScene.");
            Assert.IsNotNull(ScoreManager.Instance, "ScoreManager missing in scene.");
            Assert.IsNotNull(blueCardManager, "BlueCardManager missing in scene.");
            Assert.IsNotNull(gc.inGameUiDocument, "inGameUiDocument not assigned on GameController.");

            // Skip the boss fight so end-game tests reach the final score popup directly.
            var bossManager = Object.FindFirstObjectByType<BossManager>();
            if (bossManager != null) bossManager.SkipBoss();
            
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Clean up the skip flag
            PlayerPrefs.DeleteKey("SkipBossForTests");
            PlayerPrefs.Save();
            yield return null;
        }

        private static Sprite MakeDummySprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private BlueFactCard MakeTestFactCard()
        {
            var card = ScriptableObject.CreateInstance<BlueFactCard>();
            card.cardName = "Test Fact";
            card.image = MakeDummySprite();
            card.fact = "Fact text.";
            return card;
        }

        private BlueMCQCard MakeTestMCQCard(bool firstCorrect)
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Test MCQ";
            card.image = MakeDummySprite();
            card.statement = "Statement";
            card.question = "Question";
            card.answers = new List<MCQAnswer>()
            {
                new MCQAnswer { answer="A", messageWhenChosen="PLACEHOLDER", correctAnswer=firstCorrect },
                new MCQAnswer { answer="B", messageWhenChosen="PLACEHOLDER", correctAnswer=!firstCorrect }
            };
            return card;
        }

        private void ForceDeckToSingleCard(BlueCard card)
        {
            blueCardManager.deck = new List<BlueCard> { card };
        }

        private void SnapPlayerToIndex(int idx)
        {
            gc.player.moving = false;
            gc.player.boardSquareIndex = idx;
            gc.player.nextBoardSquareIndex = idx;
            gc.player.finalBoardSquareIndex = idx;
            gc.player.gameObject.transform.position = gc.boardSquares[idx].transform.position;
        }

        private IEnumerator WaitForMovementToFinish()
        {
            yield return new WaitUntil(() => !gc.player.moving);
        }

        private static void ClickVE(VisualElement ve)
        {
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = ve;
                ve.SendEvent(click);
            }
        }

        private VisualElement FinalRoot()
        {
            Assert.IsNotNull(gc.finalScorePopup, "finalScorePopup not assigned in GameController.");
            var doc = gc.finalScorePopup.GetComponent<UIDocument>();
            Assert.IsNotNull(doc, "FinalScorePopup must be on an object with UIDocument.");
            return doc.rootVisualElement;
        }

        private IEnumerator WaitForBlueCardUI()
        {
            yield return null;
        }

        private IEnumerator WaitForFeedbackToAppear(VisualElement root)
        {
            yield return null;
        }

        // 1. A player can start the game by choosing a character.
        [UnityTest]
        public IEnumerator AT1_PlayerTokenExistsAtStart()
        {
            Assert.IsNotNull(gc.player.gameObject, "Player token GameObject not assigned.");
            Assert.AreEqual(0, gc.player.boardSquareIndex, "Player should start at index 0.");
            yield return null;
        }

        // 2. The player rolls a dice and moves forward.
        [UnityTest]
        public IEnumerator AT2_PlayerMovesForward()
        {
            SnapPlayerToIndex(0);
            bool moved = gc.MovePlayer(1, triggerLandOn: false, showCountdown: false);
            Assert.IsTrue(moved, "MovePlayer(1) should return true.");
            yield return WaitForMovementToFinish();
            Assert.AreEqual(1, gc.player.boardSquareIndex, "Player should end at index 1 after moving 1.");
        }

        // 3. On a grey spot, the player grabs a pom pom (score +1).
        [UnityTest]
        public IEnumerator AT3_GraySquare_IncreasesScore()
        {
            ScoreManager.Instance.AddScore(-100); // reset to 0
            int before = ScoreManager.Instance.GetScore();

            gc.LandedOnGraySquare();
            yield return null;

            Assert.IsNotNull(gc.grayCardUiDocument, "grayCardUiDocument is not assigned on GameController.");
            var cardBox = gc.grayCardUiDocument.rootVisualElement.Q<VisualElement>("card_box");
            Assert.IsNotNull(cardBox, "Gray card 'card_box' not found (UXML name mismatch).");

            var closeBtn = cardBox.Q<Button>("close_button");
            Assert.IsNotNull(closeBtn, "Gray card 'close_button' not found (UXML name mismatch).");
            var click = ClickEvent.GetPooled();
            click.target = closeBtn;
            closeBtn.SendEvent(click);

            yield return null;

            int after = ScoreManager.Instance.GetScore();
            Assert.AreEqual(before + 1, after, "Gray square should increase score by 1 after closing the gray card.");
        }

        // 4. On a blue spot, player picks a card (fact or question).
        [UnityTest]
        public IEnumerator AT4_BlueSquare_OpensCardUI()
        {
            ForceDeckToSingleCard(MakeTestFactCard());

            gc.LandedOnBlueSquare();
            yield return WaitForBlueCardUI();

            Assert.IsNotNull(gc.blueCardUiDocument, "Blue card UI document should be set.");
            Assert.AreEqual(DisplayStyle.Flex, gc.blueCardUiDocument.rootVisualElement.resolvedStyle.display,
                "Blue card UI should be visible.");
        }

        // 5. If question answered correctly, remove one pom pom (score -1).
        [UnityTest]
        public IEnumerator AT5_CorrectAnswer_DecreasesScore()
        {
            ScoreManager.Instance.AddScore(-100); // 0
            ScoreManager.Instance.AddScore(3);    // start at 3
            int before = ScoreManager.Instance.GetScore();

            ForceDeckToSingleCard(MakeTestMCQCard(firstCorrect: true));
            gc.LandedOnBlueSquare();
            yield return WaitForBlueCardUI();

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answersEl = root.Q<VisualElement>("answers_element");
            var answerButtons = answersEl.Query<Button>(className: "answer-option").ToList();

            ClickVE(answerButtons[0]); // correct
            yield return WaitForFeedbackToAppear(root);

            int after = ScoreManager.Instance.GetScore();
            Assert.AreEqual(before - 1, after, "Correct answer should decrease score by 1.");
        }

        // 6. If answered incorrectly, player moves back by one spot.
        [UnityTest]
        public IEnumerator AT6_WrongAnswer_MovesBackOne()
        {
            SnapPlayerToIndex(2);

            ForceDeckToSingleCard(MakeTestMCQCard(firstCorrect: false)); // first is wrong
            gc.LandedOnBlueSquare();
            yield return WaitForBlueCardUI();

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answersEl = root.Q<VisualElement>("answers_element");
            var answerButtons = answersEl.Query<Button>(className: "answer-option").ToList();

            // Wrong answer = first
            ClickVE(answerButtons[0]);
            yield return WaitForFeedbackToAppear(root);

            // Continue triggers backstep in your BlueMCQCard
            var cont = root.Q<Button>("feedback_continue");
            Assert.IsNotNull(cont, "feedback_continue not found.");
            ClickVE(cont);
            yield return null;

            yield return WaitForMovementToFinish();
            Assert.AreEqual(1, gc.player.boardSquareIndex, "Wrong answer should move player back by 1.");
        }

        // 7. The game continues this way.
        [UnityTest]
        public IEnumerator AT7_GameContinues_DiceButtonExists()
        {
            var dice = gc.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.IsNotNull(dice, "dice_button should exist during game.");
            yield return null;
        }

        // 8. When the game ends, final pom poms determine points (final score popup shows final score).
        [UnityTest]
        public IEnumerator AT8_EndGame_ShowsFinalScore()
        {
            ScoreManager.Instance.AddScore(-100);
            ScoreManager.Instance.AddScore(7);

            int last = gc.boardSquares.Count - 1;
            SnapPlayerToIndex(last - 1);

            gc.MovePlayer(1, triggerLandOn: false, showCountdown: false);
            yield return null;
            yield return WaitForMovementToFinish();

            yield return null;
            yield return null;

            var root = FinalRoot();
            var overlay = root.Q<VisualElement>("overlay");
            var aqhi = root.Q<Label>("final_aqhi");

            Assert.IsNotNull(overlay, "overlay missing in FinalScore.uxml.");
            Assert.IsNotNull(aqhi, "final_aqhi missing in FinalScore.uxml.");
            Assert.AreEqual(DisplayStyle.Flex, overlay.resolvedStyle.display, "Final overlay should be visible.");
            Assert.IsTrue(aqhi.text.Contains("7"), $"Expected Final AQHI label to contain 7, got: {aqhi.text}");
        }

        // 9. Game checks corresponding AQHI with pom poms collected.
        [UnityTest]
        public IEnumerator AT9_AQHILabelMatchesScoreValue()
        {
            ScoreManager.Instance.AddScore(-100);
            ScoreManager.Instance.AddScore(4);
            int expected = ScoreManager.Instance.GetScore();

            int last = gc.boardSquares.Count - 1;
            SnapPlayerToIndex(last - 1);

            gc.MovePlayer(1, triggerLandOn: false, showCountdown: false);
            yield return null;
            yield return WaitForMovementToFinish();

            var root = FinalRoot();
            var aqhi = root.Q<Label>("final_aqhi");
            Assert.IsNotNull(aqhi, "final_aqhi label missing.");

            Assert.IsTrue(aqhi.text.Contains(expected.ToString()),
                $"AQHI label should reflect collected pom poms / score. Expected {expected}, got: {aqhi.text}");
        }
    }
}
