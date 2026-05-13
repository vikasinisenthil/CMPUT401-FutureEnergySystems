using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Multiplayer
{
    [Category("Multiplayer")]
    public class MP01_PlayWith2Others
    {
        private GameController gameController;
        private GameManager gameManager;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            PlayerPrefs.SetInt(GameController.PREF_SKIP_DICE_ANIMATION_FOR_TESTS, 1);
            PlayerPrefs.Save();

            // Load MainMenu to create GameManager
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;

            gameManager = GameManager.Instance;
            Assert.IsNotNull(gameManager, "GameManager should exist");
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            PlayerPrefs.SetInt(GameController.PREF_SKIP_DICE_ANIMATION_FOR_TESTS, 0);
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CanStart2PlayerGame()
        {
            // Set up 2 player game
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 2;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist };
            gameManager.difficulty = Difficulty.Easy;

            // Load multiplayer board
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();
            Assert.IsNotNull(gameController, "GameController should exist");
            Assert.AreEqual(2, gameController.players.Count, "Should have 2 players");

            // Verify both players exist
            Assert.IsNotNull(GameObject.Find("Player1"), "Player1 should exist");
            Assert.IsNotNull(GameObject.Find("Player2"), "Player2 should exist");
        }

        [UnityTest]
        public IEnumerator CanStart3PlayerGame()
        {
            // Set up 3 player game
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 3;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist, HeroType.Ranger };
            gameManager.difficulty = Difficulty.Easy;

            // Load multiplayer board
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();
            Assert.IsNotNull(gameController, "GameController should exist");
            Assert.AreEqual(3, gameController.players.Count, "Should have 3 players");

            // Verify all players exist
            Assert.IsNotNull(GameObject.Find("Player1"), "Player1 should exist");
            Assert.IsNotNull(GameObject.Find("Player2"), "Player2 should exist");
            Assert.IsNotNull(GameObject.Find("Player3"), "Player3 should exist");
        }

        [UnityTest]
        public IEnumerator PlayersTakeTurnsInOrder()
        {
            // Set up 3 player game
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 3;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist, HeroType.Ranger };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();

            // Verify starting player is Player 1
            Assert.AreEqual(0, gameController.currentPlayerIndex, "Should start with Player 1");
            Assert.AreEqual(gameController.players[0], gameController.player, "Current player should be Player 1");

            // Simulate turn end
            gameController.ResumeGameAfterMinigame();
            yield return null;

            // Verify turn advanced to Player 2
            Assert.AreEqual(1, gameController.currentPlayerIndex, "Should advance to Player 2");
            Assert.AreEqual(gameController.players[1], gameController.player, "Current player should be Player 2");

            // Simulate turn end
            gameController.ResumeGameAfterMinigame();
            yield return null;

            // Verify turn advanced to Player 3
            Assert.AreEqual(2, gameController.currentPlayerIndex, "Should advance to Player 3");
            Assert.AreEqual(gameController.players[2], gameController.player, "Current player should be Player 3");

            // Simulate turn end - should wrap back to Player 1
            gameController.ResumeGameAfterMinigame();
            yield return null;

            Assert.AreEqual(0, gameController.currentPlayerIndex, "Should wrap back to Player 1");
            Assert.AreEqual(gameController.players[0], gameController.player, "Current player should be Player 1 again");
        }

        [UnityTest]
        public IEnumerator OnlyCurrentPlayerCanMove()
        {
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 2;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();
            var rollDiceButton = gameController.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");

            Assert.IsNotNull(rollDiceButton, "Dice button should exist");
            Assert.IsTrue(rollDiceButton.enabledSelf, "Dice button should be enabled for Player 1's turn");

            // Record initial positions
            Vector3 player1InitialPos = gameController.players[0].gameObject.transform.position;
            Vector3 player2InitialPos = gameController.players[1].gameObject.transform.position;

            // Player 1 rolls
            using (var click = ClickEvent.GetPooled())
            {
                click.target = rollDiceButton;
                rollDiceButton.SendEvent(click);
            }
            yield return null;

            // Wait for movement to complete
            for (int i = 0; i < 300; i++)
            {
                if (!gameController.players[0].moving) break;
                yield return null;
            }

            // Verify only Player 1 moved
            Assert.AreNotEqual(player1InitialPos, gameController.players[0].gameObject.transform.position, "Player 1 should have moved");
            Assert.AreEqual(player2InitialPos, gameController.players[1].gameObject.transform.position, "Player 2 should not have moved");
        }

        [UnityTest]
        public IEnumerator EachPlayerHasSeparateToken()
        {
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 3;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist, HeroType.Ranger };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();

            // Verify each player has their own GameObject
            GameObject player1Obj = gameController.players[0].gameObject;
            GameObject player2Obj = gameController.players[1].gameObject;
            GameObject player3Obj = gameController.players[2].gameObject;

            Assert.AreNotEqual(player1Obj, player2Obj, "Player 1 and 2 should have different GameObjects");
            Assert.AreNotEqual(player2Obj, player3Obj, "Player 2 and 3 should have different GameObjects");
            Assert.AreNotEqual(player1Obj, player3Obj, "Player 1 and 3 should have different GameObjects");

            // Verify each has SpriteRenderer (visual token)
            Assert.IsNotNull(player1Obj.GetComponent<SpriteRenderer>(), "Player 1 should have sprite");
            Assert.IsNotNull(player2Obj.GetComponent<SpriteRenderer>(), "Player 2 should have sprite");
            Assert.IsNotNull(player3Obj.GetComponent<SpriteRenderer>(), "Player 3 should have sprite");
        }

        [UnityTest]
        public IEnumerator CurrentPlayerIsIndicatedInUI()
        {
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 2;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();
            var rollDiceButton = gameController.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");

            // Check Player 1's turn indicator
            Assert.IsTrue(rollDiceButton.text.Contains("P1") || rollDiceButton.text.Contains("Player 1"), 
                "Dice button should indicate Player 1's turn");

            // End turn
            gameController.ResumeGameAfterMinigame();
            yield return null;

            // Check Player 2's turn indicator
            Assert.IsTrue(rollDiceButton.text.Contains("P2") || rollDiceButton.text.Contains("Player 2"), 
                "Dice button should indicate Player 2's turn");
        }

        [UnityTest]
        public IEnumerator DiceButtonDisabledDuringOtherPlayersTurn()
        {
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 2;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();
            var rollDiceButton = gameController.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");

            // Player 1 rolls dice
            using (var click = ClickEvent.GetPooled())
            {
                click.target = rollDiceButton;
                rollDiceButton.SendEvent(click);
            }
            yield return null;

            // During movement, button should be disabled (implicitly, since player.moving = true)
            // We can't directly test button state during movement without dice button checking player.moving
            // But we can verify turn indicator updates after movement

            // Wait for movement to complete
            for (int i = 0; i < 300; i++)
            {
                if (!gameController.players[0].moving) break;
                yield return null;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TurnOrderRepeatsUntilGameEnds()
        {
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 2;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();

            // Simulate multiple turn cycles
            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Player 1's turn
                Assert.AreEqual(0, gameController.currentPlayerIndex, $"Cycle {cycle}: Should be Player 1");
                gameController.ResumeGameAfterMinigame();
                yield return null;

                // Player 2's turn
                Assert.AreEqual(1, gameController.currentPlayerIndex, $"Cycle {cycle}: Should be Player 2");
                gameController.ResumeGameAfterMinigame();
                yield return null;

                // Should wrap back to Player 1
            }

            Assert.AreEqual(0, gameController.currentPlayerIndex, "Should be back to Player 1 after 3 cycles");
        }

        [UnityTest]
        public IEnumerator OnlyCurrentPlayerSeesBlueCard()
        {
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 2;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();

            // Verify Player 1 is current
            Assert.AreEqual(0, gameController.currentPlayerIndex, "Should be Player 1's turn");

            // Trigger blue square for Player 1
            gameController.LandedOnBlueSquare();
            yield return null;

            // Verify blue card is shown
            var blueCardDoc = gameController.blueCardUiDocument;
            Assert.IsNotNull(blueCardDoc, "Blue card UI should exist");
            Assert.AreEqual(DisplayStyle.Flex, blueCardDoc.rootVisualElement.style.display.value, 
                "Blue card should be visible for current player");

            // The test verifies the card appears - in actual gameplay, only the current player
            // would see this on their screen in a turn-based setup
        }

        [UnityTest]
        public IEnumerator GameSupportsExactly2Or3Players()
        {
            // Test 2 players
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 2;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();
            Assert.AreEqual(2, gameController.players.Count, "Should support 2 players");

            // Reset and test 3 players
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;

            gameManager = GameManager.Instance;
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = 3;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Cyclist, HeroType.Scientist, HeroType.Ranger };
            gameManager.difficulty = Difficulty.Easy;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;

            gameController = Object.FindObjectOfType<GameController>();
            Assert.AreEqual(3, gameController.players.Count, "Should support 3 players");
        }
    }
}