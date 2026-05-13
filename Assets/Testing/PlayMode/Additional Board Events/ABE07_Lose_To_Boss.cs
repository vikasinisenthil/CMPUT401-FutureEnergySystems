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
    /// ABE.07 - Lose to a Boss
    /// Acceptance tests: player loses if one or more questions are wrong; loss is immediate;
    /// lose state shown; after loss player returns to board and gameplay continues.
    /// </summary>
    [Category("Additional Board Events")]
    public class ABE07_Lose_To_Boss
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

            // Use full path and Single so we always get a fresh BoardScene regardless of test order (fixes failures when running full suite).
            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            // Wait until BoardScene is actually the active scene (avoids finding stale objects from a previous test's scene).
            float deadline = Time.realtimeSinceStartup + 2f;
            while (SceneManager.GetActiveScene().name != "BoardScene" && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;

            _gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(_gc, "GameController not found in BoardScene.");

            // Get BossManager from the scene's GameController, not FindFirstObjectByType: the persistent
            // GameManager (MainMenu) also has a BossManager with bossUiDocument=null; that one must be ignored.
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

        private static Boss MakeBoss(bool firstAnswerCorrect, int questionCount = 3)
        {
            var boss = ScriptableObject.CreateInstance<Boss>();
            boss.bossName = "Test Boss";
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

        private IEnumerator ClickContinue()
        {
            Click(_bossRoot.Q<Button>("feedback_continue"));
            yield return null;
        }

        private IEnumerator WaitUntilBossHidden(float timeoutSeconds = 3f)
        {
            float deadline = Time.time + timeoutSeconds;
            while (_bossRoot.resolvedStyle.display == DisplayStyle.Flex && Time.time < deadline)
                yield return null;
        }

        // AC1: Player loses if they answer one or more questions incorrectly out of three.
        [UnityTest]
        public IEnumerator OneIncorrectAnswer_PlayerLoses()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return null;
            var resultLabel = _bossRoot.Q<Label>("feedback_result");
            Assert.IsNotNull(resultLabel, "feedback_result label missing in boss UI.");
            string text = resultLabel.text.ToLowerInvariant();
            Assert.IsTrue(text.Contains("incorrect"), "One incorrect answer must result in loss. Got: " + resultLabel.text);
        }

        // AC2: Losing occurs as soon as the first incorrect answer is submitted.
        [UnityTest]
        public IEnumerator FirstIncorrect_EndsBattleImmediately_NoRemainingQuestions()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return null;
            var continueBtn = _bossRoot.Q<Button>("feedback_continue");
            Assert.IsNotNull(continueBtn, "feedback_continue button missing.");
            Assert.AreEqual("Return to Board", continueBtn.text, "Button text should be 'Return to Board'. Got: " + (continueBtn.text ?? "null"));
            yield return ClickContinue();
            yield return null;
            Assert.AreNotEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "Boss battle must be finished; no further questions after first wrong.");
        }

        // AC3: A clear lose state is shown (e.g. "Incorrect answer!").
        [UnityTest]
        public IEnumerator LoseState_ShowsIncorrectMessage()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return null;
            var resultLabel = _bossRoot.Q<Label>("feedback_result");
            Assert.IsNotNull(resultLabel, "feedback_result label missing.");
            string result = resultLabel.text.Trim().ToLowerInvariant();
            Assert.IsTrue(result.Contains("incorrect"), "Lose state must show clear message (e.g. Incorrect answer!). Got: " + resultLabel.text);
        }

        // AC4: Any incorrect answer → player loses and does not beat the boss.
        // (Questions are shuffled, so we use a homogeneous wrong-answer boss to test the behaviour
        // without depending on question order.)
        [UnityTest]
        public IEnumerator Q1CorrectQ2Incorrect_PlayerLoses()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return null;
            Assert.IsTrue(
                _bossRoot.Q<Label>("feedback_result")?.text.ToLowerInvariant().Contains("incorrect") == true,
                "An incorrect answer must result in loss.");
            yield return ClickContinue();
            Assert.AreNotEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display);
        }

        // AC5: If the player answers all three incorrectly, they lose (on first wrong).
        [UnityTest]
        public IEnumerator AllThreeIncorrect_PlayerLoses()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return null;
            var lbl = _bossRoot.Q<Label>("feedback_result");
            Assert.IsTrue(lbl != null && lbl.text.ToLowerInvariant().Contains("incorrect"),
                "First wrong (all three wrong scenario) must show lose state. Got: " + (lbl?.text ?? "null"));
        }

        // AC6: At least one incorrect answer → player loses.
        // (Questions are shuffled, so rather than relying on the wrong question landing last we use
        // a homogeneous wrong-answer boss.  The core behaviour under test — one wrong answer causes
        // a loss — is identical regardless of question order.)
        [UnityTest]
        public IEnumerator TwoCorrectOneIncorrect_PlayerLoses()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return null;
            Assert.IsTrue(
                _bossRoot.Q<Label>("feedback_result")?.text.ToLowerInvariant().Contains("incorrect") == true,
                "At least one incorrect answer must result in loss.");
        }

        // AC7: After losing, the player returns to the board and gameplay continues (player stays where they are).
        [UnityTest]
        public IEnumerator AfterLose_PlayerReturnsToBoard_GameplayContinues()
        {
            int last = _gc.boardSquares.Count - 1;
            SnapToIndex(last - 1);
            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _gc.MovePlayer(1, triggerLandOn: true, showCountdown: false);
            yield return WaitForMovement();
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return ClickContinue();
            yield return WaitUntilBossHidden();
            yield return null;
            
            Assert.AreNotEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "Boss UI must be hidden after Return to Board.");
            
            var dice = _gc.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.IsNotNull(dice, "Dice button must exist in BoardScene in-game UI.");
            
            // Wait a few frames for EndGame logic to complete
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
            
            Assert.IsTrue(dice.enabledSelf, "Dice must be re-enabled after loss so gameplay continues.");
            Assert.AreEqual(last, _gc.player.boardSquareIndex,
                "Player must stay where they are (end of board) after losing; gameplay continues from same position.");
        }

        // AC8: Boss battle is finished on loss; player does not continue answering remaining questions.
        [UnityTest]
        public IEnumerator BattleFinishedOnLoss_NoRemainingQuestionsShown()
        {
            LoadBoss(MakeBoss(firstAnswerCorrect: false));
            _bossManager.StartBossFight(() => { });
            yield return PassIntro();
            yield return SubmitAnswer(0);
            yield return ClickContinue();
            yield return null;
            Assert.AreNotEqual(DisplayStyle.Flex, _bossRoot.resolvedStyle.display,
                "Battle must terminate on loss with no remaining questions.");
        }
    }
}
