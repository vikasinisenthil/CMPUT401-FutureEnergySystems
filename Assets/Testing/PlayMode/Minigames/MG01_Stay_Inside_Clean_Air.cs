using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>
/// MG.01 — Stay Inside &amp; Clean the Air (wind/smoke clearing minigame on BoardSquare6).
/// </summary>
namespace Minigames {
    [Category("Minigames")]
    public class MG01_Stay_Inside_Clean_Air
    {
        private static void Click(VisualElement target)
        {
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = target;
                target.SendEvent(click);
            }
        }

        private static void TapAt(StayInsideCleanAirMinigame mg, Vector2 panelPosition)
        {
            MethodInfo tapMethod = typeof(StayInsideCleanAirMinigame).GetMethod(
                "TryTapSmokeAtPosition",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(tapMethod, "TryTapSmokeAtPosition should exist for tap processing.");
            tapMethod.Invoke(mg, new object[] { panelPosition });
        }

        private static IEnumerator BeginRound(StayInsideCleanAirMinigame mg)
        {
            Button begin = mg.uiDocument.rootVisualElement.Q<Button>("begin_button");
            Assert.NotNull(begin, "begin_button should exist");
            Click(begin);
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Load MainMenu first to create GameManager
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager should exist after loading MainMenu");
            
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            gm.difficulty = Difficulty.Easy;

            yield return null;

            // Now load BoardScene
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator StayInsideMinigameExistsInScene()
        {
            StayInsideCleanAirMinigame mg = Object.FindFirstObjectByType<StayInsideCleanAirMinigame>();
            Assert.NotNull(mg, "StayInsideCleanAirMinigame should exist in BoardScene");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MinigameNameMatchesUserStory()
        {
            StayInsideCleanAirMinigame mg = Object.FindFirstObjectByType<StayInsideCleanAirMinigame>();
            Assert.NotNull(mg);
            Assert.True(
                mg.MinigameName.Contains("Stay Inside") || mg.MinigameName.Contains("Clean"),
                $"Expected title about staying inside / clean air, got: {mg.MinigameName}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TappingSmokeCloudReducesSmoke()
        {
            StayInsideCleanAirMinigame mg = Object.FindFirstObjectByType<StayInsideCleanAirMinigame>();
            Assert.NotNull(mg);
            mg.tapsPerCloudToClear = 2;
            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            Assert.AreEqual(1f, mg.GetSmokeDensity(), 0.001f, "Smoke should start full");

            VisualElement root = mg.uiDocument.rootVisualElement;
            VisualElement playArea = mg.uiDocument.rootVisualElement.Q<VisualElement>("room_frame");
            Assert.NotNull(playArea);

            Image puff = root.Q<Image>(className: "sica-smoke-puff-instance");
            Assert.NotNull(puff, "At least one smoke puff should exist");
            Vector2 puffCenter = puff.worldBound.center;

            TapAt(mg, puffCenter);
            yield return null;
            TapAt(mg, puffCenter);
            yield return null;

            Assert.Less(mg.GetSmokeDensity(), 1f, "Smoke should decrease after tapping a smoke cloud");
            Assert.Greater(mg.GetClearedPercent(), 0f, "Cleared percent should increase");

            yield return null;
        }

        [UnityTest]
        public IEnumerator BackgroundTapDoesNotReduceSmoke()
        {
            StayInsideCleanAirMinigame mg = Object.FindFirstObjectByType<StayInsideCleanAirMinigame>();
            Assert.NotNull(mg);
            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            Assert.AreEqual(1f, mg.GetSmokeDensity(), 0.001f, "Smoke should start full");

            VisualElement playArea = mg.uiDocument.rootVisualElement.Q<VisualElement>("room_frame");
            Assert.NotNull(playArea);

            Vector2 bottomCenter = new Vector2(playArea.worldBound.center.x, playArea.worldBound.yMax - 2f);
            TapAt(mg, bottomCenter);
            yield return null;

            Assert.AreEqual(1f, mg.GetSmokeDensity(), 0.001f, "Background tap should not clear smoke");
        }

        [UnityTest]
        public IEnumerator PollutionReductionTiersMatchSpec()
        {
            Assert.AreEqual(2, StayInsideCleanAirMinigame.ComputePollutionReductionForCleared(76f, 75f, 40f));
            Assert.AreEqual(1, StayInsideCleanAirMinigame.ComputePollutionReductionForCleared(50f, 75f, 40f));
            Assert.AreEqual(0, StayInsideCleanAirMinigame.ComputePollutionReductionForCleared(20f, 75f, 40f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExitButtonStopsMinigame()
        {
            StayInsideCleanAirMinigame mg = Object.FindFirstObjectByType<StayInsideCleanAirMinigame>();
            Assert.NotNull(mg);
            mg.StartMinigame();
            yield return null;

            Button exit = mg.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(exit);

            Click(exit);
            yield return null;

            Assert.False(mg.IsActive, "Minigame should be inactive after exit");
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndPageExitFiresCompleteAfterTimeUp()
        {
            StayInsideCleanAirMinigame mg = Object.FindFirstObjectByType<StayInsideCleanAirMinigame>();
            Assert.NotNull(mg);

            mg.timeLimit = 0.1f;

            int completionValue = int.MinValue;
            bool completed = false;
            bool exited = false;
            mg.OnMinigameComplete += value =>
            {
                completionValue = value;
                completed = true;
            };
            mg.OnMinigameExited += () => exited = true;

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);
            yield return new WaitForSeconds(0.2f);

            Button exit = mg.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist on end page");
            Click(exit);
            yield return null;

            Assert.IsTrue(completed, "OnMinigameComplete should fire when exiting end page.");
            Assert.IsFalse(exited, "OnMinigameExited should not fire from end-page exit.");
            Assert.AreNotEqual(int.MinValue, completionValue, "Completion value should be reported.");
        }

        [UnityTest]
        public IEnumerator InstructionsMentionWindOrSwipe()
        {
            StayInsideCleanAirMinigame mg = Object.FindFirstObjectByType<StayInsideCleanAirMinigame>();
            Assert.NotNull(mg);
            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            Label desc = mg.uiDocument.rootVisualElement.Q<Label>("minigame_description");
            Assert.NotNull(desc);
            string t = desc.text.ToLower();
            Assert.True(
                t.Contains("wind") || t.Contains("swipe") || t.Contains("tap") || t.Contains("blow"),
                "Instructions should describe wind-like interaction");

            yield return null;
        }

        [UnityTest]
        public IEnumerator SmokeCloudsContainerAndPuffsExist()
        {
            StayInsideCleanAirMinigame mg = Object.FindFirstObjectByType<StayInsideCleanAirMinigame>();
            Assert.NotNull(mg);
            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            var root = mg.uiDocument.rootVisualElement;
            Assert.NotNull(root.Q<VisualElement>("smoke_clouds_container"));
            Assert.NotNull(root.Q<VisualElement>("room_frame"));
            Assert.GreaterOrEqual(mg.SmokePuffInstanceCount, StayInsideCleanAirMinigame.SmokePuffCountMin);
            Assert.LessOrEqual(mg.SmokePuffInstanceCount, StayInsideCleanAirMinigame.SmokePuffCountMax);
            yield return null;
        }
    }
}
