using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AdditionalBoardEvents
{
    [Category("Additional Board Events")]
    public class ABE08_Learn_From_Boss_Mistakes
    {
        private GameController _gc;
        private BossManager _bossManager;
        private VisualElement _bossRoot;

        private const string BOARD_SCENE = "Assets/Scenes/BoardScene.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PlayerPrefs.SetInt(BossManager.PREF_SKIP_BOSS_ANSWER_DELAY_FOR_TESTS, 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            while (SceneManager.GetActiveScene().name != "BoardScene")
                yield return null;
            yield return null;

            _gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(_gc, "GameController not found in BoardScene.");

            _bossManager = _gc.GetComponent<BossManager>();
            Assert.IsNotNull(_bossManager, "BossManager not found on GameController.");
            Assert.IsNotNull(_bossManager.bossUiDocument, "BossManager.bossUiDocument not assigned.");

            _bossRoot = _bossManager.bossUiDocument.rootVisualElement;
            Assert.IsNotNull(_bossRoot, "Boss UIDocument rootVisualElement is null.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PlayerPrefs.SetInt(BossManager.PREF_SKIP_BOSS_ANSWER_DELAY_FOR_TESTS, 0);
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

        private static BlueMCQCard MakeCard(
            string questionText,
            string wrongAnswer,
            string correctAnswer,
            string wrongMessage,
            string correctMessage,
            bool firstAnswerCorrect)
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Boss Q";
            card.image = MakeDummySprite();
            card.statement = "Test statement.";
            card.question = questionText;
            card.answers = new List<MCQAnswer>
            {
                new MCQAnswer
                {
                    answer = firstAnswerCorrect ? correctAnswer : wrongAnswer,
                    messageWhenChosen = firstAnswerCorrect ? correctMessage : wrongMessage,
                    correctAnswer = firstAnswerCorrect
                },
                new MCQAnswer
                {
                    answer = firstAnswerCorrect ? wrongAnswer : correctAnswer,
                    messageWhenChosen = firstAnswerCorrect ? wrongMessage : correctMessage,
                    correctAnswer = !firstAnswerCorrect
                }
            };
            return card;
        }

        private static Boss MakeBossWithThreeQuestions(List<BlueMCQCard> questions, string bossName = "ABE08 Boss")
        {
            var boss = ScriptableObject.CreateInstance<Boss>();
            boss.bossName = bossName;
            boss.questions = questions;
            return boss;
        }

        private void LoadBoss(Boss boss)
        {
            _bossManager.bossPool = new List<Boss> { boss };
        }

        private static void Click(VisualElement ve)
        {
            var e = ClickEvent.GetPooled();
            e.target = ve;
            ve.SendEvent(e);
        }

        private IEnumerator PassIntro()
        {
            yield return null;
            Click(_bossRoot.Q<Button>("intro_begin"));
            yield return null;
        }

        private IEnumerator WaitForFeedbackSlide()
        {
            var feedbackSlide = _bossRoot.Q<VisualElement>("feedback_slide");
            Assert.IsNotNull(feedbackSlide, "feedback_slide missing.");

            int frameGuard = 0;
            while (feedbackSlide.resolvedStyle.display != DisplayStyle.Flex && frameGuard++ < 3600)
                yield return null;

            Assert.AreEqual(DisplayStyle.Flex, feedbackSlide.resolvedStyle.display,
                "feedback_slide should become visible after selecting an answer.");
        }

        private IEnumerator SubmitAnswer(int answerIndex)
        {
            var answersEl = _bossRoot.Q<VisualElement>("answers_element");
            var buttons = answersEl.Query<Button>(className: "answer-option").ToList();
            Assert.Greater(buttons.Count, answerIndex, "Not enough answer buttons.");
            Click(buttons[answerIndex]);
            yield return WaitForFeedbackSlide();
        }

        private IEnumerator ClickContinue()
        {
            Click(_bossRoot.Q<Button>("feedback_continue"));
            yield return null;
        }

        private IEnumerator AnswerAllThree(int[] answerIndexes)
        {
            Assert.AreEqual(3, answerIndexes.Length, "Need 3 answers for the 3 boss questions.");
            yield return PassIntro();
            for (int q = 0; q < 3; q++)
            {
                yield return SubmitAnswer(answerIndexes[q]);
                if (q < 2)
                    yield return ClickContinue();
            }
        }

        // AT1: After battle ends (loss), player can see which question was wrong.
        // All questions are wrong so the first one shown (regardless of shuffle order) always
        // triggers an immediate loss with feedback identifying the wrong question.
        [UnityTest]
        public IEnumerator AT1_AfterBattleEnds_PlayerCanSeeWrongQuestions()
        {
            var questions = new List<BlueMCQCard>
            {
                MakeCard("Q1", "Wrong A1", "Correct A1", "Q1 was incorrect", "Q1 correct", firstAnswerCorrect: false),
                MakeCard("Q2", "Wrong A2", "Correct A2", "Q2 was incorrect", "Q2 correct", firstAnswerCorrect: false),
                MakeCard("Q3", "Wrong A3", "Correct A3", "Q3 was incorrect", "Q3 correct", firstAnswerCorrect: false)
            };

            LoadBoss(MakeBossWithThreeQuestions(questions));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0); // always wrong regardless of question order
            yield return null;

            var result = _bossRoot.Q<Label>("feedback_result");
            var reason = _bossRoot.Q<Label>("feedback_reason");
            Assert.IsNotNull(result, "feedback_result missing.");
            Assert.IsNotNull(reason, "feedback_reason missing.");
            Assert.IsTrue(result.text.Contains("Wins") || result.text.Contains("incorrect"),
                "After battle end, a lose/outcome screen should be shown.");
            Assert.IsTrue(reason.text.Contains("Q1") || reason.text.Contains("Q2") || reason.text.Contains("Q3"),
                "Post-battle feedback should identify which question was wrong.");
        }

        // AT2: For an incorrect answer, the correct answer is shown.
        // All three questions share the same correct answer text ("Correct A1") so the assertion
        // holds regardless of which question the shuffle places first.
        [UnityTest]
        public IEnumerator AT2_IncorrectAnswers_ShowCorrectAnswersInFeedback()
        {
            var questions = new List<BlueMCQCard>
            {
                MakeCard("Q1", "Wrong A1", "Correct A1", "Q1 wrong", "Q1 correct", firstAnswerCorrect: false),
                MakeCard("Q2", "Wrong A2", "Correct A1", "Q2 wrong", "Q2 correct", firstAnswerCorrect: false),
                MakeCard("Q3", "Wrong A3", "Correct A1", "Q3 wrong", "Q3 correct", firstAnswerCorrect: false)
            };

            LoadBoss(MakeBossWithThreeQuestions(questions));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0); // always wrong regardless of question order
            yield return null;

            var reason = _bossRoot.Q<Label>("feedback_reason");
            Assert.IsNotNull(reason, "feedback_reason missing.");
            StringAssert.Contains("Correct answer:", reason.text,
                "Feedback should include the correct-answer label for an incorrect response.");
            StringAssert.Contains("Correct A1", reason.text,
                "Feedback should show the correct answer text for the incorrect response.");
        }

        // AT3: Player can read feedback before returning to board gameplay.
        [UnityTest]
        public IEnumerator AT3_FeedbackVisibleUntilPlayerContinues()
        {
            var questions = new List<BlueMCQCard>
            {
                MakeCard("Q1", "Wrong A1", "Correct A1", "Q1 wrong", "Q1 correct", firstAnswerCorrect: true),
                MakeCard("Q2", "Wrong A2", "Correct A2", "Q2 wrong", "Q2 correct", firstAnswerCorrect: true),
                MakeCard("Q3", "Wrong A3", "Correct A3", "Q3 wrong", "Q3 correct", firstAnswerCorrect: true)
            };

            LoadBoss(MakeBossWithThreeQuestions(questions));
            _bossManager.StartBossFight(() => { });
            yield return AnswerAllThree(new[] { 0, 0, 0 }); // all correct
            yield return ClickContinue(); // See Results -> post-battle feedback

            Assert.AreEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "Feedback should stay visible until the player chooses to continue.");
            var continueButton = _bossRoot.Q<Button>("feedback_continue");
            Assert.IsNotNull(continueButton, "feedback_continue missing.");
            Assert.IsTrue(continueButton.enabledSelf, "Player must be able to continue after reading feedback.");
        }

        // AT4: Feedback appears only after battle ends, not during the three questions.
        [UnityTest]
        public IEnumerator AT4_FeedbackNotShownDuringThreeQuestions()
        {
            var questions = new List<BlueMCQCard>
            {
                MakeCard("Q1", "Wrong A1", "Correct A1", "Q1 wrong", "Q1 correct", firstAnswerCorrect: true),
                MakeCard("Q2", "Wrong A2", "Correct A2", "Q2 wrong", "Q2 correct", firstAnswerCorrect: true),
                MakeCard("Q3", "Wrong A3", "Correct A3", "Q3 wrong", "Q3 correct", firstAnswerCorrect: true)
            };

            LoadBoss(MakeBossWithThreeQuestions(questions));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();

            for (int q = 0; q < 2; q++)
            {
                yield return SubmitAnswer(0);
                yield return null;

                var result = _bossRoot.Q<Label>("feedback_result");
                var continueBtn = _bossRoot.Q<Button>("feedback_continue");
                var title = _bossRoot.Q<Label>("card_title");

                Assert.IsNotNull(result, "feedback_result missing.");
                Assert.IsNotNull(continueBtn, "feedback_continue missing.");
                Assert.IsNotNull(title, "card_title missing.");
                Assert.AreEqual("Next Question", continueBtn.text,
                    "During questions 1 and 2, flow should remain in-question (not post-battle summary).");
                Assert.IsFalse(result.text.Contains("Wins") || result.text.Contains("Defeated") || result.text.Contains("All correct!"),
                    "Post-battle summary must not be shown during the three-question sequence.");

                yield return ClickContinue();
                Assert.IsTrue(title.text.Contains($"{q + 2}/3"),
                    "After continuing, battle should advance to the next question.");
            }
        }

        // AT5: If all three are correct, an "All correct!" screen is shown.
        [UnityTest]
        public IEnumerator AT5_AllThreeCorrect_ShowsAllCorrectScreen()
        {
            var questions = new List<BlueMCQCard>
            {
                MakeCard("Q1", "Wrong A1", "Correct A1", "Q1 wrong", "Q1 correct", firstAnswerCorrect: true),
                MakeCard("Q2", "Wrong A2", "Correct A2", "Q2 wrong", "Q2 correct", firstAnswerCorrect: true),
                MakeCard("Q3", "Wrong A3", "Correct A3", "Q3 wrong", "Q3 correct", firstAnswerCorrect: true)
            };

            LoadBoss(MakeBossWithThreeQuestions(questions, bossName: "AllCorrectBoss"));
            _bossManager.StartBossFight(() => { });
            yield return AnswerAllThree(new[] { 0, 0, 0 }); // all correct
            yield return ClickContinue(); // See Results
            yield return null;

            var result = _bossRoot.Q<Label>("feedback_result");
            Assert.IsNotNull(result, "feedback_result missing.");
            Assert.IsTrue(result.text.Contains("All correct!") || result.text.Contains("Defeated"),
                "All-correct outcome screen should be shown when all three answers are correct. Got: " + result.text);
        }
    }
}
