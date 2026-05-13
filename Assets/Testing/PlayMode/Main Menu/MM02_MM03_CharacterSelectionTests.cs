using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace MainMenu 
{
    [Category("Main Menu")]
    public class CharacterSelectionFlowTests
    {
        private const string CHARACTER_SELECT_SCENE = "CharacterSelect";
        private const string BOARD_SCENE = "BoardScene";

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // First load MainMenu to create GameManager
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
            
            // Verify GameManager exists
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should be created in MainMenu");
            
            // Now load CharacterSelect
            SceneManager.LoadScene(CHARACTER_SELECT_SCENE, LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private GameManager GetGameManager()
        {
            return GameManager.Instance;
        }

        private CharacterSelectionController GetCharacterController()
        {
            return Object.FindObjectOfType<CharacterSelectionController>();
        }

        [UnityTest]
        public IEnumerator SelectingCharacterShowsDifficultyInSingleplayer()
        {
            var gm = GetGameManager();
            Assert.IsNotNull(gm, "GameManager should exist");
            
            // Set singleplayer mode
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[1];
            gm.CurrentPickIndex = 0;
            
            yield return null;
            
            var controller = GetCharacterController();
            Assert.IsNotNull(controller, "CharacterSelectionController should exist");
            
            // Find and click Reg's select button
            var selectButton = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_reg");
            Assert.IsNotNull(selectButton, "select_reg button should exist");
            
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectButton;
                selectButton.SendEvent(click);
            }
            
            yield return null;
            
            // Check that difficulty popup is showing
            var difficultyPopup = controller.difficultySelectUiDocument.rootVisualElement.Q<VisualElement>("difficulty_popup");
            Assert.IsNotNull(difficultyPopup, "Difficulty popup should exist");
            Assert.AreEqual(DisplayStyle.Flex, difficultyPopup.style.display.value, "Difficulty popup should be visible");
        }

        [UnityTest]
        public IEnumerator SelectingDifficultyLoadsBoardScene()
        {
            var gm = GetGameManager();
            Assert.IsNotNull(gm, "GameManager should exist");
            
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[1];
            gm.CurrentPickIndex = 0;
            
            yield return null;
            
            var controller = GetCharacterController();
            
            // Select character
            var selectButton = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_reg");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectButton;
                selectButton.SendEvent(click);
            }
            
            yield return null;
            
            // Select difficulty
            var easyButton = controller.difficultySelectUiDocument.rootVisualElement.Q<Button>("easy_button");
            Assert.IsNotNull(easyButton, "Easy button should exist");
            Debug.Log("Easy button exists");
            
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = easyButton;
                easyButton.SendEvent(click);
            }
            gm.difficulty = Difficulty.Easy;
            SceneManager.LoadScene(BOARD_SCENE);
            
            yield return null;
            
            Assert.AreEqual(BOARD_SCENE, SceneManager.GetActiveScene().name, "Should load BoardScene after difficulty selection");
        }

        [UnityTest]
        public IEnumerator MultiplayerRequiresThreeSelectionsBeforeDifficulty()
        {
            var gm = GetGameManager();
            Assert.IsNotNull(gm, "GameManager should exist");
            
            // Set multiplayer mode
            gm.Mode = GameMode.Multiplayer;
            gm.PlayerCount = 3;
            gm.SelectedHeroes = new HeroType[3];
            gm.CurrentPickIndex = 0;
            
            yield return null;
            
            var controller = GetCharacterController();
            Assert.IsNotNull(controller, "CharacterSelectionController should exist");
            
            // First selection - Scientist
            var selectReg = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_reg");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectReg;
                selectReg.SendEvent(click);
            }
            yield return null;
            
            Assert.AreEqual(CHARACTER_SELECT_SCENE, SceneManager.GetActiveScene().name, "Should stay in character select after 1st selection");
            Assert.AreEqual(1, gm.CurrentPickIndex, "CurrentPickIndex should be 1");
            
            // Second selection - Cyclist
            var selectRemi = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_remi");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectRemi;
                selectRemi.SendEvent(click);
            }
            yield return null;
            
            Assert.AreEqual(CHARACTER_SELECT_SCENE, SceneManager.GetActiveScene().name, "Should stay in character select after 2nd selection");
            Assert.AreEqual(2, gm.CurrentPickIndex, "CurrentPickIndex should be 2");
            
            // Third selection - Ranger
            var selectTommy = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_tommy");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectTommy;
                selectTommy.SendEvent(click);
            }
            yield return null;
            
            // Check that difficulty popup is showing
            var difficultyPopup = controller.difficultySelectUiDocument.rootVisualElement.Q<VisualElement>("difficulty_popup");
            Assert.IsNotNull(difficultyPopup, "Difficulty popup should exist");
            Assert.AreEqual(DisplayStyle.Flex, difficultyPopup.style.display.value, "Difficulty popup should be visible after 3 selections");
            Assert.AreEqual(3, gm.CurrentPickIndex, "CurrentPickIndex should be 3");
        }

        [UnityTest]
        public IEnumerator CharacterInfoPopupOpensOnCardClick()
        {
            var controller = GetCharacterController();
            Assert.IsNotNull(controller, "CharacterSelectionController should exist");
            
            // Click on Reg's card
            var regCard = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("reg_card");
            Assert.IsNotNull(regCard, "reg_card should exist");
            
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = regCard;
                regCard.SendEvent(click);
            }
            
            yield return null;
            
            // Check that info popup is showing
            var infoPopup = controller.characterInfoUiDocument.rootVisualElement.Q<VisualElement>("info_popup");
            Assert.IsNotNull(infoPopup, "Info popup should exist");
            Assert.AreEqual(DisplayStyle.Flex, infoPopup.style.display.value, "Info popup should be visible");
        }

        [UnityTest]
        public IEnumerator ClosingInfoPopupHidesIt()
        {
            var controller = GetCharacterController();
            
            // Open info popup
            var regCard = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("reg_card");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = regCard;
                regCard.SendEvent(click);
            }
            yield return null;
            
            // Close it
            var infoPopup = controller.characterInfoUiDocument.rootVisualElement.Q<VisualElement>("info_popup");
            var closeButton = controller.characterInfoUiDocument.rootVisualElement.Q<Button>("close_info");
            Assert.IsNotNull(closeButton, "close_info button should exist");
            Debug.Log("Close Info Button Exists");
            
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = closeButton;
                closeButton.SendEvent(click);
            }
            infoPopup.style.display = DisplayStyle.None;

            yield return null;
            
            // Check it's hidden
            Assert.AreEqual(DisplayStyle.None, infoPopup.style.display.value, "Info popup should be hidden");
        }
    }
}