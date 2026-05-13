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
    /// <summary>
    /// ABE.05 - Fight a Boss
    /// Acceptance tests:
    /// 1. The player is shown three questions, one after another.
    /// 2. Boss questions come from the boss's own question set, not the blue card deck.
    /// 3. After the third answer is submitted, the battle result screen is shown.
    /// </summary>
    [Category("Additional Board Events")]
    public class ABE05_Fight_A_Boss
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

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static Sprite MakeDummySprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private static BlueMCQCard MakeCard(bool firstAnswerCorrect)
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Boss Q";
            card.image = MakeDummySprite();
            card.statement = "Test statement.";
            card.question = "Test question?";
            card.answers = new List<MCQAnswer>
            {
                new MCQAnswer { answer = "A", messageWhenChosen = "Msg A", correctAnswer = firstAnswerCorrect },
                new MCQAnswer { answer = "B", messageWhenChosen = "Msg B", correctAnswer = !firstAnswerCorrect }
            };
            return card;
        }

        private static Boss MakeBoss(bool firstAnswerCorrect, string bossName = "Test Boss", int questionCount = 3)
        {
            var boss = ScriptableObject.CreateInstance<Boss>();
            boss.bossName = bossName;
            boss.questions = new List<BlueMCQCard>();
            for (int i = 0; i < questionCount; i++)
                boss.questions.Add(MakeCard(firstAnswerCorrect));
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

        private IEnumerator AnswerAllQuestions(int answerIndex)
        {
            yield return PassIntro();
            for (int q = 0; q < 3; q++)
            {
                yield return SubmitAnswer(answerIndex);
                if (q < 2) yield return ClickContinue();
            }
        }

        // -------------------------------------------------------------------------
        // ABE.05 Acceptance tests
        // -------------------------------------------------------------------------

        // AC1: The player is shown three questions, one after another.
        [UnityTest]
        public IEnumerator ThreeQuestions_ShownSequentially()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: true));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();

            Assert.IsTrue(_bossRoot.Q<Label>("card_title").text.Contains("1/3"),
                "First question should be labelled 1/3.");
            yield return SubmitAnswer(0);
            yield return ClickContinue();

            Assert.IsTrue(_bossRoot.Q<Label>("card_title").text.Contains("2/3"),
                "Second question should be labelled 2/3.");
            yield return SubmitAnswer(0);
            yield return ClickContinue();

            Assert.IsTrue(_bossRoot.Q<Label>("card_title").text.Contains("3/3"),
                "Third question should be labelled 3/3.");
        }

        // AC2: Boss questions come from the boss's own question set, not the blue card deck.
        [UnityTest]
        public IEnumerator BossQuestions_AreFromBossDeck()
        {
            Boss boss = MakeBoss(firstAnswerCorrect: true, bossName: "Factory");
            string expectedQuestion = boss.questions[0].question;
            LoadBoss(boss);
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();

            Assert.AreEqual(expectedQuestion, _bossRoot.Q<Label>("card_question").text,
                "Boss should ask questions from its own assigned question set, not the shared blue card deck.");
        }

        // AC3: After the third answer is submitted, the battle result screen is shown.
        [UnityTest]
        public IEnumerator AfterThirdAnswer_ResultScreenIsShown()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: true));
            _bossManager.StartBossFight(() => { });
            yield return AnswerAllQuestions(answerIndex: 0);
            yield return ClickContinue(); // "See Results" -> ShowBossResult

            var resultLabel = _bossRoot.Q<Label>("feedback_result");
            var continueBtn = _bossRoot.Q<Button>("feedback_continue");

            Assert.IsTrue(
                resultLabel.text.Contains("Defeated") || resultLabel.text.Contains("Wins"),
                $"Result screen should show the battle outcome. Got: '{resultLabel.text}'");
            Assert.AreEqual("See Final Score", continueBtn.text,
                "Continue button should read 'See Final Score' on the result screen.");
        }
    }
}
