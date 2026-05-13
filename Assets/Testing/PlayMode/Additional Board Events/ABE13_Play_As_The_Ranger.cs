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
    public class ABE13_Play_As_The_Ranger
    {
        private readonly List<Object> spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in spawned)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            spawned.Clear();

            foreach (var panel in Resources.FindObjectsOfTypeAll<PanelSettings>())
            {
                if (panel != null && panel.name.StartsWith("TestPanelSettings_"))
                    Object.DestroyImmediate(panel);
            }
        }

        [Test]
        public void ABE13_1_PlayerCanChooseRangerBeforeGameStarts()
        {
            CreateAudioManager();
            var gm = CreateGameManager();

            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[1];
            gm.CurrentPickIndex = 0;

            var selectionDoc = CreateUIDocument("CharacterSelectionDoc");
            var infoDoc = CreateUIDocument("CharacterInfoDoc");
            var difficultyDoc = CreateUIDocument("DifficultyDoc");

            BuildCharacterSelectionUI(selectionDoc.rootVisualElement);
            BuildCharacterInfoUI(infoDoc.rootVisualElement);
            BuildDifficultyUI(difficultyDoc.rootVisualElement);

            var go = new GameObject("CharacterSelectionController");
            spawned.Add(go);

            var controller = go.AddComponent<CharacterSelectionController>();
            controller.characterSelectionUiDocument = selectionDoc;
            controller.characterInfoUiDocument = infoDoc;
            controller.difficultySelectUiDocument = difficultyDoc;
            controller.remiBlue = MakeTexture(Color.blue);
            controller.regRed = MakeTexture(Color.red);
            controller.tommyYellow = MakeTexture(Color.yellow);

            controller.SendMessage("Start");

            controller.OnTommySelected();

            Assert.AreEqual(HeroType.Ranger, gm.SelectedHeroes[0]);
        }

        [Test]
        public void ABE13_2_RangerTakesNoPollutionDamageOnWildfireGreySpot()
        {
            CreateAudioManager();
            CreateGameManager().SelectedHeroes = new[] { HeroType.Ranger };

            var scoreManager = CreateScoreManagerWithUI(HeroType.Ranger);
            scoreManager.SetScore(3);

            CreateBlueCardDocumentsForGameControllerStart();
            var gc = CreateGameControllerWithUI(HeroType.Ranger);

            gc.LandedOnGraySquare(GreySpotType.Wildfire);
            TriggerGrayClose(gc);

            Assert.AreEqual(3, ScoreManager.Instance.GetScore());
        }

        [Test]
        public void ABE13_3_NonRangerStillTakesPollutionDamageOnWildfireGreySpot()
        {
            CreateAudioManager();
            CreateGameManager().SelectedHeroes = new[] { HeroType.Scientist };

            var scoreManager = CreateScoreManagerWithUI(HeroType.Scientist);
            scoreManager.SetScore(3);

            CreateBlueCardDocumentsForGameControllerStart();
            var gc = CreateGameControllerWithUI(HeroType.Scientist);

            gc.LandedOnGraySquare(GreySpotType.Wildfire);
            TriggerGrayClose(gc);

            Assert.AreEqual(4, ScoreManager.Instance.GetScore());
        }

        [Test]
        public void ABE13_4_RangerRemovesDoubleUsualPollutionOnPlantTreesGreenSpot()
        {
            CreateAudioManager();
            CreateGameManager().SelectedHeroes = new[] { HeroType.Ranger };

            var scoreManager = CreateScoreManagerWithUI(HeroType.Ranger);
            scoreManager.SetScore(5);

            CreateBlueCardDocumentsForGameControllerStart();
            var gc = CreateGameControllerWithUI(HeroType.Ranger);

            var plantTrees = new GameObject("PlantTrees");
            spawned.Add(plantTrees);

            SetPrivateField(gc, "currentGreenMinigameObject", plantTrees);
            InvokePrivateMethod(gc, "HandleMinigameCompleted", 1);

            Assert.AreEqual(3, ScoreManager.Instance.GetScore());
        }

        [Test]
        public void ABE13_5_RangerAbilitiesAreDescribedOnCharacterSelectionScreen()
        {
            CreateAudioManager();
            var gm = CreateGameManager();

            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[1];
            gm.CurrentPickIndex = 0;

            var selectionDoc = CreateUIDocument("CharacterSelectionDoc");
            var infoDoc = CreateUIDocument("CharacterInfoDoc");
            var difficultyDoc = CreateUIDocument("DifficultyDoc");

            BuildCharacterSelectionUI(selectionDoc.rootVisualElement);
            BuildCharacterInfoUI(infoDoc.rootVisualElement);
            BuildDifficultyUI(difficultyDoc.rootVisualElement);

            var go = new GameObject("CharacterSelectionController");
            spawned.Add(go);

            var controller = go.AddComponent<CharacterSelectionController>();
            controller.characterSelectionUiDocument = selectionDoc;
            controller.characterInfoUiDocument = infoDoc;
            controller.difficultySelectUiDocument = difficultyDoc;
            controller.remiBlue = MakeTexture(Color.blue);
            controller.regRed = MakeTexture(Color.red);
            controller.tommyYellow = MakeTexture(Color.yellow);

            controller.SendMessage("Start");

            controller.OnTommyClicked();

            var nameLabel = infoDoc.rootVisualElement.Q<Label>("info_name");
            var abilitiesLabel = infoDoc.rootVisualElement.Q<Label>("info_abilities");

            Assert.AreEqual("Tommy the Ranger", nameLabel.text);
            StringAssert.Contains("Fire Resistance", abilitiesLabel.text);
            StringAssert.Contains("Photosynthesis Boost", abilitiesLabel.text);
        }

        [UnityTest]
        public IEnumerator ABE13_6_DuringGameplayAvatarReflectsRanger()
        {
            // Load MainMenu first to create GameManager
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            
            // Set up for Ranger
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist");
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[] { HeroType.Ranger };
            
            // Load BoardScene
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;
            for (int i = 0; i < 5; i++) yield return null;

            var gc = Object.FindObjectOfType<GameController>();
            var scoreManager = ScoreManager.Instance;
            
            Assert.IsNotNull(gc, "GameController should exist");
            Assert.IsNotNull(scoreManager, "ScoreManager should exist");
            Assert.IsNotNull(scoreManager.inGameUiDocument, "ScoreManager UI Document should exist");
            
            // Query inside the score-1 container
            var scoreBox = scoreManager.inGameUiDocument.rootVisualElement.Q<VisualElement>("score-1");
            Assert.IsNotNull(scoreBox, "score-1 container should exist");
            
            var characterAvatar = scoreBox.Q<Image>("character_avatar");
            Assert.IsNotNull(characterAvatar, "Character avatar should exist");
            Assert.IsNotNull(characterAvatar.image, "Character avatar image should be set");
            
            // Verify it's the Ranger sprite
            var rangerSprites = gm.GetCharacterSprites(HeroType.Ranger, 0);
            Assert.IsNotNull(rangerSprites, "Ranger sprites should be configured");
            Assert.IsNotNull(rangerSprites.idleSprite, "Ranger idle sprite should be assigned");
            
            Assert.AreEqual(rangerSprites.idleSprite.texture, characterAvatar.image, 
                "Avatar should show Ranger sprite");
        }

        private GameManager CreateGameManager()
        {
            if (GameManager.Instance != null)
                return GameManager.Instance;

            var go = new GameObject("GameManager");
            spawned.Add(go);

            var gm = go.AddComponent<GameManager>();

            var rangerSprite = MakeSprite(MakeTexture(Color.yellow));
            gm.tommyYellowSprites = new CharacterSprites
            {
                idleSprite = rangerSprite,
                sickSprite = rangerSprite,
                walkSprites = new[] { rangerSprite }
            };

            var cyclistSprite = MakeSprite(MakeTexture(Color.blue));
            gm.remiBlueSprites = new CharacterSprites
            {
                idleSprite = cyclistSprite,
                sickSprite = cyclistSprite,
                walkSprites = new[] { cyclistSprite }
            };

            var scientistSprite = MakeSprite(MakeTexture(Color.red));
            gm.regRedSprites = new CharacterSprites
            {
                idleSprite = scientistSprite,
                sickSprite = scientistSprite,
                walkSprites = new[] { scientistSprite }
            };

            gm.SelectedHeroes = new[] { HeroType.Ranger };
            return gm;
        }

        private ScoreManager CreateScoreManagerWithUI(HeroType hero)
        {
            GameManager.Instance.SelectedHeroes = new[] { hero };

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.inGameUiDocument = CreateUIDocument("ScoreDoc");
                BuildInGameScoreUI(ScoreManager.Instance.inGameUiDocument.rootVisualElement);
                ScoreManager.Instance.SendMessage("Start");
                return ScoreManager.Instance;
            }

            var scoreDoc = CreateUIDocument("ScoreDoc");
            BuildInGameScoreUI(scoreDoc.rootVisualElement);

            var go = new GameObject("ScoreManager");
            spawned.Add(go);

            var scoreManager = go.AddComponent<ScoreManager>();
            scoreManager.inGameUiDocument = scoreDoc;
            scoreManager.SendMessage("Start");

            return scoreManager;
        }

        private GameController CreateGameControllerWithUI(HeroType hero)
        {
            GameManager.Instance.SelectedHeroes = new[] { hero };

            var go = new GameObject("GameController");
            spawned.Add(go);

            var gc = go.AddComponent<GameController>();

            gc.player = new Player
            {
                gameObject = new GameObject("Player"),
                heroType = hero
            };
            spawned.Add(gc.player.gameObject);

            gc.inGameUiDocument = CreateUIDocument("InGameDoc");
            gc.grayCardUiDocument = CreateUIDocument("GrayDoc");
            gc.blueCardUiDocument = CreateUIDocument("BlueDoc");

            BuildInGameControllerUI(gc.inGameUiDocument.rootVisualElement);
            BuildGrayCardUI(gc.grayCardUiDocument.rootVisualElement);
            BuildSimpleRootUI(gc.blueCardUiDocument.rootVisualElement);

            gc.boardSquares = new List<GameObject>();
            for (int i = 0; i < 40; i++)
            {
                var square = new GameObject($"BoardSquare_{i + 1}");
                spawned.Add(square);

                square.transform.position = new Vector3(i, 0, 0);

                var boardSquare = square.AddComponent<BoardSquare>();
                boardSquare.gameController = go;
                boardSquare.color = BoardSquareColor.GRAY;
                boardSquare.greySpotType = GreySpotType.Generic;

                gc.boardSquares.Add(square);
            }

            gc.SendMessage("Start");

            return gc;
        }

        private void CreateAudioManager()
        {
            if (AudioManager.Instance == null)
            {
                var audioGo = new GameObject("AudioManager");
                spawned.Add(audioGo);
                audioGo.AddComponent<AudioManager>();
            }

            if (Object.FindFirstObjectByType<AudioListener>() == null)
            {
                var listenerGo = new GameObject("TestAudioListener");
                spawned.Add(listenerGo);
                listenerGo.AddComponent<AudioListener>();
            }
        }

        private UIDocument CreateUIDocument(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "TestPanelSettings_" + name;
            spawned.Add(panelSettings);

            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            return doc;
        }

        private void CreateBlueCardDocumentsForGameControllerStart()
        {
            var blueFactDoc = CreateUIDocument("BlueFactCardUIDocument");
            BuildSimpleRootUI(blueFactDoc.rootVisualElement);

            var blueMcqDoc = CreateUIDocument("BlueMCQCardUIDocument");
            BuildSimpleRootUI(blueMcqDoc.rootVisualElement);
        }

        private void BuildCharacterSelectionUI(VisualElement root)
        {
            root.Clear();
            root.Add(new Label { name = "screen_title" });
            root.Add(new VisualElement { name = "remi_card" });
            root.Add(new VisualElement { name = "reg_card" });
            root.Add(new VisualElement { name = "tommy_card" });
            root.Add(new VisualElement { name = "back_button" });
            root.Add(new VisualElement { name = "select_remi" });
            root.Add(new VisualElement { name = "select_reg" });
            root.Add(new VisualElement { name = "select_tommy" });
        }

        private void BuildCharacterInfoUI(VisualElement root)
        {
            root.Clear();

            var popup = new VisualElement { name = "info_popup" };
            popup.Add(new Label { name = "info_name" });
            popup.Add(new Label { name = "info-subtitle" });
            popup.Add(new VisualElement { name = "info_image" });
            popup.Add(new Label { name = "info_description" });
            popup.Add(new Label { name = "info_abilities" });
            popup.Add(new Label { name = "info_best_for" });
            popup.Add(new Button { name = "close_info" });

            root.Add(popup);
        }

        private void BuildDifficultyUI(VisualElement root)
        {
            root.Clear();

            var popup = new VisualElement { name = "difficulty_popup" };
            popup.Add(new Button { name = "easy_button" });
            popup.Add(new Button { name = "medium_button" });
            popup.Add(new Button { name = "hard_button" });
            popup.Add(new Button { name = "back_button" });

            root.Add(popup);
        }

        private void BuildGrayCardUI(VisualElement root)
        {
            root.Clear();

            var overlay = new VisualElement { name = "overlay" };
            overlay.Add(new Button { name = "close_button" });
            root.Add(overlay);
        }

        private void BuildInGameControllerUI(VisualElement root)
        {
            root.Clear();
            root.Add(new VisualElement { name = "ui-container" });
            root.Add(new Button { name = "dice_button" });
        }

        private void BuildInGameScoreUI(VisualElement root)
        {
            root.Clear();
            
            var scoreBox = new VisualElement { name = "score-1" };
            scoreBox.AddToClassList("score-box");
            scoreBox.AddToClassList("score-1");
            
            var scoreContent = new VisualElement();
            scoreContent.AddToClassList("score-content");
            
            scoreContent.Add(new VisualElement { name = "progress_bar" });
            scoreContent.Add(new Label { name = "score_label" });
            
            scoreBox.Add(scoreContent);
            scoreBox.Add(new Image { name = "character_avatar" });
            
            root.Add(scoreBox);
        }

        private void BuildSimpleRootUI(VisualElement root)
        {
            root.Clear();
            root.Add(new VisualElement { name = "root_container" });
        }

        private Texture2D MakeTexture(Color color)
        {
            var tex = new Texture2D(8, 8);
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            spawned.Add(tex);
            return tex;
        }

        private Sprite MakeSprite(Texture2D tex)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            spawned.Add(sprite);
            return sprite;
        }

        private void TriggerGrayClose(GameController gameController)
        {
            var field = typeof(GameController).GetField(
                "grayCloseButtonCallback",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(field, "grayCloseButtonCallback field not found.");

            var callback = field.GetValue(gameController) as EventCallback<ClickEvent>;
            Assert.IsNotNull(callback, "grayCloseButtonCallback was null.");

            using var evt = ClickEvent.GetPooled();
            callback.Invoke(evt);
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found.");
            field.SetValue(target, value);
        }

        private void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method '{methodName}' not found.");
            method.Invoke(target, args);
        }
    }
}