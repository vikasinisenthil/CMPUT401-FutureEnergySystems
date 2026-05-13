using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GeneralBoardMovement
{
    [Category("General Board Movement")]
    public class GBM08_Turn_Camera_Follow
    {
        private GameController gc;
        private CameraController cameraController;
        private Camera mainCamera;
        private GameManager gameManager;

        // Waits until the camera position changes from a reference point, up to maxFrames.
        private IEnumerator WaitForCameraToMove(Vector3 referencePos, int maxFrames = 3600)
        {
            for (int i = 0; i < maxFrames; i++)
            {
                if (mainCamera.transform.position != referencePos) yield break;
                yield return null;
            }
        }

        // Waits until the camera's orthographic size changes from a reference value, up to maxFrames.
        private IEnumerator WaitForCameraSizeToChange(float referenceSize, int maxFrames = 3600)
        {
            for (int i = 0; i < maxFrames; i++)
            {
                if (!Mathf.Approximately(mainCamera.orthographicSize, referenceSize)) yield break;
                yield return null;
            }
        }

        // Waits until the camera stops drifting (two consecutive frames within threshold), up to maxFrames.
        private IEnumerator WaitForCameraToSettle(float threshold = 0.01f, int maxFrames = 3600)
        {
            Vector3 prev = mainCamera.transform.position;
            for (int i = 0; i < maxFrames; i++)
            {
                yield return null;
                if (Vector3.Distance(prev, mainCamera.transform.position) < threshold) yield break;
                prev = mainCamera.transform.position;
            }
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;

            gameManager = GameManager.Instance;
            Assert.IsNotNull(gameManager, "GameManager should exist");
        }

        private IEnumerator SetupSingleplayer()
        {
            gameManager.Mode = GameMode.Singleplayer;
            gameManager.PlayerCount = 1;
            gameManager.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            gameManager.difficulty = Difficulty.Easy;

            yield return null;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            gc = Object.FindObjectOfType<GameController>();
            mainCamera = Camera.main;
            cameraController = mainCamera?.GetComponent<CameraController>();

            Assert.IsNotNull(gc, "GameController should exist");
            Assert.IsNotNull(mainCamera, "Main Camera should exist");
            Assert.IsNotNull(cameraController, "CameraController should exist on Main Camera");
        }

        private IEnumerator SetupMultiplayer(int playerCount)
        {
            gameManager.Mode = GameMode.Multiplayer;
            gameManager.PlayerCount = playerCount;
            gameManager.SelectedHeroes = playerCount == 2
                ? new HeroType[] { HeroType.Cyclist, HeroType.Scientist }
                : new HeroType[] { HeroType.Cyclist, HeroType.Scientist, HeroType.Ranger };
            gameManager.difficulty = Difficulty.Easy;

            yield return null;

            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            gc = Object.FindObjectOfType<GameController>();
            mainCamera = Camera.main;
            cameraController = mainCamera?.GetComponent<CameraController>();

            Assert.IsNotNull(gc, "GameController should exist");
            Assert.IsNotNull(mainCamera, "Main Camera should exist");
            Assert.IsNotNull(cameraController, "CameraController should exist on Main Camera");
        }

        [UnityTest]
        public IEnumerator CameraFollowsPlayerDuringMovement()
        {
            yield return SetupSingleplayer();

            Vector3 initialCameraPos = mainCamera.transform.position;

            gc.MovePlayer(3, triggerLandOn: false, showCountdown: false);
            yield return null;

            // Poll until camera actually moves, or bail after 120 frames
            yield return WaitForCameraToMove(initialCameraPos, maxFrames: 3600);

            Assert.AreNotEqual(initialCameraPos, mainCamera.transform.position,
                "Camera should move when player is moving");

            for (int i = 0; i < 300; i++)
            {
                if (!gc.player.moving) break;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CameraStopsWhenPlayerStopsMoving()
        {
            yield return SetupSingleplayer();

            gc.MovePlayer(2, triggerLandOn: false, showCountdown: false);
            yield return null;

            for (int i = 0; i < 300; i++)
            {
                if (!gc.player.moving) break;
                yield return null;
            }

            for (int i = 0; i < 300; i++)
            {
                if (!gc.player.moving) break;
                yield return null;
            }

            for (int i = 0; i < 300; i++)
            {
                if (!gc.player.moving) break;
                yield return null;
            }

            Assert.IsFalse(gc.player.moving, "Player should have stopped moving");

            // Poll until camera settles, then snapshot two frames to confirm stability
            yield return WaitForCameraToSettle(threshold: 0.01f, maxFrames: 3600);

            Vector3 cameraPos1 = mainCamera.transform.position;
            yield return null;
            Vector3 cameraPos2 = mainCamera.transform.position;

            float cameraDrift = Vector3.Distance(cameraPos1, cameraPos2);
            Assert.Less(cameraDrift, 0.5f,
                "Camera should be stable when player is not moving");
        }

        [UnityTest]
        public IEnumerator CameraZoomsInWhenFollowingPlayer()
        {
            yield return SetupSingleplayer();

            float initialSize = mainCamera.orthographicSize;

            gc.MovePlayer(3, triggerLandOn: false, showCountdown: false);
            yield return null;

            // Poll until orthographic size actually changes
            yield return WaitForCameraSizeToChange(initialSize, maxFrames: 3600);

            Assert.Less(mainCamera.orthographicSize, initialSize + 0.5f,
                "Camera should zoom in when following player");

            for (int i = 0; i < 300; i++)
            {
                if (!gc.player.moving) break;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CameraFollowsActivePlayerInMultiplayer()
        {
            yield return SetupMultiplayer(2);

            Vector3 player1Pos = gc.players[0].gameObject.transform.position;

            gc.MovePlayer(2, triggerLandOn: false, showCountdown: false);
            yield return null;

            // Poll until camera moves from its initial position
            yield return WaitForCameraToMove(mainCamera.transform.position, maxFrames: 3600);

            Vector3 cameraPos = mainCamera.transform.position;
            float distanceToPlayer1 = Vector3.Distance(
                new Vector3(cameraPos.x, cameraPos.y, 0),
                new Vector3(player1Pos.x, player1Pos.y, 0)
            );
            Assert.Less(distanceToPlayer1, 10f,
                "Camera should be near Player 1 during their turn");

            for (int i = 0; i < 300; i++)
            {
                if (!gc.players[0].moving) break;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CameraKeepsPlayerVisible()
        {
            yield return SetupSingleplayer();

            gc.MovePlayer(5, triggerLandOn: false, showCountdown: false);
            yield return null;

            for (int i = 0; i < 100 && gc.player.moving; i++)
            {
                Vector3 playerPos = gc.player.gameObject.transform.position;
                Vector3 cameraPos = mainCamera.transform.position;

                float distance = Vector3.Distance(
                    new Vector3(playerPos.x, playerPos.y, 0),
                    new Vector3(cameraPos.x, cameraPos.y, 0)
                );

                Assert.Less(distance, 8f,
                    $"Player should remain visible (near camera center) during movement. Distance: {distance}");

                yield return null;
            }

            for (int i = 0; i < 200; i++)
            {
                if (!gc.player.moving) break;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CameraDoesNotObscureUI()
        {
            yield return SetupSingleplayer();

            gc.MovePlayer(3, triggerLandOn: false, showCountdown: false);
            yield return null;

            // Poll until camera moves (movement has started) before checking UI
            yield return WaitForCameraToMove(mainCamera.transform.position, maxFrames: 3600);

            var diceButton = gc.inGameUiDocument.rootVisualElement.Q<UnityEngine.UIElements.Button>("dice_button");
            Assert.IsNotNull(diceButton, "Dice button should exist");
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex,
                diceButton.resolvedStyle.display,
                "Dice button should be visible");

            var scoreBox = gc.inGameUiDocument.rootVisualElement.Q<UnityEngine.UIElements.VisualElement>("score-1");
            Assert.IsNotNull(scoreBox, "Score box should exist");
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex,
                scoreBox.resolvedStyle.display,
                "Score box should be visible");

            for (int i = 0; i < 300; i++)
            {
                if (!gc.player.moving) break;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CameraFramingIncludesUpcomingSpaces()
        {
            yield return SetupSingleplayer();

            int startIndex = gc.player.boardSquareIndex;
            Vector3 startPos = gc.boardSquares[startIndex].transform.position;

            gc.MovePlayer(4, triggerLandOn: false, showCountdown: false);
            yield return null;

            // Poll until camera starts moving before sampling its position
            yield return WaitForCameraToMove(mainCamera.transform.position, maxFrames: 3600);

            int targetIndex = Mathf.Min(startIndex + 4, gc.boardSquares.Count - 1);
            Vector3 targetPos = gc.boardSquares[targetIndex].transform.position;

            Vector3 cameraPos = mainCamera.transform.position;

            float distToStart = Vector3.Distance(
                new Vector3(cameraPos.x, cameraPos.y, 0),
                new Vector3(startPos.x, startPos.y, 0)
            );
            float distToTarget = Vector3.Distance(
                new Vector3(cameraPos.x, cameraPos.y, 0),
                new Vector3(targetPos.x, targetPos.y, 0)
            );

            Assert.Less(distToStart, 15f, "Camera should not be too far from start position");
            Assert.Less(distToTarget, 15f, "Camera should not be too far from target position");

            for (int i = 0; i < 300; i++)
            {
                if (!gc.player.moving) break;
                yield return null;
            }
        }
    }
}