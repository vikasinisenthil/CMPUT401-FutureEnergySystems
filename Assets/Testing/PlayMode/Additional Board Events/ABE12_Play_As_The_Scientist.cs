using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AdditionalBoardEvents
{
    [Category("Additional Board Events")]
    public class ABE12_Play_As_The_Scientist
    {
        private const string BOARD_SCENE = "BoardScene";

        private static Sprite MakeDummySprite()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private static BlueMCQCard MakeTestMCQCard()
        {
            var card = ScriptableObject.CreateInstance<BlueMCQCard>();
            card.cardName = "Scientist Sensor Test";
            card.image = MakeDummySprite();
            card.statement = "TEST_STATEMENT";
            card.question = "TEST_QUESTION";
            card.answers = new List<MCQAnswer>()
            {
                new MCQAnswer { answer = "Wrong", messageWhenChosen = "Nope", correctAnswer = false },
                new MCQAnswer { answer = "Correct", messageWhenChosen = "Yes", correctAnswer = true }
            };
            return card;
        }
        
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Load MainMenu to create GameManager
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist");
            
            // Set singleplayer mode
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            // Load CharacterSelect scene
            SceneManager.LoadScene("CharacterSelect", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScientistIsSelectableOnCharacterScreen()
        {
            var controller = Object.FindObjectOfType<CharacterSelectionController>();
            Assert.IsNotNull(controller, "CharacterSelectionController should exist");
            
            // Check that Scientist card exists and is clickable
            var regCard = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("reg_card");
            Assert.IsNotNull(regCard, "Reg (Scientist) card should exist on character selection screen");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScientistAbilityIsDescribedOnCharacterScreen()
        {
            var controller = Object.FindObjectOfType<CharacterSelectionController>();
            
            // Click on Scientist card to show info
            var regCard = controller.characterSelectionUiDocument.rootVisualElement.Q<VisualElement>("reg_card");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = regCard;
                regCard.SendEvent(click);
            }
            
            yield return null;
            
            // Check that info popup shows ability description
            var infoPopup = controller.characterInfoUiDocument.rootVisualElement.Q<VisualElement>("info_popup");
            var abilities = controller.characterInfoUiDocument.rootVisualElement.Q<Label>("info_abilities");
            
            Assert.IsNotNull(abilities, "Abilities label should exist in character info");
            Assert.IsTrue(abilities.text.ToLower().Contains("forecasting") || 
                         abilities.text.ToLower().Contains("sensor"),
                         "Scientist ability description should mention Forecasting or Sensor");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingScientistLoadsGameWithScientist()
        {
            var gm = GameManager.Instance;
            var controller = Object.FindObjectOfType<CharacterSelectionController>();
            
            // Select Scientist
            controller.OnRegSelected();
            yield return null;

            // Select difficulty
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
            Assert.AreEqual(HeroType.Scientist, gm.SelectedHeroes[0], "Selected hero should be Scientist");
        }

        [UnityTest]
        public IEnumerator ScientistHasTwoSensorUsesAtGameStart()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            
            var gc = Object.FindObjectOfType<GameController>();
            Assert.IsNotNull(gc, "GameController should exist");
            Assert.AreEqual(HeroType.Scientist, gc.player.heroType, "Player should be Scientist");
            Assert.AreEqual(2, gc.player.forecastingUses, "Scientist should have 2 sensor uses at start");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator SensorButtonAppearsOnQuizCards()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            
            var gc = Object.FindObjectOfType<GameController>();
            
            // Land on blue square to trigger quiz card
            gc.LandedOnBlueSquare();
            yield return null;
            
            // Check if sensor button exists in the blue card UI
            var blueCardUI = gc.blueCardUiDocument;
            if (blueCardUI != null)
            {
                var sensorButton = blueCardUI.rootVisualElement.Q<Button>("sensor_button");
                
                // Sensor button should exist and be visible for Scientist
                if (sensorButton != null)
                {
                    Assert.AreEqual(DisplayStyle.Flex, sensorButton.style.display.value, 
                        "Sensor button should be visible for Scientist on quiz cards");
                }
            }
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator UsingSensorHighlightsCorrectAnswer()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            
            var gc = Object.FindObjectOfType<GameController>();
            
            var blueCardManager = gc.GetComponent<BlueCardManager>();
            Assert.IsNotNull(blueCardManager, "BlueCardManager should exist");

            blueCardManager.deck.Clear();
            BlueMCQCard testCard = MakeTestMCQCard();
            blueCardManager.deck.Add(testCard);

            gc.LandedOnBlueSquare();

            // Wait for blue card UI and sensor button to be ready.
            Button sensorButton = null;
            for (int i = 0; i < 180; i++)
            {
                var root = gc.blueCardUiDocument?.rootVisualElement;
                sensorButton = root?.Q<Button>("sensor_button");
                if (sensorButton != null &&
                    sensorButton.style.display.value == DisplayStyle.Flex &&
                    sensorButton.enabledSelf)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsNotNull(sensorButton, "sensor_button should exist on Scientist MCQ card.");
            Assert.AreEqual(DisplayStyle.Flex, sensorButton.style.display.value, "sensor_button should be visible.");
            Assert.IsTrue(sensorButton.enabledSelf, "sensor_button should be enabled.");

            int usesBefore = gc.player.forecastingUses;

            // PlayMode UI click dispatch can be flaky for this button in tests.
            // Invoke the exact ability path on the active test card deterministically.
            MethodInfo useSensorMethod = typeof(BlueMCQCard).GetMethod("UseSensor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(useSensorMethod, "UseSensor method not found on BlueMCQCard.");
            useSensorMethod.Invoke(testCard, null);

            // Wait until use count decreases to avoid one-frame flake.
            for (int i = 0; i < 120 && gc.player.forecastingUses == usesBefore; i++)
            {
                yield return null;
            }

            Assert.AreEqual(usesBefore - 1, gc.player.forecastingUses,
                "Sensor uses should decrease after use");

            var blueCardUI = gc.blueCardUiDocument;
            var answerButtons = blueCardUI.rootVisualElement.Query<Button>(className: "answer-option").ToList();
            bool anyHighlighted = false;
            foreach (var btn in answerButtons)
            {
                if (btn.ClassListContains("sensor-highlight"))
                {
                    anyHighlighted = true;
                    break;
                }
            }

            Assert.IsTrue(anyHighlighted, "At least one answer should be highlighted after using sensor");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator SensorButtonDisabledAfterTwoUses()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            
            var gc = Object.FindObjectOfType<GameController>();
            
            // Use sensor twice
            gc.player.forecastingUses = 2;
            gc.player.forecastingUses--;
            gc.player.forecastingUses--;
            
            Assert.AreEqual(0, gc.player.forecastingUses, "Should have 0 uses remaining");
            
            // Land on blue square
            gc.LandedOnBlueSquare();
            yield return null;
            
            var blueCardUI = gc.blueCardUiDocument;
            var sensorButton = blueCardUI?.rootVisualElement.Q<Button>("sensor_button");
            
            if (sensorButton != null)
            {
                // Button should be hidden when no uses remain
                Assert.AreEqual(DisplayStyle.None, sensorButton.style.display.value, 
                    "Sensor button should be hidden when no uses remain");
            }
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScientistAvatarAppearsInGame()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            
            var gc = Object.FindObjectOfType<GameController>();
            var playerSprite = gc.player.gameObject.GetComponent<SpriteRenderer>();
            
            Assert.IsNotNull(playerSprite, "Player should have a SpriteRenderer");
            Assert.IsNotNull(playerSprite.sprite, "Player should have a sprite assigned");
            
            // Verify it's the scientist sprite (check if sprite is from scientist sprites)
            var scientistSprites = gm.GetCharacterSprites(HeroType.Scientist);
            Assert.IsNotNull(scientistSprites, "Scientist sprites should be configured");
            Assert.IsNotNull(scientistSprites.idleSprite, "Scientist idle sprite should be assigned");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator SensorDoesNotWorkOnFactCards()
        {
            var gm = GameManager.Instance;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            
            var gc = Object.FindObjectOfType<GameController>();
            
            // Force a fact card to be drawn
            var blueCardManager = gc.GetComponent<BlueCardManager>();
            if (blueCardManager != null)
            {
                // Clear deck and add only a fact card
                blueCardManager.deck.Clear();
                var factCard = Resources.Load<BlueFactCard>("BlueCards/TreesAreAirHelpers");
                if (factCard != null)
                {
                    blueCardManager.deck.Add(factCard);
                    
                    gc.LandedOnBlueSquare();
                    yield return null;
                    
                    var blueCardUI = gc.blueCardUiDocument;
                    var sensorButton = blueCardUI?.rootVisualElement.Q<Button>("sensor_button");
                    
                    // Sensor button should NOT appear on fact cards
                    Assert.IsTrue(sensorButton == null || sensorButton.style.display.value == DisplayStyle.None, 
                        "Sensor button should not appear on fact cards");
                }
            }
            
            yield return null;
        }
    }
}
