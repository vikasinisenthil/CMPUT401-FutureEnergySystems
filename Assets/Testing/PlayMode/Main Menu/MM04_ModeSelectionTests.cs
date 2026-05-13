using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace MainMenu 
{
    [Category("Main Menu")]
    public class ModeSelectionTests
    {
        private const string MAIN_MENU = "MainMenu";
        private const string CHARACTER_SELECT = "CharacterSelect";

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene(MAIN_MENU, LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private GameManager GetGameManager()
        {
            return GameManager.Instance;
        }

        private Button FindButtonByName(string buttonName)
        {
            var gm = GetGameManager();
            if (gm == null || gm.mainMenuUiDocument == null) return null;
            
            return gm.mainMenuUiDocument.rootVisualElement.Q<Button>(buttonName);
        }

        [UnityTest]
        public IEnumerator MainMenuHasSingleAndMultiButtons()
        {
            var singleButton = FindButtonByName("singleplayer_button");
            var multiButton = FindButtonByName("threeplayer_button");
            
            Assert.IsNotNull(singleButton, "Missing singleplayer_button in MainMenu UI.");
            Assert.IsNotNull(multiButton, "Missing multiplayer_button in MainMenu UI.");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChoosingSingleplayerSetsModeAndStartsSingleSetup()
        {
            var gm = GetGameManager();
            Assert.IsNotNull(gm, "GameManager not found.");
            
            var singleButton = FindButtonByName("singleplayer_button");
            Assert.IsNotNull(singleButton, "Singleplayer button not found");
            
            // Simulate button click
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = singleButton;
                singleButton.SendEvent(click);
            }
            
            yield return null;
            
            Assert.AreEqual(GameMode.Singleplayer, gm.Mode, "Mode should be Singleplayer");
            Assert.AreEqual(1, gm.PlayerCount, "PlayerCount should be 1");
            Assert.AreEqual(0, gm.CurrentPickIndex, "CurrentPickIndex should be 0");
            Assert.AreEqual(CHARACTER_SELECT, SceneManager.GetActiveScene().name, "Should navigate to CharacterSelect");
        }

        [UnityTest]
        public IEnumerator ChoosingMultiplayerSetsModeAndStartsMultiSetup()
        {
            var gm = GetGameManager();
            Assert.IsNotNull(gm, "GameManager not found.");
            
            var multiButton = FindButtonByName("threeplayer_button");
            Assert.IsNotNull(multiButton, "Multiplayer button not found");
            
            // Simulate button click
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = multiButton;
                multiButton.SendEvent(click);
            }
            
            yield return null;
            
            Assert.AreEqual(GameMode.Multiplayer, gm.Mode, "Mode should be Multiplayer");
            Assert.AreEqual(3, gm.PlayerCount, "PlayerCount should be 3");
            Assert.AreEqual(0, gm.CurrentPickIndex, "CurrentPickIndex should be 0");
            Assert.AreEqual(CHARACTER_SELECT, SceneManager.GetActiveScene().name, "Should navigate to CharacterSelect");
        }

        [UnityTest]
        public IEnumerator ReturningToMainMenuAllowsChoosingDifferentMode()
        {
            var gm = GetGameManager();
            Assert.IsNotNull(gm, "GameManager not found.");
            
            // First choose multiplayer
            var multiButton = FindButtonByName("threeplayer_button");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = multiButton;
                multiButton.SendEvent(click);
            }
            
            yield return null;
            Assert.AreEqual(GameMode.Multiplayer, gm.Mode, "Mode should be Multiplayer");
            
            // Go back to main menu
            SceneManager.LoadScene(MAIN_MENU);
            yield return null;
            yield return null;
            
            // Now choose singleplayer
            var singleButton = FindButtonByName("singleplayer_button");
            Assert.IsNotNull(singleButton, "Singleplayer button should exist after returning to main menu");
            
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = singleButton;
                singleButton.SendEvent(click);
            }
            
            yield return null;
            
            Assert.AreEqual(GameMode.Singleplayer, gm.Mode, "Mode should change to Singleplayer");
            Assert.AreEqual(1, gm.PlayerCount, "PlayerCount should be 1");
        }
    }
}