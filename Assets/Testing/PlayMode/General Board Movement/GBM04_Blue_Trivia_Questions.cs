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
    [Category("General Board Movement")]
    public class GBM04_Blue_Trivia_Questions
    {
        private GameController gc;
        private BlueCardManager blueCardManager;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;

            gc = Object.FindFirstObjectByType<GameController>();
            blueCardManager = Object.FindFirstObjectByType<BlueCardManager>();

            Assert.IsNotNull(gc, "GameController not found in BoardScene.");
            Assert.IsNotNull(blueCardManager, "BlueCardManager not found in BoardScene.");
            Assert.IsNotNull(ScoreManager.Instance, "ScoreManager.Instance is null.");
        }

        private static Sprite MakeDummySprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private static BlueMCQCard MakeTestMCQCard(
            bool firstCorrect,
            string firstText = "ANSWER_1",
            string secondText = "ANSWER_2")
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "TEST_NAME";
            card.statement = "TEST_STATEMENT";
            card.question = "TEST_QUESTION";
            card.image = MakeDummySprite();
            card.answers = new List<MCQAnswer>
            {
                new MCQAnswer
                {
                    answer = firstText,
                    correctAnswer = firstCorrect,
                    messageWhenChosen = "ANSWER_1_MESSAGE_WHEN_CHOSEN"
                },
                new MCQAnswer
                {
                    answer = secondText,
                    correctAnswer = !firstCorrect,
                    messageWhenChosen = "ANSWER_2_MESSAGE_WHEN_CHOSEN"
                }
            };
            return card;
        }

        private void ForceDeckToSingleCard(BlueCard card)
        {
            blueCardManager.deck = new List<BlueCard> { card };
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

        private IEnumerator WaitForFeedbackToAppear(VisualElement feedbackSlide, Label resultLabel)
        {
            yield return null;
        }

        private static List<Button> GetAnswerButtons(VisualElement root)
        {
            var answersElement = root.Q<VisualElement>("answers_element");
            Assert.IsNotNull(answersElement, "answers_element missing.");

            var buttons = answersElement.Query<Button>(className: "answer-option").ToList();
            Assert.GreaterOrEqual(buttons.Count, 2, "Need at least two answer-option buttons.");
            return buttons;
        }

        [UnityTest]
        public IEnumerator LandingOnBlueSquareCanDrawMCQCard()
        {
            int mcqCardCount = 0;
            foreach (BlueCard card in blueCardManager.deck)
            {
                if (card is BlueMCQCard)
                {
                    mcqCardCount++;
                    break;
                }
            }

            Assert.Greater(mcqCardCount, 0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerCanOnlySelectOneAnswer()
        {
            ForceDeckToSingleCard(MakeTestMCQCard(firstCorrect: true));

            int beforeTotalAnswered = gc.triviaCorrectCount + gc.triviaIncorrectCount;

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answerButtons = GetAnswerButtons(root);

            ClickVisualElement(answerButtons[0]);
            ClickVisualElement(answerButtons[1]); // should have no effect after first click
            yield return null;
            // yield return WaitForFeedbackToAppear(root.Q<VisualElement>("feedback_slide"), root.Q<Label>("feedback_result"));

            int afterTotalAnswered = gc.triviaCorrectCount + gc.triviaIncorrectCount;
            Assert.AreEqual(beforeTotalAnswered + 1, afterTotalAnswered, "Only one answer should be accepted.");
        }

        [UnityTest]
        public IEnumerator EachQuizCardHasOneCorrectAnswer()
        {
            foreach (BlueCard card in blueCardManager.deck)
            {
                if (!(card is BlueMCQCard mcqCard)) continue;

                int numberOfCorrect = 0;
                foreach (MCQAnswer ans in mcqCard.answers)
                {
                    if (ans.correctAnswer) numberOfCorrect++;
                }

                Assert.AreEqual(1, numberOfCorrect, $"The card: \"{card.cardName}\" does not have exactly 1 correct answer.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator CorrectAnswerDecreasesPollution()
        {
            ForceDeckToSingleCard(MakeTestMCQCard(firstCorrect: true));

            ScoreManager.Instance.AddScore(-100);
            ScoreManager.Instance.AddScore(5);

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answerButtons = GetAnswerButtons(root);
            var feedbackSlide = root.Q<VisualElement>("feedback_slide");
            var resultLabel = root.Q<Label>("feedback_result");
            var continueButton = root.Q<Button>("feedback_continue");

            ClickVisualElement(answerButtons[0]); // correct
            yield return WaitForFeedbackToAppear(feedbackSlide, resultLabel);
            ClickVisualElement(continueButton);
            yield return null;

            Assert.AreEqual(4, ScoreManager.Instance.GetScore());
        }

        [UnityTest]
        public IEnumerator IncorrectAnswerIncreasesPollution()
        {
            ForceDeckToSingleCard(MakeTestMCQCard(firstCorrect: false));

            ScoreManager.Instance.AddScore(-100);
            ScoreManager.Instance.AddScore(5);

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answerButtons = GetAnswerButtons(root);
            var feedbackSlide = root.Q<VisualElement>("feedback_slide");
            var resultLabel = root.Q<Label>("feedback_result");
            var continueButton = root.Q<Button>("feedback_continue");

            ClickVisualElement(answerButtons[0]); // incorrect
            yield return WaitForFeedbackToAppear(feedbackSlide, resultLabel);
            ClickVisualElement(continueButton);
            yield return null;

            Assert.AreEqual(6, ScoreManager.Instance.GetScore());
        }

        [UnityTest]
        public IEnumerator EnsureDismissButtonShows()
        {
            ForceDeckToSingleCard(MakeTestMCQCard(firstCorrect: false));

            gc.LandedOnBlueSquare();
            yield return WaitForMCQUI();

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answerButtons = GetAnswerButtons(root);
            var feedbackSlide = root.Q<VisualElement>("feedback_slide");
            var resultLabel = root.Q<Label>("feedback_result");
            var continueButton = root.Q<Button>("feedback_continue");

            ClickVisualElement(answerButtons[0]); // incorrect
            yield return WaitForFeedbackToAppear(feedbackSlide, resultLabel);

            Assert.IsNotNull(continueButton, "feedback_continue button missing.");
            Assert.IsTrue(continueButton.enabledSelf, "feedback_continue should be enabled.");
        }
    }
}
