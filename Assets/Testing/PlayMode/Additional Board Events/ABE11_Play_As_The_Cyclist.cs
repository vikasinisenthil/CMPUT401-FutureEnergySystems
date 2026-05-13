using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AdditionalBoardEvents
{
    /// <summary>
    /// ABE.11 - Play as the Cyclist
    /// Acceptance tests for choosing and playing as Remi the Cyclist (Speed Boost, Traffic Weaver).
    /// </summary>
    [Category("Additional Board Events")]
    public class ABE11_Play_As_The_Cyclist
    {
        private const string BOARD_SCENE = "BoardScene";

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist");

            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[] { HeroType.Cyclist };

            SceneManager.LoadScene("CharacterSelect", LoadSceneMode.Single);
            yield return null;
        }

        /// <summary>
        /// AC1: The player can choose Cyclist on the character selection screen before the game starts.
        /// </summary>
        [UnityTest]
        public IEnumerator CyclistIsSelectableOnCharacterScreen()
        {
            var controller = Object.FindObjectOfType<CharacterSelectionController>();
            Assert.IsNotNull(controller, "CharacterSelectionController should exist");

            var remiCard = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("remi_card");
            Assert.IsNotNull(remiCard, "Remi (Cyclist) card should exist on character selection screen");

            yield return null;
        }

        /// <summary>
        /// AC5: The Cyclist's abilities are described on the character selection screen.
        /// </summary>
        [UnityTest]
        public IEnumerator CyclistAbilityIsDescribedOnCharacterScreen()
        {
            var controller = Object.FindObjectOfType<CharacterSelectionController>();

            var remiCard = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("remi_card");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = remiCard;
                remiCard.SendEvent(click);
            }

            yield return null;

            var abilities = controller.characterInfoUiDocument.rootVisualElement.Q<Label>("info_abilities");
            Assert.IsNotNull(abilities, "Abilities label should exist in character info");
            string abilitiesLower = abilities.text.ToLower();
            Assert.IsTrue(
                abilitiesLower.Contains("speed boost") || abilitiesLower.Contains("move +1"),
                "Cyclist ability description should mention Speed Boost or +1 move");
            Assert.IsTrue(
                abilitiesLower.Contains("traffic weaver") || abilitiesLower.Contains("zero pollution"),
                "Cyclist ability description should mention Traffic Weaver or zero pollution damage");

            yield return null;
        }

        /// <summary>
        /// AC6: During the game, the avatar and theme reflect the Cyclist.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectingCyclistLoadsGameWithCyclist()
        {
            var gm = GameManager.Instance;
            var controller = Object.FindObjectOfType<CharacterSelectionController>();

            controller.OnRemiSelected();
            yield return null;

            var difficultyPopup = controller.difficultySelectUiDocument.rootVisualElement.Q<VisualElement>("difficulty_popup");
            var easyButton = controller.difficultySelectUiDocument.rootVisualElement.Q<Button>("easy_button");

            if (easyButton != null)
            {
                using (var downEvt = PointerDownEvent.GetPooled())
                {
                    downEvt.target = easyButton;
                    easyButton.SendEvent(downEvt);
                }
                using (var upEvt = PointerUpEvent.GetPooled())
                {
                    upEvt.target = easyButton;
                    easyButton.SendEvent(upEvt);
                }
            }
            gm.difficulty = Difficulty.Easy;
            SceneManager.LoadScene(BOARD_SCENE);

            yield return null;

            Assert.AreEqual("BoardScene", SceneManager.GetActiveScene().name, "Should load BoardScene");
            Assert.AreEqual(HeroType.Cyclist, gm.SelectedHeroes[0], "Selected hero should be Cyclist");
        }

        /// <summary>
        /// AC6: During the game, the avatar reflects the Cyclist (hero type and sprite).
        /// </summary>
        [UnityTest]
        public IEnumerator CyclistAvatarReflectedInGame()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Cyclist };

            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;

            var gc = Object.FindObjectOfType<GameController>();
            Assert.IsNotNull(gc, "GameController should exist");
            Assert.AreEqual(HeroType.Cyclist, gc.player.heroType, "Player should be Cyclist");

            var playerSprite = gc.player.gameObject.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(playerSprite, "Player should have a SpriteRenderer");
            Assert.IsNotNull(playerSprite.sprite, "Player should have a sprite assigned");

            var cyclistSprites = gm.GetCharacterSprites(HeroType.Cyclist);
            Assert.IsNotNull(cyclistSprites, "Cyclist sprites should be configured");
            Assert.IsNotNull(cyclistSprites.idleSprite, "Cyclist idle sprite should be assigned");

            yield return null;
        }

        /// <summary>
        /// AC2/AC3: When the Cyclist lands on a Green spot (or "Ride Bike!" path), they move +1 extra space automatically.
        /// Verifies that after completing a green square minigame, the Cyclist advances one extra square.
        /// </summary>
        [UnityTest]
        public IEnumerator CyclistMovesPlusOneAfterGreenSquare()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Cyclist };

            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;

            var gc = Object.FindObjectOfType<GameController>();
            Assert.IsNotNull(gc, "GameController should exist");
            Assert.AreEqual(HeroType.Cyclist, gc.player.heroType, "Player should be Cyclist");

            // Ensure a placeholder minigame is available
            var placeholder = Object.FindFirstObjectByType<PlaceholderMinigame>(FindObjectsInactive.Include)
                ?? Object.FindObjectOfType<PlaceholderMinigame>(true);
            Assert.IsNotNull(placeholder, "BoardScene must contain a PlaceholderMinigame for this test");
            var mm = gc.GetComponent<MinigameManager>();
            if (mm != null)
            {
                mm.minigameObjects.Clear();
                mm.minigameObjects.Add(placeholder.gameObject);
            }

            int indexBefore = gc.player.boardSquareIndex;

            gc.LandedOnGreenSquare();
            yield return null;

            var root = placeholder.uiDocument.rootVisualElement;
            var completeButton = root.Q<Button>("complete_button");
            Assert.IsNotNull(completeButton, "PlaceholderMinigame should have a complete_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = completeButton;
                completeButton.SendEvent(click);
            }

            // Wait for the +1 bonus move to complete (Cyclist Speed Boost)
            float timeout = 5f;
            while (gc.player.moving && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            Assert.IsFalse(gc.player.moving, "Player should have finished the bonus move");
            Assert.AreEqual(indexBefore + 1, gc.player.boardSquareIndex,
                "Cyclist should have moved +1 extra space after completing green square minigame (Speed Boost)");

            yield return null;
        }

        /// <summary>
        /// Remi's Speed Boost should move forward only after a green-square minigame
        /// and must not trigger a grey tile effect on the destination tile.
        /// </summary>
        [UnityTest]
        public IEnumerator CyclistBonusMoveDoesNotTriggerGreyTileEffect()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Cyclist };

            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;

            var gc = Object.FindObjectOfType<GameController>();
            Assert.IsNotNull(gc, "GameController should exist");

            var placeholder = Object.FindFirstObjectByType<PlaceholderMinigame>(FindObjectsInactive.Include)
                ?? Object.FindObjectOfType<PlaceholderMinigame>(true);
            Assert.IsNotNull(placeholder, "BoardScene must contain a PlaceholderMinigame for this test");

            var mm = gc.GetComponent<MinigameManager>();
            if (mm != null)
            {
                mm.minigameObjects.Clear();
                mm.minigameObjects.Add(placeholder.gameObject);
            }

            ScoreManager.Instance.SetScore(5);
            int scoreBefore = ScoreManager.Instance.GetScore();
            int expectedScoreAfterMinigameOnly = Mathf.Max(scoreBefore - placeholder.pollutionReductionAmount, 0);

            gc.player.boardSquareIndex = 0;
            gc.boardSquares[1].GetComponent<BoardSquare>().color = BoardSquareColor.GRAY;
            gc.boardSquares[1].GetComponent<BoardSquare>().greySpotType = GreySpotType.CarTraffic;

            gc.LandedOnGreenSquare();
            yield return null;

            var root = placeholder.uiDocument.rootVisualElement;
            var completeButton = root.Q<Button>("complete_button");
            Assert.IsNotNull(completeButton, "PlaceholderMinigame should have a complete_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = completeButton;
                completeButton.SendEvent(click);
            }

            float timeout = 5f;
            while (gc.player.moving && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            Assert.IsFalse(gc.player.moving, "Player should have finished the bonus move");
            Assert.AreEqual(1, gc.player.boardSquareIndex, "Cyclist should still advance one tile after the minigame.");
            Assert.AreEqual(expectedScoreAfterMinigameOnly, ScoreManager.Instance.GetScore(),
                "Only the minigame reward should affect score; the grey tile penalty should not trigger from the bonus move.");

            var grayOverlay = gc.grayCardUiDocument.rootVisualElement.Q<VisualElement>("overlay");
            Assert.IsNotNull(grayOverlay, "Grey overlay should exist");
            Assert.AreEqual(DisplayStyle.None, grayOverlay.resolvedStyle.display, "Grey overlay should remain closed after the bonus move.");
        }

        /// <summary>
        /// Remi's Speed Boost should move forward only after a green-square minigame
        /// and must not trigger a blue tile effect on the destination tile.
        /// </summary>
        [UnityTest]
        public IEnumerator CyclistBonusMoveDoesNotTriggerBlueTileEffect()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Cyclist };

            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;

            var gc = Object.FindObjectOfType<GameController>();
            Assert.IsNotNull(gc, "GameController should exist");

            var placeholder = Object.FindFirstObjectByType<PlaceholderMinigame>(FindObjectsInactive.Include)
                ?? Object.FindObjectOfType<PlaceholderMinigame>(true);
            Assert.IsNotNull(placeholder, "BoardScene must contain a PlaceholderMinigame for this test");

            var mm = gc.GetComponent<MinigameManager>();
            if (mm != null)
            {
                mm.minigameObjects.Clear();
                mm.minigameObjects.Add(placeholder.gameObject);
            }

            gc.player.boardSquareIndex = 0;
            gc.boardSquares[1].GetComponent<BoardSquare>().color = BoardSquareColor.BLUE;

            var blueFactDoc = GameObject.Find("BlueFactCardUIDocument")?.GetComponent<UIDocument>();
            var blueMcqDoc = GameObject.Find("BlueMCQCardUIDocument")?.GetComponent<UIDocument>();
            Assert.IsNotNull(blueFactDoc, "Blue fact card UI should exist");
            Assert.IsNotNull(blueMcqDoc, "Blue MCQ card UI should exist");

            gc.LandedOnGreenSquare();
            yield return null;

            var root = placeholder.uiDocument.rootVisualElement;
            var completeButton = root.Q<Button>("complete_button");
            Assert.IsNotNull(completeButton, "PlaceholderMinigame should have a complete_button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = completeButton;
                completeButton.SendEvent(click);
            }

            float timeout = 5f;
            while (gc.player.moving && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            Assert.IsFalse(gc.player.moving, "Player should have finished the bonus move");
            Assert.AreEqual(1, gc.player.boardSquareIndex, "Cyclist should still advance one tile after the minigame.");
            Assert.AreEqual(DisplayStyle.None, blueFactDoc.rootVisualElement.resolvedStyle.display, "Blue fact card should remain closed after the bonus move.");
            Assert.AreEqual(DisplayStyle.None, blueMcqDoc.rootVisualElement.resolvedStyle.display, "Blue MCQ card should remain closed after the bonus move.");
        }

        /// <summary>
        /// AC4: When the Cyclist lands on a "Car/Traffic" Grey spot, they take zero pollution damage (Traffic Weaver).
        /// </summary>
        [UnityTest]
        public IEnumerator CyclistTakesZeroDamageOnCarTrafficGreySpot()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Cyclist };

            SceneManager.LoadScene(BOARD_SCENE, LoadSceneMode.Single);
            yield return null;

            var gc = Object.FindObjectOfType<GameController>();
            Assert.IsNotNull(gc, "GameController should exist");
            Assert.AreEqual(HeroType.Cyclist, gc.player.heroType, "Player should be Cyclist");

            ScoreManager.Instance.SetScore(5);
            int scoreBefore = ScoreManager.Instance.GetScore();

            gc.LandedOnGraySquare(isCarTraffic: true);

            var root = gc.grayCardUiDocument.rootVisualElement.Q<VisualElement>("overlay");
            var closeButton = root.Q<Button>("close_button");
            Assert.IsNotNull(closeButton, "Grey overlay should have a close button");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = closeButton;
                closeButton.SendEvent(click);
            }

            yield return null;

            Assert.AreEqual(scoreBefore, ScoreManager.Instance.GetScore(),
                "Cyclist should take zero pollution damage on Car/Traffic grey spot (Traffic Weaver)");

            yield return null;
        }
    }
}
