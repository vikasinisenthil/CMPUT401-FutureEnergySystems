using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement
{
    public class GBM11_Wrong_Answer_Backstep
    {
        private GameController gc;
        private BlueCardManager blueCardManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("BoardScene");
            yield return null;

            gc = Object.FindFirstObjectByType<GameController>();
            blueCardManager = Object.FindFirstObjectByType<BlueCardManager>();

            Assert.IsNotNull(gc, "GameController not found in BoardScene.");
            Assert.IsNotNull(blueCardManager, "BlueCardManager not found in BoardScene.");
            Assert.IsNotNull(ScoreManager.Instance, "ScoreManager.Instance is null (ScoreManager missing in scene?).");
        }

        private static Sprite MakeDummySprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private BlueMCQCard MakeTestMCQCard_FirstIsWrong_SecondIsCorrect()
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Test MCQ Backstep";
            card.image = MakeDummySprite();
            card.statement = "Test Statement";
            card.question = "Pick the correct one";

            card.answers = new List<MCQAnswer>()
            {
                new MCQAnswer { answer = "Wrong",   messageWhenChosen = "PLACEHOLDER", correctAnswer = false },
                new MCQAnswer { answer = "Correct", messageWhenChosen = "PLACEHOLDER", correctAnswer = true  }
            };

            return card;
        }

        private void ForceDeckToSingleCard(BlueCard card)
        {
            blueCardManager.deck = new List<BlueCard>() { card };
        }

        private static void ClickVisualElement(VisualElement ve)
        {
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = ve;
                ve.SendEvent(click);
            }
        }

        private IEnumerator WaitForMCQUI()
        {
            yield return null;
        }

        private (VisualElement answers, VisualElement feedbackSlide, Label result, Button cont) GetUI()
        {
            var root = gc.blueCardUiDocument.rootVisualElement;
            Assert.IsNotNull(root);

            var answers = root.Q<VisualElement>("answers_element");
            var feedbackSlide = root.Q<VisualElement>("feedback_slide");
            var result = root.Q<Label>("feedback_result");
            var cont = root.Q<Button>("feedback_continue");

            Assert.IsNotNull(answers, "answers_element missing");
            Assert.IsNotNull(feedbackSlide, "feedback_slide missing");
            Assert.IsNotNull(result, "feedback_result missing");
            Assert.IsNotNull(cont, "feedback_continue missing");

            return (answers, feedbackSlide, result, cont);
        }

        private List<Button> GetAnswerButtons(VisualElement answersElement)
        {
            var btns = answersElement.Query<Button>(className: "answer-option").ToList();
            Assert.GreaterOrEqual(btns.Count, 2, "Need at least 2 answer buttons.");
            return btns;
        }

        private IEnumerator WaitForFeedbackToAppear(VisualElement feedbackSlide, Label resultLabel)
        {
            float startTime = Time.time;
            while(true)
            {
                if (Time.time - startTime > 60 * 5) break;
                
                if (feedbackSlide.style.display == DisplayStyle.Flex)
                    yield break;

                if (feedbackSlide.resolvedStyle.display == DisplayStyle.Flex)
                    yield break;

                yield return null;
            }

            Assert.Fail(
                $"Feedback slide did not appear in time. " +
                $"style.display={feedbackSlide.style.display}, " +
                $"resolvedStyle.display={feedbackSlide.resolvedStyle.display}, " +
                $"feedback_result='{resultLabel.text}'");
        }

        private IEnumerator WaitForMovementToFinish()
        {
            yield return new WaitUntil(() => !gc.player.moving);
        }

        private IEnumerator WaitUntilMovingStarts()
        {
            yield return null;
        }

        private void SetPlayerPositionIndex(int idx)
        {
            gc.player.moving = false;
            gc.player.boardSquareIndex = idx;
            gc.player.nextBoardSquareIndex = idx;
            gc.player.finalBoardSquareIndex = idx;
            gc.player.gameObject.transform.position = gc.boardSquares[idx].transform.position;
        }

        [UnityTest]
        public IEnumerator AT1_WrongAnswer_MovesBackExactlyOne()
        {
            ForceDeckToSingleCard(MakeTestMCQCard_FirstIsWrong_SecondIsCorrect());

            SetPlayerPositionIndex(2);
            int startIndex = gc.player.boardSquareIndex;

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]);
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            ClickVisualElement(ui.cont);
            yield return WaitUntilMovingStarts();
            yield return WaitForMovementToFinish();

            Assert.AreEqual(startIndex - 1, gc.player.boardSquareIndex, "Player should move back exactly one square.");
        }

        [UnityTest]
        public IEnumerator AT2_Backstep_HappensAfterFeedbackDismiss()
        {
            ForceDeckToSingleCard(MakeTestMCQCard_FirstIsWrong_SecondIsCorrect());

            SetPlayerPositionIndex(3);
            int startIndex = gc.player.boardSquareIndex;

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]);
            yield return null;
            Assert.IsFalse(gc.player.moving, "Player should not move backward immediately after clicking answer.");
            Assert.AreEqual(startIndex, gc.player.boardSquareIndex, "Position should not change before feedback dismissal.");

            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);
            Assert.AreEqual(DisplayStyle.Flex, ui.feedbackSlide.resolvedStyle.display, "Feedback should be visible after answer delay.");
            Assert.IsFalse(gc.player.moving, "Player should not move backward until feedback is dismissed.");
            Assert.AreEqual(startIndex, gc.player.boardSquareIndex, "Position should not change before feedback dismissal.");

            ClickVisualElement(ui.cont);
            yield return WaitUntilMovingStarts();

            Assert.IsTrue(gc.player.moving, "Player should begin moving only after dismissing feedback.");
        }

        [UnityTest]
        public IEnumerator AT3_AtStart_NoBackwardMove()
        {
            ForceDeckToSingleCard(MakeTestMCQCard_FirstIsWrong_SecondIsCorrect());

            SetPlayerPositionIndex(0);

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]);
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            ClickVisualElement(ui.cont);
            yield return null;
            yield return null;

            Assert.AreEqual(0, gc.player.boardSquareIndex, "Player should remain at Start (0).");
            Assert.IsFalse(gc.player.moving, "Player should not start moving when already at Start.");
        }

        [UnityTest]
        public IEnumerator AT4_WrongAnswer_IncreasesScoreBy1()
        {
            ForceDeckToSingleCard(MakeTestMCQCard_FirstIsWrong_SecondIsCorrect());

            int before = ScoreManager.Instance.GetScore();

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]);
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            int after = ScoreManager.Instance.GetScore();
            Assert.AreEqual(before + 1, after, "Wrong answer should increase AQHI/pollution score by 1.");
        }

        [UnityTest]
        public IEnumerator AT5_ShowsIncorrectFeedbackBeforeMove()
        {
            ForceDeckToSingleCard(MakeTestMCQCard_FirstIsWrong_SecondIsCorrect());

            SetPlayerPositionIndex(2);

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]);
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            Assert.AreEqual(DisplayStyle.Flex, ui.feedbackSlide.resolvedStyle.display, "Feedback should be visible.");
            Assert.IsTrue(ui.result.text.ToLower().Contains("incorrect"),
                "feedback_result should clearly indicate the answer was incorrect.");

            Assert.IsFalse(gc.player.moving, "Movement should not start until feedback dismissed.");
        }

        [UnityTest]
        public IEnumerator AT6_PositionUpdatedAfterBackwardMove()
        {
            ForceDeckToSingleCard(MakeTestMCQCard_FirstIsWrong_SecondIsCorrect());

            SetPlayerPositionIndex(4);
            int startIndex = gc.player.boardSquareIndex;

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]);
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            ClickVisualElement(ui.cont);
            yield return WaitUntilMovingStarts();
            yield return WaitForMovementToFinish();

            int expected = startIndex - 1;
            Assert.AreEqual(expected, gc.player.boardSquareIndex, "boardSquareIndex should reflect the new position.");

            var expectedPos = gc.boardSquares[expected].transform.position;
            var tokenPos = gc.player.gameObject.transform.position;

            float distance = Vector2.Distance(
                new Vector2(tokenPos.x, tokenPos.y),
                new Vector2(expectedPos.x, expectedPos.y)
            );

            Assert.Less(distance, 0.2f, "Player token should end near the expected board square position.");
        }
    }
}
