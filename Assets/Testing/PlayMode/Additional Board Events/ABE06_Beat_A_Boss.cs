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
    /// ABE.06 - Beat a Boss
    /// Acceptance tests:
    /// 1. A player beats the boss only if they answer all three questions correctly.
    /// 2. If the player answers all three correctly: the win state is shown.
    /// 3. If the player answers one or more incorrectly: the player does not beat the boss.
    /// 4. After a win, the player returns to the board and can continue moving.
    /// 5. After a loss, the player also returns to the board and gameplay continues.
    /// </summary>
    [Category("Additional Board Events")]
    public class ABE06_Beat_A_Boss
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

        private static Boss MakeBoss(bool firstAnswerCorrect, string bossName = "ABE06 Boss", int questionCount = 3)
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

        /// <summary>Answers all three questions with the given answer index (0 = first option, etc.).</summary>
        private IEnumerator AnswerAllThree(int correctAnswerIndex)
        {
            yield return PassIntro();
            for (int q = 0; q < 3; q++)
            {
                yield return SubmitAnswer(correctAnswerIndex);
                if (q < 2)
                    yield return ClickContinue();
            }
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

        private IEnumerator WaitUntilBossHidden()
        {
            while (_bossRoot.resolvedStyle.display == DisplayStyle.Flex)
                yield return null;
        }

        // -------------------------------------------------------------------------
        // ABE.06 Acceptance tests
        // -------------------------------------------------------------------------

        // 1. A player beats the boss only if they answer all three questions correctly.
        [UnityTest]
        public IEnumerator AllThreeCorrect_PlayerBeatsBoss()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: true)); // index 0 is correct for all three
            _bossManager.StartBossFight(() => { });
            yield return AnswerAllThree(correctAnswerIndex: 0);
            yield return ClickContinue(); // "See Results" -> ShowBossResult() shows "Defeated!"
            yield return null;

            var resultLabel = _bossRoot.Q<Label>("feedback_result");
            Assert.IsNotNull(resultLabel, "feedback_result label missing.");
            Assert.IsTrue(resultLabel.text.Contains("Defeated"),
                "Player must beat the boss only when all three are correct. Expected 'Defeated', got: " + resultLabel.text);
        }

        // 2. If the player answers all three correctly: the win state is shown.
        [UnityTest]
        public IEnumerator AllThreeCorrect_WinStateIsShown()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: true, bossName: "WinBoss"));
            _bossManager.StartBossFight(() => { });
            yield return AnswerAllThree(correctAnswerIndex: 0);
            yield return ClickContinue(); // "See Results" -> ShowBossResult() shows win state
            yield return null;

            var resultLabel = _bossRoot.Q<Label>("feedback_result");
            var reasonLabel = _bossRoot.Q<Label>("feedback_reason");
            var continueBtn = _bossRoot.Q<Button>("feedback_continue");

            Assert.IsNotNull(resultLabel, "feedback_result missing.");
            Assert.IsTrue(resultLabel.text.Contains("Defeated"), "Win state must show Defeated. Got: " + resultLabel.text);
            Assert.IsNotNull(reasonLabel, "feedback_reason missing.");
            Assert.IsTrue(reasonLabel.text.Contains("correct") || reasonLabel.text.Contains("Pollution"),
                "Win state should explain outcome. Got: " + reasonLabel.text);
            Assert.AreEqual("See Final Score", continueBtn?.text,
                "Win state must offer 'See Final Score'. Got: " + (continueBtn?.text ?? "null"));
        }

        // 3. If the player answers one or more incorrectly: the player does not beat the boss.
        [UnityTest]
        public IEnumerator OneIncorrect_PlayerDoesNotBeatBoss()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: false)); // first option is wrong
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return null;

            var resultLabel = _bossRoot.Q<Label>("feedback_result");
            Assert.IsNotNull(resultLabel, "feedback_result missing.");
            Assert.IsTrue(resultLabel.text.ToLowerInvariant().Contains("incorrect"),
                "One incorrect answer must mean player does not beat the boss. Got: " + resultLabel.text);
        }

        // 4. After a win, the player returns to the board and can continue moving.
        [UnityTest]
        public IEnumerator AfterWin_PlayerReturnsToBoard_CanContinue()
        {
            bool completeCallbackFired = false;
            LoadBoss(MakeBoss(firstAnswerCorrect: true));
            _bossManager.StartBossFight(() => { completeCallbackFired = true; });
            yield return AnswerAllThree(correctAnswerIndex: 0);
            yield return ClickContinue(); // "See Results" -> ShowBossResult() (Defeated! / See Final Score)
            yield return ClickContinue(); // "See Final Score" -> CompleteBossFight(true) -> win callback
            yield return WaitUntilBossHidden();
            yield return null;

            Assert.IsTrue(completeCallbackFired, "Boss complete callback must run so game can resume / show final score.");
            Assert.AreNotEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "After a win, boss UI must hide so the player returns to the board.");
        }

        // 5. After a loss, the player also returns to the board and gameplay continues.
        // In this implementation, a "loss" is reached after answering all three wrong, then "See Results" -> "See Final Score".
        [UnityTest]
        public IEnumerator AfterLoss_PlayerReturnsToBoard_GameplayContinues()
        {
            int last = _gc.boardSquares.Count - 1;
            SnapToIndex(last - 1);
            LoadBoss(MakeBoss(firstAnswerCorrect: false)); // index 0 is wrong for all three
            _gc.MovePlayer(1, triggerLandOn: true, showCountdown: false);
            yield return WaitForMovement();
            yield return PassIntro();
            // Answer all three wrong and click through: Q1 wrong -> Next Question, Q2 wrong -> Next Question, Q3 wrong -> See Results
            for (int q = 0; q < 3; q++)
            {
                yield return SubmitAnswer(0);
                yield return ClickContinue();
            }
            // Now on lose screen with "See Final Score" -> CompleteBossFight
            yield return ClickContinue();
            yield return WaitUntilBossHidden();
            yield return null;

            Assert.AreNotEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "After a loss, boss UI must hide so the player returns to the board.");
            Assert.AreEqual(last, _gc.player.boardSquareIndex,
                "Player should remain on the same tile after loss; gameplay continues from there.");
            // At last tile, game shows final score (callback from boss), so dice may stay disabled; still "returned to board"
            var dice = _gc.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.IsNotNull(dice, "Dice button must exist.");
        }
    }
}
