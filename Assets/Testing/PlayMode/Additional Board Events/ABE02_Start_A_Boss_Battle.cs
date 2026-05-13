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
    /// ABE.02 - Start a Boss Battle
    /// Acceptance tests:
    /// 1. A boss battle is triggered when the player reaches the end of the board.
    /// 2. The boss UI appears and the dice button is disabled during the battle.
    /// 3. Answering all questions correctly reduces pollution by 3.
    /// 4. The first wrong answer increases pollution by 1 and ends the battle immediately.
    /// 5. After the battle ends, the boss UI hides and the game resumes.
    /// 6. The boss battle cannot be skipped or closed.
    /// </summary>
    [Category("Additional Board Events")]
    public class ABE02_Start_A_Boss_Battle
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

        private void SnapToIndex(int idx)
        {
            _gc.player.moving = false;
            _gc.player.boardSquareIndex = idx;
            _gc.player.nextBoardSquareIndex = idx;
            _gc.player.finalBoardSquareIndex = idx;
            _gc.player.gameObject.transform.position = _gc.boardSquares[idx].transform.position;
        }

        private IEnumerator WaitForMovement()
        {
            while (_gc.player.moving)
                yield return null;
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

        private IEnumerator SubmitAnswerByText(string answerText)
        {
            var answersEl = _bossRoot.Q<VisualElement>("answers_element");
            var buttons = answersEl.Query<Button>(className: "answer-option").ToList();
            var target = buttons.FirstOrDefault(b => b.text == answerText);
            Assert.IsNotNull(target, $"Answer button '{answerText}' not found.");
            Click(target);
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
        // ABE.02 Acceptance tests
        // -------------------------------------------------------------------------

        // AC1: A boss battle is triggered when the player reaches the end of the board.
        [UnityTest]
        public IEnumerator BossBattle_TriggersAtEndOfBoard()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: true));

            int last = _gc.boardSquares.Count - 1;
            SnapToIndex(last - 1);
            _gc.MovePlayer(1, triggerLandOn: false, showCountdown: false);
            yield return WaitForMovement();

            Assert.AreEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "Boss UI should appear when the player reaches the last square.");
        }

        // AC2: The boss UI appears and the dice button is disabled during the battle.
        [UnityTest]
        public IEnumerator BossUI_AppearsAndDiceIsDisabled()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: true));

            int last = _gc.boardSquares.Count - 1;
            SnapToIndex(last - 1);
            _gc.MovePlayer(1, triggerLandOn: false, showCountdown: false);
            yield return WaitForMovement();

            Assert.AreEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "Boss UI should be visible during the battle.");

            var dice = _gc.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.IsFalse(dice.enabledSelf,
                "Dice button must be disabled while the boss battle is active.");
        }

        // AC3: Answering all questions correctly reduces pollution by 3.
        [UnityTest]
        public IEnumerator AllCorrectAnswers_ReducesPollutionBy3()
        {
            ScoreManager.Instance.AddScore(-100);
            ScoreManager.Instance.AddScore(5);
            int before = ScoreManager.Instance.GetScore();

            LoadBoss(MakeBoss(firstAnswerCorrect: true));
            _bossManager.StartBossFight(() => { });
            yield return AnswerAllQuestions(answerIndex: 0);
            yield return ClickContinue();

            Assert.AreEqual(before - 3, ScoreManager.Instance.GetScore(),
                "All correct answers should reduce pollution by 3.");
        }

        // AC4: The first wrong answer increases pollution by 1 and ends the battle immediately.
        [UnityTest]
        public IEnumerator FirstWrongAnswer_IncreasesPollutionBy1AndEndsBattle()
        {
            ScoreManager.Instance.AddScore(-100);
            ScoreManager.Instance.AddScore(2);
            int before = ScoreManager.Instance.GetScore();

            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswerByText("A");
            yield return ClickContinue();

            Assert.AreEqual(before + 1, ScoreManager.Instance.GetScore(),
                "First wrong answer should increase pollution by 1 and end the battle.");
        }

        // AC4b: Q1 correct, Q2 wrong — battle ends immediately with pollution +1.
        [UnityTest]
        public IEnumerator Q1CorrectQ2Wrong_LoseImmediatelyPollutionPlus1()
        {
            ScoreManager.Instance.AddScore(-100);
            ScoreManager.Instance.AddScore(3);
            int before = ScoreManager.Instance.GetScore();

            var boss = ScriptableObject.CreateInstance<Boss>();
            boss.bossName = "Mixed Boss";
            boss.questions = new List<BlueMCQCard>
            {
                MakeCard(firstAnswerCorrect: true),
                MakeCard(firstAnswerCorrect: false),
                MakeCard(firstAnswerCorrect: false)
            };
            LoadBoss(boss);
            _bossManager.StartBossFight(() => { });

            yield return PassIntro();
            yield return SubmitAnswerByText("A");
            yield return ClickContinue();
            yield return SubmitAnswerByText("A");
            yield return ClickContinue();

            Assert.AreEqual(before + 1, ScoreManager.Instance.GetScore(),
                "Q1 correct, Q2 wrong: battle ends immediately, pollution +1.");
        }

        // AC5: After the battle ends, the boss UI hides and the game resumes.
        [UnityTest]
        public IEnumerator AfterBattleEnds_BossUIHidesAndGameResumes()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: true));
            bool onCompleteCalled = false;
            _bossManager.StartBossFight(() => { onCompleteCalled = true; });

            yield return AnswerAllQuestions(answerIndex: 0);
            yield return ClickContinue(); // "See Results" -> ShowBossResult
            yield return ClickContinue(); // "See Final Score" -> CompleteBossFight

            Assert.IsTrue(onCompleteCalled,
                "The onComplete callback should fire after the battle ends so the game can resume.");
            Assert.AreNotEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "Boss UI should be hidden after the battle ends.");
        }

        // AC6: The boss battle cannot be skipped: no close or skip button exists in the UI.
        [UnityTest]
        public IEnumerator BossBattle_HasNoSkipOrCloseButton()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: true));
            _bossManager.StartBossFight(() => { });
            yield return null;

            Assert.IsNull(_bossRoot.Q<Button>("close_button"),
                "Boss UI must not have a close button — the battle cannot be abandoned.");
            Assert.IsNull(_bossRoot.Q<Button>("skip_button"),
                "Boss UI must not have a skip button — the battle cannot be skipped.");
        }
    }
}
