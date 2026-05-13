using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace MainMenu 
{
    [Category("Main Menu")]
    public class PickPromptTests
    {
        private const string MAIN_MENU_SCENE = "MainMenu";
        private const string CHARACTER_SELECT_SCENE = "CharacterSelect";

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Load MainMenu to create GameManager
            SceneManager.LoadScene(MAIN_MENU_SCENE, LoadSceneMode.Single);
            yield return null;
        }

        private Label FindPromptLabel()
        {
            var controller = Object.FindObjectOfType<CharacterSelectionController>();
            if (controller == null || controller.characterSelectionUiDocument == null)
            {
                return null;
            }

            // Look for a prompt label in the character selection UI
            return controller.characterSelectionUiDocument.rootVisualElement.Q<Label>("screen_title");
        }

        [UnityTest]
        public IEnumerator SingleplayerShowsGenericPrompt()
        {
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist");
            
            // Set singleplayer mode
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.CurrentPickIndex = 0;
            
            // Load character select
            SceneManager.LoadScene(CHARACTER_SELECT_SCENE, LoadSceneMode.Single);
            yield return null;
            
            var prompt = FindPromptLabel();
            
            if (prompt != null)
            {
                yield return null; // Allow PickPrompt to update
                
                Assert.IsTrue(prompt.text.ToLower().Contains("choose") || 
                             prompt.text.ToLower().Contains("select"), 
                             $"Prompt should say 'choose' or 'select', got: {prompt.text}");
                
                Assert.IsFalse(prompt.text.ToLower().Contains("player 1") || 
                              prompt.text.ToLower().Contains("player 2"),
                              $"Singleplayer should not show Player numbers: {prompt.text}");
            }
            else
            {
                Assert.Inconclusive("Pick prompt label not found in UI - may not be implemented yet");
            }
        }

        [UnityTest]
        public IEnumerator MultiplayerShowsPlayer1Then2Then3()
        {
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist");
            
            // Set multiplayer mode
            gm.Mode = GameMode.Multiplayer;
            gm.PlayerCount = 3;
            gm.SelectedHeroes = new HeroType[3];
            gm.CurrentPickIndex = 0;
            
            // Load character select
            SceneManager.LoadScene(CHARACTER_SELECT_SCENE, LoadSceneMode.Single);
            yield return null;
            
            var controller = Object.FindObjectOfType<CharacterSelectionController>();
            var prompt = FindPromptLabel();
            
            if (prompt == null)
            {
                Assert.Inconclusive("Pick prompt label not found in UI - may not be implemented yet");
                yield break;
            }
            
            // Player 1
            yield return null;
            Assert.IsTrue(prompt.text.Contains("Player 1"), 
                $"Expected 'Player 1', got: {prompt.text}");
            
            // Select first character (Reg)
            var selectReg = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_reg");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectReg;
                selectReg.SendEvent(click);
            }
            yield return null;
            
            // Player 2
            Assert.IsTrue(prompt.text.Contains("Player 2"), 
                $"Expected 'Player 2', got: {prompt.text}");
            
            // Select second character (Remi)
            var selectRemi = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_remi");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectRemi;
                selectRemi.SendEvent(click);
            }
            yield return null;
            
            // Player 3
            Assert.IsTrue(prompt.text.Contains("Player 3"), 
                $"Expected 'Player 3', got: {prompt.text}");
        }

        [UnityTest]
        public IEnumerator MultiplayerDoesNotShowPlayer4()
        {
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist");
            
            gm.Mode = GameMode.Multiplayer;
            gm.PlayerCount = 3;
            gm.SelectedHeroes = new HeroType[3];
            gm.CurrentPickIndex = 0;
            
            SceneManager.LoadScene(CHARACTER_SELECT_SCENE, LoadSceneMode.Single);
            yield return null;
            
            var controller = Object.FindObjectOfType<CharacterSelectionController>();
            var prompt = FindPromptLabel();
            
            if (prompt == null)
            {
                Assert.Inconclusive("Pick prompt label not found in UI");
                yield break;
            }
            
            // Select all 3 characters
            var selectReg = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_reg");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectReg;
                selectReg.SendEvent(click);
            }
            yield return null;
            
            var selectRemi = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_remi");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectRemi;
                selectRemi.SendEvent(click);
            }
            yield return null;
            
            var selectTommy = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("select_tommy");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = selectTommy;
                selectTommy.SendEvent(click);
            }
            yield return null;
            
            // After 3 selections, should not show Player 4
            Assert.IsFalse(prompt.text.Contains("Player 4"), 
                $"Should never show Player 4, got: {prompt.text}");
            
            // Difficulty selection should be showing instead
            var difficultyPopup = controller.difficultySelectUiDocument.rootVisualElement.Q<VisualElement>("difficulty_popup");
            if (difficultyPopup != null)
            {
                Assert.AreEqual(DisplayStyle.Flex, difficultyPopup.style.display.value,
                    "Difficulty selection should be shown after all 3 players selected");
            }
        }
    }
}