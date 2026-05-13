using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    [Category("General Board Movement")]
    public class GBM09_Choose_Trivia_Answer
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

        private BlueMCQCard MakeTestMCQCard()
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Test MCQ";
            card.image = MakeDummySprite();

            card.statement = "Test Statement";
            card.question = "What is 2 + 2?";

            card.answers = new List<MCQAnswer>()
            {
                new MCQAnswer { answer = "3", messageWhenChosen = "Nope",    correctAnswer = false },
                new MCQAnswer { answer = "4", messageWhenChosen = "Correct!", correctAnswer = true  },
                new MCQAnswer { answer = "5", messageWhenChosen = "Nope",    correctAnswer = false },
            };

            return card;
        }

        private BlueFactCard MakeTestFactCard()
        {
            var card = ScriptableObject.CreateInstance<BlueFactCard>();
            card.cardName = "Test Fact";
            card.image = MakeDummySprite();

            card.fact = "Just a fact (not a question).";
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

        private IEnumerator WaitForBlueCardUIWithAnswers()
        {
            yield return null;
        }

        private IEnumerator WaitForFeedbackToAppear(VisualElement root)
        {
            // yield return null;

            var feedbackSlide = root.Q<VisualElement>("feedback_slide");
            var feedbackResult = root.Q<Label>("feedback_result");
            Assert.IsNotNull(feedbackSlide, "feedback_slide missing.");
            Assert.IsNotNull(feedbackResult, "feedback_result missing.");

            float startTime = Time.time;

            int i;
            while(true)
            {
                if (Time.time - startTime > 60 * 5) break;
                
                if (feedbackSlide.style.display == DisplayStyle.Flex) yield break;
                if (feedbackSlide.resolvedStyle.display == DisplayStyle.Flex) yield break;
                yield return null;
            }

            Assert.Fail("Feedback did not appear in time after selecting an answer.");
        }

        [UnityTest]
        public IEnumerator ShowsAnswerOptions_WhenTriviaCardDrawn()
        {
            ForceDeckToSingleCard(MakeTestMCQCard());

            gc.LandedOnBlueSquare();
            yield return WaitForBlueCardUIWithAnswers();

            Assert.IsNotNull(gc.blueCardUiDocument, "Expected a UI document after drawing a blue card.");

            var root = gc.blueCardUiDocument.rootVisualElement;

            var answersElement = root.Q<VisualElement>("answers_element");
            Assert.IsNotNull(answersElement, "answers_element not found (UXML changed?).");

            var answerButtons = answersElement.Query<Button>(className: "answer-option").ToList();
            Assert.AreEqual(3, answerButtons.Count, "Expected 3 answer options for the test MCQ card.");
        }

        [UnityTest]
        public IEnumerator SelectingAnswer_VisuallyIndicatesOnlyOneSelected()
        {
            ForceDeckToSingleCard(MakeTestMCQCard());

            gc.LandedOnBlueSquare();
            yield return WaitForBlueCardUIWithAnswers();

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answersElement = root.Q<VisualElement>("answers_element");

            var answerButtons = answersElement.Query<Button>(className: "answer-option").ToList();
            Assert.GreaterOrEqual(answerButtons.Count, 2, "Need at least 2 answer buttons for this test.");

            ClickVisualElement(answerButtons[0]);
            yield return WaitForFeedbackToAppear(root);

            foreach (var b in answerButtons)
                Assert.IsFalse(b.enabledSelf, "Answer buttons should lock after selecting one answer.");
        }

        [UnityTest]
        public IEnumerator AfterSelection_SubmitEvaluatesAndLocksIn()
        {
            ForceDeckToSingleCard(MakeTestMCQCard());

            gc.LandedOnBlueSquare();
            yield return WaitForBlueCardUIWithAnswers();

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answersElement = root.Q<VisualElement>("answers_element");

            var answerButtons = answersElement.Query<Button>(className: "answer-option").ToList();
            Assert.GreaterOrEqual(answerButtons.Count, 2);

            ClickVisualElement(answerButtons[1]);
            yield return WaitForFeedbackToAppear(root);

            foreach (var b in answerButtons)
                Assert.IsFalse(b.enabledSelf, "Answer buttons should be disabled after selecting/submitting.");

            var feedbackSlide = root.Q<VisualElement>("feedback_slide");
            var feedbackResult = root.Q<Label>("feedback_result");
            Assert.AreEqual(DisplayStyle.Flex, feedbackSlide.resolvedStyle.display, "Feedback should be visible after selecting.");
            Assert.IsFalse(string.IsNullOrEmpty(feedbackResult.text), "Feedback result text should be populated.");
        }

        [UnityTest]
        public IEnumerator FactCard_DoesNotShowMCQOptions()
        {
            ForceDeckToSingleCard(MakeTestFactCard());

            gc.LandedOnBlueSquare();
            yield return null;
            yield return null;

            Assert.IsNotNull(gc.blueCardUiDocument);

            var root = gc.blueCardUiDocument.rootVisualElement;

            var answersElement = root.Q<VisualElement>("answers_element");
            if (answersElement != null)
            {
                var anyAnswerButtons = answersElement.Query<Button>(className: "answer-option").ToList();
                Assert.AreEqual(0, anyAnswerButtons.Count, "Fact card should not show MCQ answer buttons.");
            }

            var factLabel = root.Q<Label>("card_fact");
            Assert.IsNotNull(factLabel, "Expected Fact card UI (card_fact label missing).");
        }
    }
}
