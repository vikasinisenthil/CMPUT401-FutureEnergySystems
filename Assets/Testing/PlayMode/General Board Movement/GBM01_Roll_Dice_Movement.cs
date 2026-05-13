using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement {
    [Category("General Board Movement")]
    public class GBM01_Roll_Dice_Movement
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            PlayerPrefs.SetInt(GameController.PREF_SKIP_DICE_ANIMATION_FOR_TESTS, 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            PlayerPrefs.SetInt(GameController.PREF_SKIP_DICE_ANIMATION_FOR_TESTS, 0);
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RollDiceControlAvailable()
        {
            GameObject gameController = GameObject.Find("GameController");
            var root = gameController.GetComponent<GameController>().inGameUiDocument.rootVisualElement;
            Button diceRollButton = root.Q<Button>("dice_button");

            Assert.True(diceRollButton.enabledInHierarchy);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator CannotRollTwice()
        {
            GameController gameController = GameObject.Find("GameController").GetComponent<GameController>();
            var root = gameController.inGameUiDocument.rootVisualElement;
            Button diceRollButton = root.Q<Button>("dice_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            Assert.True(gameController.player.moving);
            int playerTarget = gameController.player.finalBoardSquareIndex;
            
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            // Make sure the player did not change there target
            Assert.AreEqual(playerTarget, gameController.player.finalBoardSquareIndex);

            yield return null;
        }

        [UnityTest]
        public IEnumerator DiceBetweenOneAndSixInclusive()
        {
            for (int i = 0; i < 10; ++i)
            {
                GameController gameController = GameObject.Find("GameController").GetComponent<GameController>();
                var root = gameController.inGameUiDocument.rootVisualElement;
                Button diceRollButton = root.Q<Button>("dice_button");

                int playerIndex = gameController.player.boardSquareIndex;

                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = diceRollButton;
                    diceRollButton.SendEvent(click);
                }

                Assert.True(gameController.player.moving);
                int playerTarget = gameController.player.finalBoardSquareIndex;
                
                Assert.True(playerIndex + playerTarget <= 6 && playerIndex + playerTarget >= 1);

                SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);

                yield return null;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerMovesCorrectAmount()
        {
            GameController gameController = GameObject.Find("GameController").GetComponent<GameController>();
            var root = gameController.inGameUiDocument.rootVisualElement;

            Button diceRollButton = root.Q<Button>("dice_button");

            int playerIndex = gameController.player.boardSquareIndex;

            // With this seed the next dice roll is 1
            Random.InitState(123); 

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            yield return new WaitUntil(() =>
            {
                return !gameController.player.moving;
            });

            Assert.AreEqual(playerIndex + 1, gameController.player.boardSquareIndex);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDoesNotMoveBackwards()
        {
            for (int i = 0; i < 10; ++i)
            {
                GameController gameController = GameObject.Find("GameController").GetComponent<GameController>();
                var root = gameController.inGameUiDocument.rootVisualElement;

                Button diceRollButton = root.Q<Button>("dice_button");

                int playerIndex = gameController.player.boardSquareIndex;

                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = diceRollButton;
                    diceRollButton.SendEvent(click);
                }

                yield return new WaitUntil(() =>
                {
                    return !gameController.player.moving;
                });

                Assert.Greater(gameController.player.boardSquareIndex, playerIndex);

                SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);

                yield return new WaitForFixedUpdate();
            }

            yield return null;
        }

            [UnityTest]
        public IEnumerator PlayerDoesNotGoPastTheEnd()
        {
            GameController gameController = GameObject.Find("GameController").GetComponent<GameController>();
            var root = gameController.inGameUiDocument.rootVisualElement;
            Button diceRollButton = root.Q<Button>("dice_button");

            // Manually move player 2 squares before the end
            gameController.player.boardSquareIndex = gameController.boardSquares.Count - 2;
            gameController.player.gameObject.GetComponent<Transform>().position = gameController.boardSquares[gameController.player.boardSquareIndex].GetComponent<Transform>().position;

            int playerIndex = gameController.player.boardSquareIndex;

            // With this seed the next dice roll is 1
            Random.InitState(123); 

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            yield return new WaitUntil(() =>
            {
                return !gameController.player.moving;
            });

            Assert.LessOrEqual(gameController.player.boardSquareIndex, gameController.boardSquares.Count);

            yield return null;
        }
    }
}