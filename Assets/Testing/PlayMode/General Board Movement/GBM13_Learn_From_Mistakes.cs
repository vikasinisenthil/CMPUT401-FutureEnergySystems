using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    public class GBM13_LearnFromMistakes
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
        }

        private static Sprite MakeDummySprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private BlueMCQCard MakeTestMCQCard(bool firstAnswerCorrect)
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Test MCQ";
            card.image = MakeDummySprite();

            card.statement = "Test Statement";
            card.question = "True or False?";

            card.answers = new List<MCQAnswer>()
            {
                new MCQAnswer { answer = "True",  messageWhenChosen = "PLACEHOLDER", correctAnswer = firstAnswerCorrect },
                new MCQAnswer { answer = "False", messageWhenChosen = "PLACEHOLDER", correctAnswer = !firstAnswerCorrect },
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
            float startTime = Time.time;
            while(true)
            {
                if (Time.time - startTime > 60 * 5) break;

                if (gc.blueCardUiDocument != null)
                {
                    var root = gc.blueCardUiDocument.rootVisualElement;
                    if (root == null) { yield return null; continue; }

                    // Require key elements for GBM.13
                    var answersElement = root.Q<VisualElement>("answers_element");
                    var questionSlide = root.Q<VisualElement>("question_slide");
                    var feedbackSlide = root.Q<VisualElement>("feedback_slide");
                    var feedbackResult = root.Q<Label>("feedback_result");
                    var feedbackReason = root.Q<Label>("feedback_reason");

                    if (answersElement != null && questionSlide != null && feedbackSlide != null
                        && feedbackResult != null && feedbackReason != null)
                    {
                        yield break;
                    }
                }
                yield return null;
            }

            Assert.Fail("MCQ UI did not initialize (required elements missing: answers_element/question_slide/feedback_slide/feedback_result/feedback_reason).");
        }

        private (VisualElement root, VisualElement answers, VisualElement questionSlide, VisualElement feedbackSlide, Label result, Label reason) GetUI()
        {
            var root = gc.blueCardUiDocument.rootVisualElement;
            Assert.IsNotNull(root);

            var answersElement = root.Q<VisualElement>("answers_element");
            var questionSlide = root.Q<VisualElement>("question_slide");
            var feedbackSlide = root.Q<VisualElement>("feedback_slide");
            var feedbackResult = root.Q<Label>("feedback_result");
            var feedbackReason = root.Q<Label>("feedback_reason");

            Assert.IsNotNull(answersElement, "answers_element missing");
            Assert.IsNotNull(questionSlide, "question_slide missing");
            Assert.IsNotNull(feedbackSlide, "feedback_slide missing");
            Assert.IsNotNull(feedbackResult, "feedback_result missing");
            Assert.IsNotNull(feedbackReason, "feedback_reason missing");

            return (root, answersElement, questionSlide, feedbackSlide, feedbackResult, feedbackReason);
        }

        private List<Button> GetAnswerButtons(VisualElement answersElement)
        {
            var btns = answersElement.Query<Button>(className: "answer-option").ToList();
            Assert.GreaterOrEqual(btns.Count, 2, "Need at least 2 answer buttons for this test.");
            return btns;
        }

        private IEnumerator WaitForFeedbackToAppear(VisualElement feedbackSlide, Label resultLabel)
        {
            float startTime = Time.time;

            while(true)
            {                
                if (Time.time - startTime > 60 * 5) break;

                if (feedbackSlide.style.display == DisplayStyle.Flex) yield break;
                if (feedbackSlide.resolvedStyle.display == DisplayStyle.Flex) yield break;
                yield return null;
            }

            Assert.Fail(
                $"Feedback did not appear in time. style.display={feedbackSlide.style.display}, " +
                $"resolvedStyle.display={feedbackSlide.resolvedStyle.display}, result='{resultLabel.text}'.");
        }

        // AT1: After selecting an answer, feedback/explanation panel is shown.
        [UnityTest]
        public IEnumerator AT1_FeedbackAppearsAfterSubmit()
        {
            ForceDeckToSingleCard(MakeTestMCQCard(firstAnswerCorrect: true));

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]);
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            Assert.AreEqual(DisplayStyle.Flex, ui.feedbackSlide.resolvedStyle.display, "feedback_slide should be visible after answer.");
            Assert.AreEqual(DisplayStyle.None, ui.questionSlide.resolvedStyle.display, "question_slide should be hidden after answer.");
        }

        // AT2: Feedback is hidden before selection, then appears after selecting an answer (no submit button required).
        [UnityTest]
        public IEnumerator AT2_FeedbackNotVisibleBeforeSubmit()
        {
            ForceDeckToSingleCard(MakeTestMCQCard(firstAnswerCorrect: true));

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            Assert.AreEqual(DisplayStyle.None, ui.feedbackSlide.style.display.value, "Precondition failed: feedback_slide should start hidden.");
            Assert.AreEqual(DisplayStyle.Flex, ui.questionSlide.style.display.value, "Precondition failed: question_slide should start visible.");

            ClickVisualElement(answers[0]);
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            Assert.AreEqual(DisplayStyle.Flex, ui.feedbackSlide.resolvedStyle.display, "feedback_slide should appear after selecting an answer.");
            Assert.AreEqual(DisplayStyle.None, ui.questionSlide.resolvedStyle.display, "question_slide should hide after selecting an answer.");
        }

        // AT3: Correct answer shows "Correct" state (not checking explanation content).
        [UnityTest]
        public IEnumerator AT3_CorrectAnswerShowsCorrectState()
        {
            // Make first answer correct
            ForceDeckToSingleCard(MakeTestMCQCard(firstAnswerCorrect: true));

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]); // correct
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            StringAssert.Contains("Correct", ui.result.text, "feedback_result should indicate Correct for a correct answer.");
            Assert.AreEqual(DisplayStyle.Flex, ui.feedbackSlide.resolvedStyle.display, "feedback_slide should be visible.");
        }

        // AT4: Incorrect answer shows "Incorrect" state (not checking explanation content).
        [UnityTest]
        public IEnumerator AT4_IncorrectAnswerShowsIncorrectState()
        {
            // Make first answer correct, then pick second (incorrect)
            ForceDeckToSingleCard(MakeTestMCQCard(firstAnswerCorrect: true));

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[1]); // incorrect
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            Assert.IsTrue(ui.result.text.ToLower().Contains("incorrect"),
                "feedback_result should indicate incorrect for an incorrect answer.");
            Assert.AreEqual(DisplayStyle.Flex, ui.feedbackSlide.resolvedStyle.display, "feedback_slide should be visible.");
        }

        // AT5: Feedback stays visible until dismissed + inputs remain locked.
        [UnityTest]
        public IEnumerator AT5_FeedbackPersistsUntilDismissed_AndLocksInputs()
        {
            ForceDeckToSingleCard(MakeTestMCQCard(firstAnswerCorrect: true));

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var ui = GetUI();
            var answers = GetAnswerButtons(ui.answers);

            ClickVisualElement(answers[0]);
            yield return WaitForFeedbackToAppear(ui.feedbackSlide, ui.result);

            foreach (var b in answers)
                Assert.IsFalse(b.enabledSelf, "Answer buttons should be disabled after selecting/submitting.");

            // Feedback remains visible if we do NOT press continue
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, ui.feedbackSlide.resolvedStyle.display, "feedback_slide should remain visible until dismissed.");
            Assert.AreEqual(DisplayStyle.None, ui.questionSlide.resolvedStyle.display, "question_slide should remain hidden while feedback is visible.");
        }
    }
}
