using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>
/// MG.04 — Clean Energy (choose the correct clean energy source minigame).
///
/// Acceptance criteria:
/// 1. The user is prompted with three energy sources.
/// 2. The user may select an energy source by clicking on it.
/// 3. The user is rewarded if they select the clean source.
/// 4. There is a brief animation and sound on success or failure.
/// 5. The user loses pollution if they succeed (returns 1 → AddScore(-1)).
/// 6. The user gains pollution if they fail (returns -1 → AddScore(+1)).
/// </summary>
namespace Minigames
{
    [Category("Minigames")]
    public class MG04_Clean_Energy
    {
        private static void Click(VisualElement target)
        {
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = target;
                target.SendEvent(click);
            }
        }

        private static IEnumerator BeginRound(CleanEnergyMinigame mg)
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

        // ── AC 1: Scene presence & three choices ─────────────────────────────

        [UnityTest]
        public IEnumerator CleanEnergyMinigameExistsInScene()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg, "CleanEnergyMinigame should exist in BoardScene");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MinigameNameContainsEnergy()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg);
            Assert.True(
                mg.MinigameName.Contains("Energy") || mg.MinigameName.Contains("Clean"),
                $"Expected name about clean energy, got: {mg.MinigameName}");
            yield return null;
        }

        /// <summary>AC 1: The user is prompted with three energy sources.</summary>
        [UnityTest]
        public IEnumerator ThreeChoiceButtonsExistAfterStart()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            var root = mg.uiDocument.rootVisualElement;

            for (int i = 0; i < 3; i++)
            {
                Button btn = root.Q<Button>($"choice_{i}");
                Assert.NotNull(btn, $"choice_{i} button should exist in UI");

                // Each button should have a non-empty label
                Label lbl = root.Q<Label>($"choice_label_{i}");
                Assert.NotNull(lbl, $"choice_label_{i} should exist");
                Assert.False(string.IsNullOrEmpty(lbl.text), $"choice_label_{i} should have text");
            }

            yield return null;
        }

        // ── AC 3: Exactly one choice is the clean source ──────────────────────

        /// <summary>AC 3: The user is rewarded if they select the clean source.</summary>
        [UnityTest]
        public IEnumerator ExactlyOneChoiceIsCorrect()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            int correctCount = 0;
            for (int i = 0; i < 3; i++)
                if (mg.IsChoiceCorrect(i)) correctCount++;

            Assert.AreEqual(1, correctCount, "Exactly one of the three choices should be the correct clean source");

            // Verify the reported correct index matches IsChoiceCorrect
            int reported = mg.GetCorrectChoiceIndex();
            Assert.True(mg.IsChoiceCorrect(reported), "GetCorrectChoiceIndex() should match IsChoiceCorrect()");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ChoiceNamesAreNonEmpty()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            for (int i = 0; i < 3; i++)
            {
                string name = mg.GetChoiceName(i);
                Assert.False(string.IsNullOrEmpty(name), $"Choice {i} should have a non-empty name");
            }

            yield return null;
        }

        // ── AC 2 & 5: Clicking correct choice fires complete with positive result

        /// <summary>
        /// AC 2 & 5: Selecting the clean source and exiting from the end page
        /// fires OnMinigameComplete with a positive pollution reduction.
        /// </summary>
        [UnityTest]
        public IEnumerator ClickingCorrectChoiceFiresCompleteWithPositiveReduction()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            int resultValue = int.MinValue;
            mg.OnMinigameComplete += val => resultValue = val;

            int correctIdx = mg.GetCorrectChoiceIndex();
            Button correctBtn = mg.uiDocument.rootVisualElement.Q<Button>($"choice_{correctIdx}");
            Assert.NotNull(correctBtn, $"choice_{correctIdx} button should exist");

            Click(correctBtn);
            yield return null;

            Button exit = mg.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist on end page");
            Click(exit);
            yield return null;

            Assert.AreNotEqual(int.MinValue, resultValue, "OnMinigameComplete should have fired");
            Assert.Greater(resultValue, 0, "Correct choice should return positive pollution reduction (lose pollution)");
        }

        // ── AC 2 & 6: Clicking wrong choice fires complete with negative result

        /// <summary>
        /// AC 2 & 6: Selecting a dirty source and exiting from the end page
        /// fires OnMinigameComplete with a negative pollution reduction.
        /// </summary>
        [UnityTest]
        public IEnumerator ClickingWrongChoiceFiresCompleteWithNegativeReduction()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            int resultValue = int.MinValue;
            mg.OnMinigameComplete += val => resultValue = val;

            // Find the first wrong choice index
            int wrongIdx = -1;
            for (int i = 0; i < 3; i++)
            {
                if (!mg.IsChoiceCorrect(i)) { wrongIdx = i; break; }
            }
            Assert.AreNotEqual(-1, wrongIdx, "Should find at least one wrong choice");

            Button wrongBtn = mg.uiDocument.rootVisualElement.Q<Button>($"choice_{wrongIdx}");
            Assert.NotNull(wrongBtn, $"choice_{wrongIdx} button should exist");

            Click(wrongBtn);
            yield return null;

            Button exit = mg.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist on end page");
            Click(exit);
            yield return null;

            Assert.AreNotEqual(int.MinValue, resultValue, "OnMinigameComplete should have fired");
            Assert.Less(resultValue, 0, "Wrong choice should return negative pollution reduction (gain pollution)");
        }

        // ── AC 4: Result label shown after selection ──────────────────────────

        /// <summary>AC 4: There is a brief animation and result shown on success or failure.</summary>
        [UnityTest]
        public IEnumerator ResultLabelAppearsAfterChoice()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            // Click any choice
            Button btn = mg.uiDocument.rootVisualElement.Q<Button>("choice_0");
            Assert.NotNull(btn);

            Click(btn);
            yield return null;

            Label result = mg.uiDocument.rootVisualElement.Q<Label>("result_label");
            Assert.NotNull(result, "result_label should exist");
            Assert.AreNotEqual(DisplayStyle.None, result.style.display.value, "result_label should be visible after a choice");
            Assert.False(string.IsNullOrEmpty(result.text), "result_label should have feedback text");

            yield return null;
        }

        // ── Exit button ───────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ExitButtonStopsMinigame()
        {
            CleanEnergyMinigame mg = Object.FindFirstObjectByType<CleanEnergyMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;

            Button exit = mg.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist in UI");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = exit;
                exit.SendEvent(click);
            }
            yield return null;

            Assert.False(mg.IsActive, "Minigame should be inactive after exit");
            yield return null;
        }
    }
}
