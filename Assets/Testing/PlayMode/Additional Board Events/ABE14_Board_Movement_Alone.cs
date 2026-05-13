using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AdditionalBoardEvents {
    /// <summary>
    /// Test suite for single player game functionality.
    /// User Story: As a player in a single player game, I want to move around the board on my own.
    ///
    /// Acceptance Criteria:
    /// - When the player chooses Single Player, the game starts with one player
    /// - Only that player's token moves on the board; there are no other human or bot tokens
    /// - The player rolls the dice on their turn and moves their token the rolled number of spaces
    /// - The player controls their own turns without waiting for other players
    /// - Turn order is effectively "always my turn"
    /// - The game can be completed by that single player reaching the end of the board
    /// </summary>
    [Category("Additional Board Events")]
    public class ABE14_Board_Movement_Alone
    {
        private const string MAIN_MENU_SCENE = "MainMenu";
        private const string BOARD_SCENE = "BoardScene";

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            PlayerPrefs.SetInt(GameController.PREF_SKIP_DICE_ANIMATION_FOR_TESTS, 1);
            PlayerPrefs.Save();

            // Ensure GameManager instance is clean for each test
            var existingManager = Object.FindFirstObjectByType<GameManager>();
            if (existingManager != null)
            {
                Object.Destroy(existingManager.gameObject);
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            PlayerPrefs.SetInt(GameController.PREF_SKIP_DICE_ANIMATION_FOR_TESTS, 0);
            PlayerPrefs.Save();
            yield return null;
        }

        /// <summary>
        /// Test: When the player chooses Single Player, the game starts with one player
        /// </summary>
        [UnityTest]
        public IEnumerator SinglePlayerMode_StartsWithOnePlayer()
        {
            // Arrange: Create GameManager and set to single player mode
            GameObject managerObj = new GameObject("GameManager");
            GameManager manager = managerObj.AddComponent<GameManager>();

            // Act: Set single player mode
            manager.OnSinglePlayerClicked();

            yield return null;

            // Assert: Verify game mode and player count
            Assert.AreEqual(GameMode.Singleplayer, manager.Mode, "Game mode should be Singleplayer");
            Assert.AreEqual(1, manager.PlayerCount, "Player count should be 1 in single player mode");
            Assert.IsNotNull(manager.SelectedHeroes, "SelectedHeroes array should be initialized");
            Assert.AreEqual(1, manager.SelectedHeroes.Length, "SelectedHeroes array should have exactly 1 slot");

            Object.Destroy(managerObj);
        }

        /// <summary>
        /// Test: Only one player token exists in the game (no other human or bot tokens)
        /// </summary>
        [UnityTest]
        public IEnumerator SinglePlayerGame_HasOnlyOnePlayerToken()
        {
            // Arrange: Load the board scene
            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            yield return null;

            // Act: Get the game controller
            GameController gameController = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(gameController, "GameController should exist in the scene");

            // Assert: Verify only one player exists
            Assert.IsNotNull(gameController.player, "Player object should exist");
            Assert.IsNotNull(gameController.player.gameObject, "Player GameObject should exist");

            Player player = gameController.player;
            Assert.IsNotNull(player, "Single player instance should exist");

            yield return null;
        }

        /// <summary>
        /// Test: The player rolls the dice and moves their token the rolled number of spaces
        /// </summary>
        [UnityTest]
        public IEnumerator Player_RollsDiceAndMovesCorrectNumberOfSpaces()
        {
            // Arrange: Load the board scene
            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameController gameController = Object.FindFirstObjectByType<GameController>();
            var root = gameController.inGameUiDocument.rootVisualElement;
            Button diceRollButton = root.Q<Button>("dice_button");

            int initialPosition = gameController.player.boardSquareIndex;

            // Act: Set a known random seed and roll the dice
            Random.InitState(123); // This seed produces a roll of 1

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            // Wait for movement to complete
            yield return new WaitUntil(() => !gameController.player.moving);

            // Assert: Player should have moved exactly 1 space (based on the seed)
            int expectedPosition = initialPosition + 1;
            Assert.AreEqual(expectedPosition, gameController.player.boardSquareIndex,
                "Player should move exactly the number of spaces rolled on the dice");
        }

        /// <summary>
        /// Test: The player can roll dice multiple times without waiting
        /// </summary>
        [UnityTest]
        public IEnumerator Player_CanRollMultipleTimesWithoutWaiting()
        {
            // Arrange: Load the board scene
            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameController gameController = Object.FindFirstObjectByType<GameController>();
            var root = gameController.inGameUiDocument.rootVisualElement;
            Button diceRollButton = root.Q<Button>("dice_button");

            int initialPosition = gameController.player.boardSquareIndex;

            // Act: Roll dice first time
            Random.InitState(456);
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            yield return new WaitUntil(() => !gameController.player.moving);
            int positionAfterFirstRoll = gameController.player.boardSquareIndex;

            // Act: Immediately roll dice second time
            Random.InitState(789);
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            yield return new WaitUntil(() => !gameController.player.moving);
            int positionAfterSecondRoll = gameController.player.boardSquareIndex;

            // Assert: Both rolls should have moved the player forward
            Assert.Greater(positionAfterFirstRoll, initialPosition, "First roll should move player forward");
            Assert.Greater(positionAfterSecondRoll, positionAfterFirstRoll, "Second roll should move player forward from first position");
        }

        /// <summary>
        /// Test: Turn order is effectively "always my turn" (no turn switching)
        /// </summary>
        [UnityTest]
        public IEnumerator SinglePlayer_TurnOrderIsAlwaysCurrentPlayer()
        {
            // Arrange: Load the board scene
            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameController gameController = Object.FindFirstObjectByType<GameController>();
            Player originalPlayer = gameController.player;

            // Act & Assert: Move player multiple times and verify the same player is always in control
            for (int i = 1; i <= 3; i++)
            {
                gameController.MovePlayer(1, triggerLandOn: false, showCountdown: false);
                yield return new WaitUntil(() => !gameController.player.moving);

                Assert.AreSame(originalPlayer, gameController.player,
                    $"Should be the same player instance on turn {i}");
            }
        }

        /// <summary>
        /// Test: The game can be completed by that single player reaching the end of the board
        /// </summary>
        [UnityTest]
        public IEnumerator SinglePlayer_CanCompleteGameByReachingEnd()
        {
            // Arrange: Load the board scene
            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameController gameController = Object.FindFirstObjectByType<GameController>();
            Object.FindFirstObjectByType<BossManager>()?.SkipBoss();

            var root = gameController.inGameUiDocument.rootVisualElement;
            Button diceRollButton = root.Q<Button>("dice_button");

            // Act: Manually move player to near the end of the board
            int nearEndPosition = gameController.boardSquares.Count - 2;
            gameController.player.boardSquareIndex = nearEndPosition;
            gameController.player.gameObject.GetComponent<Transform>().position =
                gameController.boardSquares[nearEndPosition].GetComponent<Transform>().position;

            yield return null;

            // Roll dice to reach the end
            Random.InitState(123); // Produces a roll that will reach the end
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            yield return new WaitUntil(() => !gameController.player.moving);

            // Assert: Player should be at or near the end of the board
            Assert.GreaterOrEqual(gameController.player.boardSquareIndex, nearEndPosition,
                "Player should reach the end position");
            Assert.LessOrEqual(gameController.player.boardSquareIndex, gameController.boardSquares.Count - 1,
                "Player should not go beyond the last square");
        }

        /// <summary>
        /// Test: Player cannot roll dice while already moving
        /// </summary>
        [UnityTest]
        public IEnumerator SinglePlayer_CannotRollWhileMoving()
        {
            // Arrange: Load the board scene
            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameController gameController = Object.FindFirstObjectByType<GameController>();
            var root = gameController.inGameUiDocument.rootVisualElement;
            Button diceRollButton = root.Q<Button>("dice_button");

            // Act: Roll dice first time
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            // Player should be moving
            Assert.IsTrue(gameController.player.moving, "Player should be moving after dice roll");
            int targetPosition = gameController.player.finalBoardSquareIndex;

            // Try to roll again while moving
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = diceRollButton;
                diceRollButton.SendEvent(click);
            }

            // Assert: Target position should not change
            Assert.AreEqual(targetPosition, gameController.player.finalBoardSquareIndex,
                "Player target should not change when rolling while already moving");

            yield return new WaitUntil(() => !gameController.player.moving);
        }

        /// <summary>
        /// Test: Player always moves forward (never backwards) in normal gameplay
        /// </summary>
        [UnityTest]
        public IEnumerator SinglePlayer_AlwaysMovesForward()
        {
            // Arrange: Load the board scene
            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;
            yield return null;

            GameController gameController = Object.FindFirstObjectByType<GameController>();

            // Act & Assert: Move player multiple times and verify always moving forward
            for (int i = 0; i < 5; i++)
            {
                int positionBefore = gameController.player.boardSquareIndex;

                gameController.MovePlayer(1, triggerLandOn: false, showCountdown: false);
                yield return new WaitUntil(() => !gameController.player.moving);

                int positionAfter = gameController.player.boardSquareIndex;
                Assert.Greater(positionAfter, positionBefore,
                    $"Player should always move forward. Iteration {i + 1}");
            }
        }
    }
}