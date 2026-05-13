using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Accessibility {
    [Category("Accessibility")]
    public class ACC02_Text_To_Speech
    {
        BlueCardManager blueCardManager;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("Assets/Scenes/MainMenu.unity", LoadSceneMode.Single);

            yield return null;

            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);

            yield return null;

            blueCardManager = Object.FindFirstObjectByType<BlueCardManager>();

            yield return null;
        }

        private void ForceDeckToSingleCard(BlueCard card)
        {
            blueCardManager.deck = new List<BlueCard> { card };
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

        private static BlueFactCard MakeTestFactCard(string name = "TEST_NAME", string fact = "TEST_FACT")
        {
            var card = ScriptableObject.CreateInstance<BlueFactCard>();
            card.cardName = name;
            card.fact = fact;
            card.image = MakeDummySprite();
            return card;
        }

        [UnityTest]
        public IEnumerator SpeakerButtonAvailableOnMCQ()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            ForceDeckToSingleCard(MakeTestMCQCard(firstCorrect: true));

            gc.LandedOnBlueSquare();

            var root = gc.blueCardUiDocument.rootVisualElement;

            var speakerButton = root.Q<Button>("speaker_button");

            Assert.True(speakerButton.enabledInHierarchy);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SpeakerButtonAvailableOnFact()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            ForceDeckToSingleCard(MakeTestFactCard());

            gc.LandedOnBlueSquare();

            var root = gc.blueCardUiDocument.rootVisualElement;

            var speakerButton = root.Q<Button>("speaker_button");

            Assert.True(speakerButton.enabledInHierarchy);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SpeakerButtonPlaysTTS()
        {
            var tts = GameObject.Find("TTS").GetComponent<TTS>();

            tts.Speak("TEST");

            Assert.IsTrue(tts.Speaking());

            yield return null;
        }

        [UnityTest]
        public IEnumerator SecondTTSUseStopsTTS()
        {
            var tts = GameObject.Find("TTS").GetComponent<TTS>();
            tts.Speak("TEST");

            tts.Stop();

            Assert.IsFalse(tts.Speaking());

            yield return null;
        }

        private static void ClickVisualElement(VisualElement ve)
        {
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = ve;
                ve.SendEvent(click);
            }
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
        public IEnumerator ClosingCardStopsTTS()
        {
            ForceDeckToSingleCard(MakeTestMCQCard(firstCorrect: true));

            GameController gc = Object.FindFirstObjectByType<GameController>();
            gc.LandedOnBlueSquare();
            yield return null;

            var tts = GameObject.Find("TTS").GetComponent<TTS>();
            tts.Speak("TEST");

            yield return null;

            var root = gc.blueCardUiDocument.rootVisualElement;
            var answerButtons = GetAnswerButtons(root);
            var continueButton = root.Q<Button>("feedback_continue");

            ClickVisualElement(answerButtons[0]); // correct
            yield return null;
            ClickVisualElement(continueButton);
            yield return null;

            Assert.IsFalse(tts.Speaking());
        }
    }
}