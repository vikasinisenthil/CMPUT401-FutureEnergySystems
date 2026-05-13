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
    public class ABE10_More_Easier_Questions
    {
        private GameController _gc;
        private BlueCardManager _blueCardManager;
        private BossManager _bossManager;
        private VisualElement _bossRoot;

        private const string BOARD_SCENE = "Assets/Scenes/BoardScene.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EnsureGameManagerForTests();

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

            EnsureEasyDifficultyAndDeck();
        }

        private static void EnsureGameManagerForTests()
        {
            if (GameManager.Instance == null)
            {
                var gmGo = new GameObject("GameManager");
                var gm = gmGo.AddComponent<GameManager>();
                gm.difficulty = Difficulty.Easy;
                gm.Mode = GameMode.Singleplayer;
                gm.PlayerCount = 1;
                gm.SelectedHeroes = new HeroType[0];
                gm.CurrentPickIndex = 0;
            }
            else
            {
                GameManager.Instance.difficulty = Difficulty.Easy;
                GameManager.Instance.Mode = GameMode.Singleplayer;
                GameManager.Instance.PlayerCount = 1;
                GameManager.Instance.SelectedHeroes = new HeroType[0];
                GameManager.Instance.CurrentPickIndex = 0;
            }
        }

        private void EnsureEasyDifficultyAndDeck()
        {
            EnsureGameManagerForTests();
            _blueCardManager.SetDifficultyAndBuildDeck(Difficulty.Easy);
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
            card.cardName = "Boss Easy Q";
            card.image = MakeDummySprite();
            card.statement = "Easy statement";
            card.question = questionText;
            card.answers = new List<MCQAnswer>
            {
                new MCQAnswer { answer = "A", messageWhenChosen = "A", correctAnswer = firstAnswerCorrect },
                new MCQAnswer { answer = "B", messageWhenChosen = "B", correctAnswer = !firstAnswerCorrect }
            };
            return card;
        }

        private static Boss MakeBossWithQuestions(List<BlueMCQCard> questions, string name = "ABE10 Boss")
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

        private static HashSet<string> EasyQuestionSet()
        {
            return Resources.LoadAll<BlueMCQCard>("BlueCards/Easy")
                .Where(c => c != null && !string.IsNullOrEmpty(c.question))
                .Select(c => c.question)
                .ToHashSet();
        }

        private BlueMCQCard ForceDeckToSingleEasyMcq()
        {
            var easyMcqs = Resources.LoadAll<BlueMCQCard>("BlueCards/Easy")
                .Where(c => c != null && !string.IsNullOrEmpty(c.question))
                .ToList();

            Assert.Greater(easyMcqs.Count, 0, "Easy MCQ set must contain at least one question card.");
            var chosen = easyMcqs[0];
            _blueCardManager.deck = new List<BlueCard> { chosen };
            return chosen;
        }

        // AT1: Easy difficulty pulls questions from the easy question set.
        [UnityTest]
        public IEnumerator AT1_EasyDifficulty_UsesEasyQuestionSet()
        {
            EnsureEasyDifficultyAndDeck();

            var easyCards = Resources.LoadAll<BlueCard>("BlueCards/Easy");
            Assert.Greater(easyCards.Length, 0, "Resources/BlueCards/Easy should contain cards.");
            Assert.AreEqual(Difficulty.Easy, GameManager.Instance.difficulty, "Game difficulty should be Easy.");
            Assert.AreEqual(easyCards.Length, _blueCardManager.deck.Count,
                "Easy deck size should match Resources/BlueCards/Easy.");
            Assert.IsTrue(_blueCardManager.deck.All(c => easyCards.Contains(c)),
                "All runtime blue cards should come from the easy set when difficulty is Easy.");
            yield return null;
        }

        // AT2: Easy difficulty applies to blue-card questions and, if applicable, boss-battle questions.
        [UnityTest]
        public IEnumerator AT2_EasyDifficulty_AppliesToBlueCards_AndBossIfApplicable()
        {
            EnsureEasyDifficultyAndDeck();
            var easyQuestions = EasyQuestionSet();
            Assert.Greater(easyQuestions.Count, 0, "Easy MCQ question set must not be empty.");

            var forcedBlueCard = ForceDeckToSingleEasyMcq();
            _gc.LandedOnBlueSquare();
            yield return null;
            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var blueQuestion = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(blueQuestion, "Blue card question label missing.");
            Assert.AreEqual(forcedBlueCard.question, blueQuestion.text,
                "Blue-card question should come from the easy question set when Easy is selected.");

            var bossQuestionTexts = new HashSet<string> { "Easy boss Q1", "Easy boss Q2", "Easy boss Q3" };
            var boss = MakeBossWithQuestions(new List<BlueMCQCard>
            {
                MakeBossQuestion("Easy boss Q1", firstAnswerCorrect: true),
                MakeBossQuestion("Easy boss Q2", firstAnswerCorrect: true),
                MakeBossQuestion("Easy boss Q3", firstAnswerCorrect: true)
            });
            LoadBoss(boss);
            _bossManager.StartBossFight(() => { });
            yield return PassBossIntro();

            var bossQuestion = _bossRoot.Q<Label>("card_question");
            Assert.IsNotNull(bossQuestion, "Boss question label missing.");
            Assert.IsTrue(bossQuestionTexts.Contains(bossQuestion.text),
                "Boss battle should display one of the questions from the provided easy question pool.");
        }

        // AT3: Difficulty does not change mid-game unless player changes settings.
        [UnityTest]
        public IEnumerator AT3_DifficultyDoesNotChangeMidGame()
        {
            EnsureEasyDifficultyAndDeck();
            var easyCards = Resources.LoadAll<BlueCard>("BlueCards/Easy");
            var easySet = new HashSet<BlueCard>(easyCards);

            var first = _blueCardManager.DrawCard();
            var second = _blueCardManager.DrawCard();

            Assert.AreEqual(Difficulty.Easy, GameManager.Instance.difficulty,
                "Difficulty should remain Easy through normal gameplay flow.");
            Assert.IsTrue(first != null && easySet.Contains(first), "First drawn card should remain from easy deck.");
            Assert.IsTrue(second != null && easySet.Contains(second), "Second drawn card should remain from easy deck.");
            yield return null;
        }

        // AT4: Current Easy difficulty is visible somewhere on screen.
        [UnityTest]
        public IEnumerator AT4_EasyDifficulty_IsVisibleOnScreen()
        {
            EnsureEasyDifficultyAndDeck();
            var forcedCard = ForceDeckToSingleEasyMcq();

            _gc.LandedOnBlueSquare();
            yield return null;

            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var questionLabel = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(questionLabel, "card_question label missing.");
            Assert.AreEqual(forcedCard.question, questionLabel.text,
                "Easy mode should be visibly reflected on-screen through an easy question being shown.");
        }

        // AT5: At least one easy question appears during a typical game when Easy is selected.
        [UnityTest]
        public IEnumerator AT5_AtLeastOneEasyQuestion_ShownWhenEasySelected()
        {
            EnsureEasyDifficultyAndDeck();
            var forcedCard = ForceDeckToSingleEasyMcq();

            _gc.LandedOnBlueSquare();
            yield return null;

            var blueRoot = _gc.blueCardUiDocument.rootVisualElement;
            var questionLabel = blueRoot.Q<Label>("card_question");
            Assert.IsNotNull(questionLabel, "card_question label missing.");
            Assert.AreEqual(forcedCard.question, questionLabel.text,
                "An easy question should be shown when playing with Easy selected.");
        }
    }
}
