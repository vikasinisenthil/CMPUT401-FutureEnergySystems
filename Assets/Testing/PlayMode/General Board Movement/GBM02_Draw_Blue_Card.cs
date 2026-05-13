using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    [Category("General Board Movement")]
    public class GBM02_Draw_Blue_Card
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneLoaded()
        {
            GameObject player = GameObject.Find("Player");
            Assert.NotNull(player);

            GameObject gameController = GameObject.Find("GameController");
            Assert.NotNull(gameController);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator LandingOnBlueSquareDrawsOnlyOneCard()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            BlueCardManager blueCardManager = GameObject.Find("GameController").GetComponent<BlueCardManager>();

            var initialCount = blueCardManager.deck.Count;

            gc.LandedOnBlueSquare();

            var afterCount = blueCardManager.deck.Count;

            Assert.AreEqual(afterCount + 1, initialCount);

            yield return null;
        }

        [UnityTest]
        public IEnumerator LandingOnGreenSquareDoesNotDrawCard()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            BlueCardManager blueCardManager = GameObject.Find("GameController").GetComponent<BlueCardManager>();

            var initialCount = blueCardManager.deck.Count;

            gc.LandedOnGreenSquare(GameObject.Find("Minigame_PlantTrees"));

            var afterCount = blueCardManager.deck.Count;

            Assert.AreEqual(afterCount, initialCount);

            yield return null;
        }

        [UnityTest]
        public IEnumerator LandingOnGraySquareDoesNotDrawCard()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
            BlueCardManager blueCardManager = GameObject.Find("GameController").GetComponent<BlueCardManager>();

            var initialCount = blueCardManager.deck.Count;

            gc.LandedOnGraySquare();

            var afterCount = blueCardManager.deck.Count;

            Assert.AreEqual(afterCount, initialCount);

            yield return null;
        }

        [UnityTest]
        public IEnumerator CardShownToPlayer()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();

            gc.LandedOnBlueSquare();

            Assert.AreNotEqual(gc.blueCardUiDocument.rootVisualElement.style.display, DisplayStyle.None);

            yield return null;
        }

        [UnityTest]
        public IEnumerator CannotRollDiceWhileReadingBlueCard()
        {
            GameController gc = GameObject.Find("GameController").GetComponent<GameController>();

            gc.LandedOnBlueSquare();

            UIDocument inGameUiDocument = gc.inGameUiDocument;

            Assert.AreNotEqual(inGameUiDocument.rootVisualElement.style.display, DisplayStyle.Flex);

            yield return null;
        }
    }
}