using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>
/// MG.10 - Composting Minigame
/// Spec: Sort waste into Compost, Recycle, or Landfill before the timer runs out.
/// </summary>

namespace Minigames
{
    [Category("Minigames")]
    public class MG10_Composting
    {
        private static IEnumerator WaitForResultVisible(CompostingMinigame cm, int maxFrames = 120)
        {
            VisualElement root = cm.uiDocument.rootVisualElement;
            VisualElement result = root.Q<VisualElement>("result_container");
            Assert.NotNull(result, "result_container should exist.");

            for (int i = 0; i < maxFrames; i++)
            {
                if (result.style.display.value == DisplayStyle.Flex)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Timed out waiting for result_container to become visible.");
        }

        private static IEnumerator StartCompostingGameplay(CompostingMinigame cm)
        {
            cm.StartMinigame();
            yield return null;

            VisualElement root = cm.uiDocument.rootVisualElement;
            Button beginButton = root.Q<Button>("begin_button");
            Assert.NotNull(beginButton, "begin_button should exist.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = beginButton;
                beginButton.SendEvent(click);
            }

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

        // ─────────────────────────────────────────────────────────────────────
        // AT-01: Minigame exists in scene when a green square triggers it.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator CompostingMinigameExistsInScene()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist in the scene.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoardSquare45IsGreenAndConfiguredForCompostingMinigame()
        {
            GameObject square45 = GameObject.Find("BoardSquare45");
            Assert.NotNull(square45, "BoardSquare45 should exist in BoardScene.");

            BoardSquare boardSquare = square45.GetComponent<BoardSquare>();
            Assert.NotNull(boardSquare, "BoardSquare45 should have BoardSquare component.");
            Assert.AreEqual(BoardSquareColor.GREEN, boardSquare.color, "BoardSquare45 should be a green square.");
            Assert.NotNull(boardSquare.minigameObject, "BoardSquare45 should have an assigned minigame object.");
            Assert.NotNull(boardSquare.minigameObject.GetComponent<CompostingMinigame>(),
                "BoardSquare45 minigame object should be CompostingMinigame.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator LandingOnGreenSquareCanTriggerCompostingMinigame()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
            MinigameManager mm = GameObject.Find("GameController")?.GetComponent<MinigameManager>();
            Assert.NotNull(gc, "GameController should exist.");
            Assert.NotNull(mm, "MinigameManager should exist.");

            mm.minigameObjects.Clear();
            mm.minigameObjects.Add(cm.gameObject);

            gc.LandedOnGreenSquare();
            yield return null;

            Assert.IsTrue(mm.IsMinigameActive, "Landing on green square should activate configured Composting minigame.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-02: Player sees intro UI with Start and Exit.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator IntroUIElementsExist()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            cm.StartMinigame();
            yield return null;

            VisualElement root = cm.uiDocument.rootVisualElement;

            Assert.NotNull(root.Q<Label>("minigame_title"), "minigame_title should exist.");
            Assert.NotNull(root.Q<VisualElement>("intro_container"), "intro_container should exist.");
            Assert.NotNull(root.Q<Button>("begin_button"), "begin_button should exist.");
            Button exitButton = root.Q<Button>("exit_button");
            Assert.NotNull(exitButton, "exit_button should exist.");
            Assert.AreEqual(DisplayStyle.None, exitButton.resolvedStyle.display, "Intro should not show exit button.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-03: Pressing Start shows gameplay UI.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator StartButtonShowsGameplayUI()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            yield return StartCompostingGameplay(cm);

            VisualElement root = cm.uiDocument.rootVisualElement;

            Assert.NotNull(root.Q<VisualElement>("gameplay_container"), "gameplay_container should exist.");
            Assert.NotNull(root.Q<Label>("timer_label"), "timer_label should exist.");
            Assert.NotNull(root.Q<Label>("score_label"), "score_label should exist.");
            Assert.NotNull(root.Q<Label>("streak_label"), "streak_label should exist.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-04: Waste item and sorting buttons are shown during gameplay.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator GameplayShowsWasteItemAndBinButtons()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            yield return StartCompostingGameplay(cm);

            VisualElement root = cm.uiDocument.rootVisualElement;

            Assert.NotNull(root.Q<Label>("item_label"), "item_label should exist.");
            Assert.NotNull(root.Q<Label>("item_emoji_label"), "item_emoji_label should exist.");
            Assert.NotNull(root.Q<Button>("compost_bin_button"), "compost_bin_button should exist.");
            Assert.NotNull(root.Q<Button>("recycle_bin_button"), "recycle_bin_button should exist.");
            Assert.NotNull(root.Q<Button>("landfill_bin_button"), "landfill_bin_button should exist.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-05: Bin buttons are interactable during gameplay.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator BinButtonsCanBeClicked()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            yield return StartCompostingGameplay(cm);

            VisualElement root = cm.uiDocument.rootVisualElement;
            Button compostButton = root.Q<Button>("compost_bin_button");
            Button recycleButton = root.Q<Button>("recycle_bin_button");
            Button landfillButton = root.Q<Button>("landfill_bin_button");

            Assert.NotNull(compostButton, "compost_bin_button should exist.");
            Assert.NotNull(recycleButton, "recycle_bin_button should exist.");
            Assert.NotNull(landfillButton, "landfill_bin_button should exist.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = compostButton;
                compostButton.SendEvent(click);
            }
            yield return null;

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = recycleButton;
                recycleButton.SendEvent(click);
            }
            yield return null;

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = landfillButton;
                landfillButton.SendEvent(click);
            }
            yield return null;

            Assert.Pass("All three bin buttons were clicked without errors.");
        }

        [UnityTest]
        public IEnumerator SelectingBinShowsFeedbackAndLoadsNextItem()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            yield return StartCompostingGameplay(cm);

            VisualElement root = cm.uiDocument.rootVisualElement;
            Label itemLabel = root.Q<Label>("item_label");
            Label feedbackLabel = root.Q<Label>("feedback_label");
            Button compostButton = root.Q<Button>("compost_bin_button");

            Assert.NotNull(itemLabel, "item_label should exist.");
            Assert.NotNull(feedbackLabel, "feedback_label should exist.");
            Assert.NotNull(compostButton, "compost_bin_button should exist.");

            string firstItem = itemLabel.text;
            string feedbackBefore = feedbackLabel.text;

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = compostButton;
                compostButton.SendEvent(click);
            }
            yield return null;

            Assert.AreNotEqual(feedbackBefore, feedbackLabel.text, "Feedback should update after selecting a bin.");
            Assert.AreNotEqual(firstItem, itemLabel.text, "After sorting one item, the next item should load.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-06: Rules popup elements exist and can be opened from results.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ResultScreenContainsContinueButton()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            cm.timeLimit = 0.05f;
            yield return StartCompostingGameplay(cm);
            yield return WaitForResultVisible(cm);

            VisualElement root = cm.uiDocument.rootVisualElement;
            Assert.NotNull(root.Q<VisualElement>("result_container"), "result_container should exist.");
            Button continueButton = root.Q<Button>("exit_button");
            Assert.NotNull(continueButton, "exit_button should exist for result Continue.");
            Assert.AreEqual(DisplayStyle.Flex, continueButton.style.display.value, "Continue should be visible on result.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-07: Rules popup can be opened and closed through button clicks.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ContinueFromResultCompletesMinigame()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            bool completed = false;
            int value = int.MinValue;
            cm.OnMinigameComplete += v =>
            {
                completed = true;
                value = v;
            };

            cm.timeLimit = 0.05f;
            yield return StartCompostingGameplay(cm);
            yield return WaitForResultVisible(cm);

            VisualElement root = cm.uiDocument.rootVisualElement;
            Button continueButton = root.Q<Button>("exit_button");
            Assert.NotNull(continueButton, "Continue button should exist on result page.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = continueButton;
                continueButton.SendEvent(click);
            }
            yield return null;

            Assert.IsTrue(completed, "OnMinigameComplete should fire from result Continue.");
            Assert.GreaterOrEqual(value, 0, "Result reward should be 0 or greater.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-08: Exit button closes minigame from intro screen.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator StartPageHidesExitButton()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            cm.StartMinigame();
            yield return null;

            VisualElement root = cm.uiDocument.rootVisualElement;
            Button exitButton = root.Q<Button>("exit_button");
            Assert.NotNull(exitButton, "exit_button should exist.");
            Assert.AreEqual(DisplayStyle.None, exitButton.resolvedStyle.display, "Exit button should be hidden on intro.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-09: Timer counts down during gameplay.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator TimerCountsDownDuringGameplay()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            yield return StartCompostingGameplay(cm);

            Label timerLabel = cm.uiDocument.rootVisualElement.Q<Label>("timer_label");
            Assert.NotNull(timerLabel, "timer_label should exist.");

            string initialText = timerLabel.text;

            yield return new WaitForSeconds(1f);

            string updatedText = timerLabel.text;

            Assert.AreNotEqual(initialText, updatedText, "Timer label should change as time counts down.");
        }

        [UnityTest]
        public IEnumerator TimeUpShowsResultWithCorrectCountAndPollutionReduction()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            cm.timeLimit = 0.05f;
            yield return StartCompostingGameplay(cm);
            yield return WaitForResultVisible(cm);

            VisualElement root = cm.uiDocument.rootVisualElement;
            VisualElement resultContainer = root.Q<VisualElement>("result_container");
            Label resultLabel = root.Q<Label>("result_label");
            Label resultScore = root.Q<Label>("result_score");

            Assert.NotNull(resultContainer, "result_container should exist.");
            Assert.NotNull(resultLabel, "result_label should exist.");
            Assert.NotNull(resultScore, "result_score should exist.");
            Assert.AreEqual(DisplayStyle.Flex, resultContainer.style.display.value, "Result screen should be shown when time is up.");
            Assert.IsTrue(resultLabel.text.ToLower().Contains("you got"), "Result should show correct-answer summary.");
            Assert.IsTrue(resultScore.text.ToLower().Contains("pollution reduction"), "Result should show pollution reduction earned.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-10: Theme is clear from UI labels and bin names.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ThemeIsClearFromUI()
        {
            CompostingMinigame cm = GameObject.FindFirstObjectByType<CompostingMinigame>();
            Assert.NotNull(cm, "CompostingMinigame should exist.");

            cm.StartMinigame();
            yield return null;

            VisualElement root = cm.uiDocument.rootVisualElement;

            Label titleLabel = root.Q<Label>("minigame_title");
            Button compostButton = root.Q<Button>("compost_bin_button");
            Button recycleButton = root.Q<Button>("recycle_bin_button");
            Button landfillButton = root.Q<Button>("landfill_bin_button");

            Assert.NotNull(titleLabel, "minigame_title should exist.");
            Assert.NotNull(compostButton, "compost_bin_button should exist.");
            Assert.NotNull(recycleButton, "recycle_bin_button should exist.");
            Assert.NotNull(landfillButton, "landfill_bin_button should exist.");

            Assert.IsTrue(titleLabel.text.ToLower().Contains("compost"),
                $"Title should reference composting but was '{titleLabel.text}'.");

            Assert.IsTrue(compostButton.text.ToLower().Contains("compost"));
            Assert.IsTrue(recycleButton.text.ToLower().Contains("recycle"));
            Assert.IsTrue(landfillButton.text.ToLower().Contains("landfill"));
        }
    }
}
