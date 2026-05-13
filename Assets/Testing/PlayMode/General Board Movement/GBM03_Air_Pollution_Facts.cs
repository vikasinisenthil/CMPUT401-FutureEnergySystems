using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    [Category("General Board Movement")]
    public class GBM03_Air_Pollution_Facts
    {
        private static Sprite MakeDummySprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private static BlueFactCard MakeTestFactCard(string name = "TEST_NAME", string fact = "TEST_FACT")
        {
            var card = ScriptableObject.CreateInstance<BlueFactCard>();
            card.cardName = name;
            card.fact = fact;
            card.image = MakeDummySprite();
            return card;
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);

            yield return null;
        }

        [UnityTest]
        public IEnumerator LandingOnBlueSquareCanDrawFactCard()
        {
            BlueCardManager blueCardManager = GameObject.Find("GameController").GetComponent<BlueCardManager>();

            int factCardCount = 0;
            foreach (BlueCard card in blueCardManager.deck)
            {
                if (card is BlueFactCard)
                {
                    factCardCount++;
                    break;
                }
            }

            Assert.Greater(factCardCount, 0);

            yield return null;
        }

        [UnityTest]
        public IEnumerator LandingOnBlueSquareCanDrawMCQCard()
        {
            BlueCardManager blueCardManager = GameObject.Find("GameController").GetComponent<BlueCardManager>();

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
        public IEnumerator FactCardShowsFact()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            BlueCardManager blueCardManager = GameObject.Find("GameController").GetComponent<BlueCardManager>();

            blueCardManager.deck.Clear();

            BlueFactCard bfc = MakeTestFactCard();

            blueCardManager.deck.Add(bfc);

            gc.LandedOnBlueSquare();

            var root = gc.blueCardUiDocument.rootVisualElement;
            Label nameLabel = root.Q<Label>("card_title");
            Label factLabel = root.Q<Label>("card_fact");

            Assert.AreEqual("TEST_NAME", nameLabel.text);
            Assert.AreEqual("TEST_FACT", factLabel.text);

            yield return null;
        }

        [UnityTest]
        public IEnumerator FactCardDoesNotChangePollutionScore()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            BlueCardManager blueCardManager = GameObject.Find("GameController").GetComponent<BlueCardManager>();

            blueCardManager.deck.Clear();

            BlueFactCard bfc = MakeTestFactCard();
            blueCardManager.deck.Add(bfc);

            int pollutionScoreBefore = gc.player.pollutionScore;

            gc.LandedOnBlueSquare();

            Assert.AreEqual(gc.player.pollutionScore, pollutionScoreBefore);

            Button dismissButton = gc.blueCardUiDocument.rootVisualElement.Q<Button>("close_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = dismissButton;
                dismissButton.SendEvent(click);
            }

            Assert.AreEqual(gc.player.pollutionScore, pollutionScoreBefore);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NormalGameplayAfterFactCard()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            BlueCardManager blueCardManager = GameObject.Find("GameController").GetComponent<BlueCardManager>();

            blueCardManager.deck.Clear();

            BlueFactCard bfc = MakeTestFactCard();
            blueCardManager.deck.Add(bfc);

            gc.LandedOnBlueSquare();

            Button dismissButton = gc.blueCardUiDocument.rootVisualElement.Q<Button>("close_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = dismissButton;
                dismissButton.SendEvent(click);
            }

            // Needs to be visible
            Assert.AreNotEqual(gc.inGameUiDocument.rootVisualElement.style.display, DisplayStyle.None);

            Button diceRollButton = gc.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.True(diceRollButton.enabledInHierarchy);

            yield return null;
        }
    }
}
