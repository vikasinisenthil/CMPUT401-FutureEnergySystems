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
    public class ABE09_More_Difficult_Questions
    {
        private GameController _gc;
        private BlueCardManager _blueCardManager;
        private BossManager _bossManager;
        private VisualElement _bossRoot;

        private const string BOARD_SCENE = "Assets/Scenes/BoardScene.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EnsureGameManagerForTests(Difficulty.Hard);

            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            while (SceneManager.GetActiveScene().name != "BoardScene")
                yield return null;
            yield return null;

            _gc = Object.FindFirstObjectByType<GameController>();
            _blueCardManager = Object.FindFirstObjectByType<BlueCardManager>();
            _bossManager = _gc != null ? _gc.GetComponent<BossManager>() : null;

            Assert.IsNotNull(_gc, "GameController not found in BoardScene.");
            Assert.IsNotNull(_blueCardManager, "BlueCardManager not found in BoardScene.");
            Assert.IsNotNull(_bossManager, "BossManager not found on GameController.");
            Assert.IsNotNull(_bossManager.bossUiDocument, "BossManager.bossUiDocument not assigned.");

            _bossRoot = _bossManager.bossUiDocument.rootVisualElement;
            Assert.IsNotNull(_bossRoot, "Boss UIDocument rootVisualElement is null.");

            EnsureDifficultyAndDeck(Difficulty.Hard);
        }

        private static void EnsureGameManagerForTests(Difficulty difficulty)
        {
            if (GameManager.Instance == null)
            {
                var gmGo = new GameObject("GameManager");
                var gm = gmGo.AddComponent<GameManager>();
                gm.difficulty = difficulty;
                gm.Mode = GameMode.Singleplayer;
                gm.PlayerCount = 1;
                gm.SelectedHeroes = new HeroType[0];
                gm.CurrentPickIndex = 0;
            }
            else
            {
                GameManager.Instance.difficulty = difficulty;
                GameManager.Instance.Mode = GameMode.Singleplayer;
                GameManager.Instance.PlayerCount = 1;
                GameManager.Instance.SelectedHeroes = new HeroType[0];
                GameManager.Instance.CurrentPickIndex = 0;
            }
        }

        private void EnsureDifficultyAndDeck(Difficulty difficulty)
        {
            EnsureGameManagerForTests(difficulty);
            _blueCardManager.SetDifficultyAndBuildDeck(difficulty);
        }

        private static Sprite MakeDummySprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private static void Click(VisualElement ve)
        {
            var e = ClickEvent.GetPooled();
            e.target = ve;
            ve.SendEvent(e);
        }

        private static BlueMCQCard MakeBossQuestion(string questionText, bool firstAnswerCorrect = true)
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Boss Hard Q";
            card.image = MakeDummySprite();
            card.statement = "Hard statement";
            card.question = questionText;
            card.answers = new List<MCQAnswer>
            {
                new MCQAnswer { answer = "A", messageWhenChosen = "A", correctAnswer = firstAnswerCorrect },
                new MCQAnswer { answer = "B", messageWhenChosen = "B", correctAnswer = !firstAnswerCorrect }
            };
            return card;
        }

        private static Boss MakeBossWithQuestions(List<BlueMCQCard> questions, string name = "ABE09 Boss")
        {
            var boss = ScriptableObject.CreateInstance<Boss>();
            boss.bossName = name;
            boss.questions = questions;
            return boss;
        }

        private void LoadBoss(Boss boss)
        {
            _bossManager.bossPool = new List<Boss> { boss };
        }

        private IEnumerator PassBossIntro()
        {
            yield return null;
            Click(_bossRoot.Q<Button>("intro_begin"));
            yield return null;
        }

        private static HashSet<string> HardQuestionSet()
        {
            return Resources.LoadAll<BlueMCQCard>("BlueCards/Hard")
                .Where(c => c != null && !string.IsNullOrEmpty(c.question))
                .Select(c => c.question)
                .ToHashSet();
        }

        private static HashSet<string> MediumQuestionSet()
        {
            return Resources.LoadAll<BlueMCQCard>("BlueCards/Medium")
                .Where(c => c != null && !string.IsNullOrEmpty(c.question))
                .Select(c => c.question)
                .ToHashSet();
        }

        private BlueMCQCard ForceDeckToSingleMcq(string resourcesPath, string emptyMessage)
        {
            var mcqs = Resources.LoadAll<BlueMCQCard>(resourcesPath)
                .Where(c => c != null && !string.IsNullOrEmpty(c.question))
                .ToList();

            Assert.Greater(mcqs.Count, 0, emptyMessage);
            var chosen = mcqs[0];
            _blueCardManager.deck = new List<BlueCard> { chosen };
            return chosen;
        }

        private BlueMCQCard ForceDeckToSingleHardMcq() =>
            ForceDeckToSingleMcq("BlueCards/Hard", "Hard MCQ set must contain at least one question card.");

        private BlueMCQCard ForceDeckToSingleMediumMcq() =>
            ForceDeckToSingleMcq("BlueCards/Medium", "Medium MCQ set must contain at least one question card.");

        // AT1: Hard difficulty pulls questions from the hard/difficult question set.
        [UnityTest]
        public IEnumerator AT1_HardDifficulty_UsesHardQuestionSet()
        {
            EnsureDifficultyAndDeck(Difficulty.Hard);

            var hardCards = Resources.LoadAll<BlueCard>("BlueCards/Hard");
            Assert.Greater(hardCards.Length, 0, "Resources/BlueCards/Hard should contain cards.");
            Assert.AreEqual(Difficulty.Hard, GameManager.Instance.difficulty, "Game difficulty should be Hard.");
            Assert.AreEqual(hardCards.Length, _blueCardManager.deck.Count,
                "Hard deck size should match Resources/BlueCards/Hard.");
            Assert.IsTrue(_blueCardManager.deck.All(c => hardCards.Contains(c)),
                "All runtime blue cards should come from the hard set when difficulty is Hard.");
            yield return null;
        }

        // AT2: Hard difficulty applies to blue-card questions and, if applicable, boss-battle questions.
        [UnityTest]
        public IEnumerator AT2_HardDifficulty_AppliesToBlueCards_AndBossIfApplicable()
        {
            EnsureDifficultyAndDeck(Difficulty.Hard);
            var hardQuestions = HardQuestionSet();
            Assert.Greater(hardQuestions.Count, 0, "Hard MCQ question set must not be empty.");

            // Blue-card side: draw one question and verify it's from hard question set.
            var forcedBlueCard = ForceDeckToSingleHardMcq();
            _gc.LandedOnBlueSquare();
            yield return null;
            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var blueQuestion = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(blueQuestion, "Blue card question label missing.");
            Assert.AreEqual(forcedBlueCard.question, blueQuestion.text,
                "Blue-card question should come from the hard question set when Hard is selected.");

            // Boss side (if applicable): load a boss whose questions are all hard-set questions.
            // Using the same question for all three slots ensures the assertion holds regardless of
            // which question the shuffle places first.
            var bossHardQuestion = hardQuestions.First();
            var boss = MakeBossWithQuestions(new List<BlueMCQCard>
            {
                MakeBossQuestion(bossHardQuestion, firstAnswerCorrect: true),
                MakeBossQuestion(bossHardQuestion, firstAnswerCorrect: true),
                MakeBossQuestion(bossHardQuestion, firstAnswerCorrect: true)
            });
            LoadBoss(boss);
            _bossManager.StartBossFight(() => { });
            yield return PassBossIntro();

            var bossQuestion = _bossRoot.Q<Label>("card_question");
            Assert.IsNotNull(bossQuestion, "Boss question label missing.");
            Assert.AreEqual(bossHardQuestion, bossQuestion.text,
                "When boss hard questions are provided, boss battle should show that hard question set.");
        }

        // AT3: Difficulty does not change mid-game unless player changes settings.
        [UnityTest]
        public IEnumerator AT3_DifficultyDoesNotChangeMidGame()
        {
            EnsureDifficultyAndDeck(Difficulty.Hard);
            var hardCards = Resources.LoadAll<BlueCard>("BlueCards/Hard");
            var hardSet = new HashSet<BlueCard>(hardCards);

            var first = _blueCardManager.DrawCard();
            var second = _blueCardManager.DrawCard();

            Assert.AreEqual(Difficulty.Hard, GameManager.Instance.difficulty,
                "Difficulty should remain Hard through normal gameplay flow.");
            Assert.IsTrue(first != null && hardSet.Contains(first), "First drawn card should remain from hard deck.");
            Assert.IsTrue(second != null && hardSet.Contains(second), "Second drawn card should remain from hard deck.");
            yield return null;
        }

        // AT4: Current Hard difficulty is visible somewhere on screen.
        [UnityTest]
        public IEnumerator AT4_HardDifficulty_IsVisibleOnScreen()
        {
            EnsureDifficultyAndDeck(Difficulty.Hard);
            var forcedCard = ForceDeckToSingleHardMcq();

            _gc.LandedOnBlueSquare();
            yield return null;

            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var questionLabel = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(questionLabel, "card_question label missing.");
            Assert.AreEqual(forcedCard.question, questionLabel.text,
                "Hard mode should be visibly reflected on-screen through a hard question being shown.");
        }

        // AT5: At least one hard question appears during a typical game when Hard is selected.
        [UnityTest]
        public IEnumerator AT5_AtLeastOneHardQuestion_ShownWhenHardSelected()
        {
            EnsureDifficultyAndDeck(Difficulty.Hard);
            var forcedCard = ForceDeckToSingleHardMcq();

            _gc.LandedOnBlueSquare();
            yield return null;

            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var questionLabel = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(questionLabel, "card_question label missing.");
            Assert.AreEqual(forcedCard.question, questionLabel.text,
                "A hard question should be shown when playing with Hard selected.");
        }

        // AT6: Medium difficulty pulls questions from the medium question set.
        [UnityTest]
        public IEnumerator AT6_MediumDifficulty_UsesMediumQuestionSet()
        {
            EnsureDifficultyAndDeck(Difficulty.Medium);

            var mediumCards = Resources.LoadAll<BlueCard>("BlueCards/Medium");
            Assert.Greater(mediumCards.Length, 0, "Resources/BlueCards/Medium should contain cards.");
            Assert.AreEqual(Difficulty.Medium, GameManager.Instance.difficulty, "Game difficulty should be Medium.");
            Assert.AreEqual(mediumCards.Length, _blueCardManager.deck.Count,
                "Medium deck size should match Resources/BlueCards/Medium.");
            Assert.IsTrue(_blueCardManager.deck.All(c => mediumCards.Contains(c)),
                "All runtime blue cards should come from the medium set when difficulty is Medium.");
            yield return null;
        }

        // AT7: Medium difficulty applies to blue-card questions and, if applicable, boss-battle questions.
        [UnityTest]
        public IEnumerator AT7_MediumDifficulty_AppliesToBlueCards_AndBossIfApplicable()
        {
            EnsureDifficultyAndDeck(Difficulty.Medium);
            var mediumQuestions = MediumQuestionSet();
            Assert.Greater(mediumQuestions.Count, 0, "Medium MCQ question set must not be empty.");

            var forcedBlueCard = ForceDeckToSingleMediumMcq();
            _gc.LandedOnBlueSquare();
            yield return null;
            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var blueQuestion = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(blueQuestion, "Blue card question label missing.");
            Assert.AreEqual(forcedBlueCard.question, blueQuestion.text,
                "Blue-card question should come from the medium question set when Medium is selected.");

            // Using the same question for all three slots ensures the assertion holds regardless of
            // which question the shuffle places first.
            var bossMediumQuestion = mediumQuestions.First();
            var boss = MakeBossWithQuestions(new List<BlueMCQCard>
            {
                MakeBossQuestion(bossMediumQuestion, firstAnswerCorrect: true),
                MakeBossQuestion(bossMediumQuestion, firstAnswerCorrect: true),
                MakeBossQuestion(bossMediumQuestion, firstAnswerCorrect: true)
            });
            LoadBoss(boss);
            _bossManager.StartBossFight(() => { });
            yield return PassBossIntro();

            var bossQuestion = _bossRoot.Q<Label>("card_question");
            Assert.IsNotNull(bossQuestion, "Boss question label missing.");
            Assert.AreEqual(bossMediumQuestion, bossQuestion.text,
                "When boss medium questions are provided, boss battle should show that medium question set.");
        }

        // AT8: Difficulty does not change mid-game unless player changes settings.
        [UnityTest]
        public IEnumerator AT8_MediumDifficulty_DoesNotChangeMidGame()
        {
            EnsureDifficultyAndDeck(Difficulty.Medium);
            var mediumCards = Resources.LoadAll<BlueCard>("BlueCards/Medium");
            var mediumSet = new HashSet<BlueCard>(mediumCards);

            var first = _blueCardManager.DrawCard();
            var second = _blueCardManager.DrawCard();

            Assert.AreEqual(Difficulty.Medium, GameManager.Instance.difficulty,
                "Difficulty should remain Medium through normal gameplay flow.");
            Assert.IsTrue(first != null && mediumSet.Contains(first), "First drawn card should remain from medium deck.");
            Assert.IsTrue(second != null && mediumSet.Contains(second), "Second drawn card should remain from medium deck.");
            yield return null;
        }

        // AT9: Current Medium difficulty is visible somewhere on screen.
        [UnityTest]
        public IEnumerator AT9_MediumDifficulty_IsVisibleOnScreen()
        {
            EnsureDifficultyAndDeck(Difficulty.Medium);
            var forcedCard = ForceDeckToSingleMediumMcq();

            _gc.LandedOnBlueSquare();
            yield return null;

            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var questionLabel = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(questionLabel, "card_question label missing.");
            Assert.AreEqual(forcedCard.question, questionLabel.text,
                "Medium mode should be visibly reflected on-screen through a medium question being shown.");
        }

        // AT10: At least one medium question appears during a typical game when Medium is selected.
        [UnityTest]
        public IEnumerator AT10_AtLeastOneMediumQuestion_ShownWhenMediumSelected()
        {
            EnsureDifficultyAndDeck(Difficulty.Medium);
            var forcedCard = ForceDeckToSingleMediumMcq();

            _gc.LandedOnBlueSquare();
            yield return null;

            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var questionLabel = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(questionLabel, "card_question label missing.");
            Assert.AreEqual(forcedCard.question, questionLabel.text,
                "A medium question should be shown when playing with Medium selected.");
        }
    }
}
