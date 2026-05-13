using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>
/// MG.07 — Save Energy (turn-off-lights minigame).
///
/// Acceptance criteria:
/// 1. User gains 1 point for each second a light is left on.
/// 2. The light turns off whenever the user presses it.
/// 3. Randomly, the lights are turned on by NPCs.
/// 4. If the user has 25 or more points at the end, they gain 1 pollution point.
/// 5. If the user has less than 25 points at the end, they lose 1 pollution point.
/// </summary>
namespace Minigames
{
    [Category("Minigames")]
    public class MG07_Save_Energy
    {
        private static void Click(VisualElement target)
        {
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = target;
                target.SendEvent(click);
            }
        }

        private static IEnumerator BeginRound(SaveEnergyMinigame mg)
        {
            Button begin = mg.uiDocument.rootVisualElement.Q<Button>("begin_button");
            Assert.NotNull(begin, "begin_button should exist");
            Click(begin);
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.Mode = GameMode.Singleplayer;
                gm.PlayerCount = 1;
                gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            }

            yield return null;
        }

        // ── AC: Scene presence ───────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SaveEnergyMinigameExistsInScene()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg, "SaveEnergyMinigame should exist in BoardScene");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MinigameNameContainsEnergy()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);
            Assert.True(
                mg.MinigameName.Contains("Energy") || mg.MinigameName.Contains("Save"),
                $"Expected name about energy / saving, got: {mg.MinigameName}");
            yield return null;
        }

        // ── AC 2: Lights start off, turn off when pressed ────────────────────

        [UnityTest]
        public IEnumerator LightsStartOff()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            for (int i = 0; i < 6; i++)
                Assert.False(mg.IsLightOn(i), $"light_{i} should be OFF at game start");

            yield return null;
        }

        /// <summary>AC 2: The light turns off whenever the user presses it.</summary>
        [UnityTest]
        public IEnumerator ClickingLitLightTurnsItOff()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            // Force NPC to turn on a light
            mg.ForceActivateNpc();
            yield return null;

            // Find the first light that is now ON
            int litIndex = -1;
            for (int i = 0; i < 6; i++)
            {
                if (mg.IsLightOn(i)) { litIndex = i; break; }
            }

            Assert.AreNotEqual(-1, litIndex, "At least one light should be ON after NPC activation");

            // Click the lit light
            VisualElement light = mg.uiDocument.rootVisualElement.Q<VisualElement>($"light_{litIndex}");
            Assert.NotNull(light, $"light_{litIndex} element should exist in UI");

            Click(light);
            yield return null;

            Assert.False(mg.IsLightOn(litIndex), $"light_{litIndex} should be OFF after player click");
            yield return null;
        }

        // ── AC 3: NPCs randomly turn on lights ───────────────────────────────

        /// <summary>AC 3: Randomly, the lights are turned on by NPCs.</summary>
        [UnityTest]
        public IEnumerator NpcCanTurnLightOn()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            Assert.AreEqual(0, mg.GetLitCount(), "No lights should be on before NPC activation");

            mg.ForceActivateNpc();
            yield return null;

            Assert.Greater(mg.GetLitCount(), 0, "At least one light should be ON after NPC activation");
            yield return null;
        }

        // ── AC 1: Points accumulate while lights are on ──────────────────────

        /// <summary>AC 1: The user gains 1 point for each second a light is left on.</summary>
        [UnityTest]
        public IEnumerator PointsAccumulateWhileLightsAreOn()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            Assert.AreEqual(0f, mg.GetPoints(), 0.01f, "Points should start at zero");

            // Turn on a light via NPC, then wait 1 real second for accumulation
            mg.ForceActivateNpc();
            yield return new WaitForSeconds(1f);

            Assert.Greater(mg.GetPoints(), 0f, "Points should accumulate while a light is on");
            yield return null;
        }

        // ── AC 4 & 5: Pollution reduction tiers ──────────────────────────────

        /// <summary>
        /// AC 4: 25 or more pts → gain 1 pollution (returns -1).
        /// AC 5: Less than 25 pts → lose 1 pollution (returns 1).
        /// </summary>
        [UnityTest]
        public IEnumerator PollutionReductionTiersMatchSpec()
        {
            // Over threshold → pollution increases (return -1)
            Assert.AreEqual(
                -1,
                SaveEnergyMinigame.ComputePollutionReductionForPoints(30f, 25f),
                "30 pts (>25 threshold) should return -1 (gain pollution)");

            // Exactly at threshold → counts as over (>= threshold fails)
            Assert.AreEqual(
                -1,
                SaveEnergyMinigame.ComputePollutionReductionForPoints(25f, 25f),
                "25 pts (= threshold) should return -1 (gain pollution)");

            // Under threshold → pollution decreases (return 1)
            Assert.AreEqual(
                1,
                SaveEnergyMinigame.ComputePollutionReductionForPoints(20f, 25f),
                "20 pts (<25 threshold) should return 1 (lose pollution)");

            Assert.AreEqual(
                1,
                SaveEnergyMinigame.ComputePollutionReductionForPoints(0f, 25f),
                "0 pts should return 1 (lose pollution)");

            yield return null;
        }

        // ── UI structure ──────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator AllSixLightElementsExistInUI()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            var root = mg.uiDocument.rootVisualElement;
            for (int i = 0; i < 6; i++)
            {
                VisualElement light = root.Q<VisualElement>($"light_{i}");
                Assert.NotNull(light, $"light_{i} should exist in the UI");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DescriptionMentionsLightsOrEnergy()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            Label desc = mg.uiDocument.rootVisualElement.Q<Label>("minigame_description");
            Assert.NotNull(desc, "minigame_description label should exist");

            string t = desc.text.ToLower();
            Assert.True(
                t.Contains("light") || t.Contains("energy") || t.Contains("turn off"),
                "Instructions should mention lights or energy");

            yield return null;
        }

        [UnityTest]
        public IEnumerator EarlyExitFromIntroFiresExitedNotComplete()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);

            bool exited = false;
            bool completed = false;
            mg.OnMinigameExited += () => exited = true;
            mg.OnMinigameComplete += _ => completed = true;

            mg.StartMinigame();
            yield return null;

            Button exit = mg.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist on intro page");
            Click(exit);
            yield return null;

            Assert.IsTrue(exited, "Early intro exit should fire OnMinigameExited.");
            Assert.IsFalse(completed, "Early intro exit should not fire OnMinigameComplete.");
        }

        [UnityTest]
        public IEnumerator EndPageExitFiresCompleteAfterTimerEnds()
        {
            SaveEnergyMinigame mg = Object.FindFirstObjectByType<SaveEnergyMinigame>();
            Assert.NotNull(mg);

            bool completed = false;
            int completionValue = int.MinValue;
            bool exited = false;
            mg.OnMinigameComplete += value =>
            {
                completed = true;
                completionValue = value;
            };
            mg.OnMinigameExited += () => exited = true;

            mg.timeLimit = 0.1f;
            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);
            yield return new WaitForSeconds(0.2f);

            Button exit = mg.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist on end page");
            Click(exit);
            yield return null;

            Assert.IsTrue(completed, "End-page exit after timer should fire OnMinigameComplete.");
            Assert.IsFalse(exited, "End-page exit should not fire OnMinigameExited.");
            Assert.AreNotEqual(int.MinValue, completionValue, "Completion value should be provided.");
        }
    }
}
