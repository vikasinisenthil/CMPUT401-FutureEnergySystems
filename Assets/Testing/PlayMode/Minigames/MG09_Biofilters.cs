using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Minigames
{
    [Category("Minigames")]
    public class MG09_Biofilter
    {
        private static IEnumerator StartBiofilterGameplay(BiofilterMinigame biofilter)
        {
            biofilter.StartMinigame();
            yield return null;

            VisualElement root = biofilter.uiDocument.rootVisualElement;
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

        [UnityTest]
        public IEnumerator BiofilterMinigameExistsInScene()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist in the scene.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator GreenSquareTriggerOpensBiofilterMinigame()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist.");

            GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
            MinigameManager mm = GameObject.Find("GameController")?.GetComponent<MinigameManager>();

            Assert.NotNull(gc, "GameController should exist.");
            Assert.NotNull(mm, "MinigameManager should exist.");

            mm.minigameObjects.Clear();
            mm.minigameObjects.Add(biofilter.gameObject);

            gc.LandedOnGreenSquare();
            yield return null;

            Assert.IsTrue(mm.IsMinigameActive, "Green-square trigger should open the Biofilter minigame.");
        }

        [UnityTest]
        public IEnumerator IntroShowsTitleRulesAndStartButton()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist.");

            biofilter.StartMinigame();
            yield return null;

            VisualElement root = biofilter.uiDocument.rootVisualElement;
            Label titleLabel = root.Q<Label>("minigame_title");
            Label descriptionLabel = root.Q<Label>("minigame_description");
            Button startButton = root.Q<Button>("begin_button");

            Assert.NotNull(titleLabel, "Intro should show the minigame title.");
            Assert.NotNull(descriptionLabel, "Intro should show kid-friendly rules text.");
            Assert.NotNull(startButton, "Intro should show a Start button.");
            Assert.IsTrue(startButton.enabledSelf, "Start button should be enabled on intro.");
            Assert.IsTrue(descriptionLabel.text.ToLower().Contains("rules"), "Intro description should contain rules text.");
        }

        [UnityTest]
        public IEnumerator PressingStartShowsLayerChoicesAndRunFilter()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist.");

            yield return StartBiofilterGameplay(biofilter);

            VisualElement root = biofilter.uiDocument.rootVisualElement;

            Assert.NotNull(root.Q<Button>("gravel_button"), "gravel_button should exist.");
            Assert.NotNull(root.Q<Button>("sand_button"), "sand_button should exist.");
            Assert.NotNull(root.Q<Button>("charcoal_button"), "charcoal_button should exist.");
            Assert.NotNull(root.Q<Button>("plants_button"), "plants_button should exist.");
            Assert.NotNull(root.Q<Button>("run_filter_button"), "run_filter_button should exist.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator LayerSelectionUpdatesFeedbackAndSelectedLayers()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist.");

            yield return StartBiofilterGameplay(biofilter);

            VisualElement root = biofilter.uiDocument.rootVisualElement;
            Label selectedLayersLabel = root.Q<Label>("selected_layers_label");
            Label feedbackLabel = root.Q<Label>("feedback_label");
            Button gravelButton = root.Q<Button>("gravel_button");
            Button sandButton = root.Q<Button>("sand_button");

            Assert.NotNull(selectedLayersLabel, "selected_layers_label should exist.");
            Assert.NotNull(feedbackLabel, "feedback_label should exist.");
            Assert.NotNull(gravelButton, "gravel_button should exist.");
            Assert.NotNull(sandButton, "sand_button should exist.");

            Assert.AreEqual("None yet", selectedLayersLabel.text, "Selected layers should start empty.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = gravelButton;
                gravelButton.SendEvent(click);
            }
            yield return null;

            Assert.IsTrue(selectedLayersLabel.text.Contains("Gravel"),
                "Selected layers should update after Gravel is chosen.");
            Assert.IsTrue(feedbackLabel.text.Contains("Gravel"),
                "Feedback should update when the player selects a layer.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = sandButton;
                sandButton.SendEvent(click);
            }
            yield return null;

            Assert.IsTrue(selectedLayersLabel.text.Contains("Sand"),
                "Selected layers should update after Sand is chosen.");
        }

        [UnityTest]
        public IEnumerator TimerAndProgressAreVisibleAndUpdateDuringGameplay()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist.");

            yield return StartBiofilterGameplay(biofilter);

            VisualElement root = biofilter.uiDocument.rootVisualElement;
            Label timerLabel = root.Q<Label>("timer_label");
            Label progressLabel = root.Q<Label>("progress_label");
            Button gravelButton = root.Q<Button>("gravel_button");

            Assert.NotNull(timerLabel, "Timer should be visible during gameplay.");
            Assert.NotNull(progressLabel, "Progress should be visible during gameplay.");
            Assert.NotNull(gravelButton, "Layer button should exist.");

            string timerBefore = timerLabel.text;
            string progressBefore = progressLabel.text;

            yield return new WaitForSeconds(1.1f);

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = gravelButton;
                gravelButton.SendEvent(click);
            }
            yield return null;

            Assert.AreNotEqual(timerBefore, timerLabel.text, "Timer text should update while playing.");
            Assert.AreNotEqual(progressBefore, progressLabel.text, "Progress text should update as steps are added.");
        }

        [UnityTest]
        public IEnumerator RunFilterShowsCorrectCountAndPollutionReductionOnResult()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist.");

            yield return StartBiofilterGameplay(biofilter);

            VisualElement root = biofilter.uiDocument.rootVisualElement;
            Button gravelButton = root.Q<Button>("gravel_button");
            Button sandButton = root.Q<Button>("sand_button");
            Button charcoalButton = root.Q<Button>("charcoal_button");
            Button plantsButton = root.Q<Button>("plants_button");
            Button runFilterButton = root.Q<Button>("run_filter_button");
            Label resultLabel = root.Q<Label>("result_label");
            Label resultScore = root.Q<Label>("result_score");

            Assert.NotNull(gravelButton, "gravel_button should exist.");
            Assert.NotNull(sandButton, "sand_button should exist.");
            Assert.NotNull(charcoalButton, "charcoal_button should exist.");
            Assert.NotNull(plantsButton, "plants_button should exist.");
            Assert.NotNull(runFilterButton, "run_filter_button should exist.");
            Assert.NotNull(resultLabel, "result_label should exist.");
            Assert.NotNull(resultScore, "result_score should exist.");

            Button[] buttons = { gravelButton, sandButton, charcoalButton, plantsButton };
            foreach (Button button in buttons)
            {
                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = button;
                    button.SendEvent(click);
                }
                yield return null;
            }

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = runFilterButton;
                runFilterButton.SendEvent(click);
            }
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, resultLabel.style.display.value,
                "Result label should be visible after Run Filter is pressed.");
            Assert.IsTrue(resultLabel.text.ToLower().Contains("layer"),
                "Result should show how many layers were in correct spots.");
            Assert.IsTrue(resultScore.text.ToLower().Contains("pollution reduction"),
                "Result should show earned pollution reduction.");
        }

        [UnityTest]
        public IEnumerator StartPageHasNoExitButton()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist.");

            biofilter.StartMinigame();
            yield return null;

            VisualElement root = biofilter.uiDocument.rootVisualElement;
            Button exitButton = root.Q<Button>("exit_button");
            Assert.NotNull(exitButton, "exit_button should exist.");
            Assert.AreEqual(DisplayStyle.None, exitButton.resolvedStyle.display, "Intro page should hide exit_button.");
        }

        [UnityTest]
        public IEnumerator ContinueFromResultAppliesEarnedPollutionReduction()
        {
            BiofilterMinigame biofilter = GameObject.FindFirstObjectByType<BiofilterMinigame>();
            Assert.NotNull(biofilter, "BiofilterMinigame should exist.");

            bool completedFired = false;
            int completionValue = -1;

            biofilter.OnMinigameComplete += value =>
            {
                completedFired = true;
                completionValue = value;
            };

            yield return StartBiofilterGameplay(biofilter);

            VisualElement root = biofilter.uiDocument.rootVisualElement;
            Button gravelButton = root.Q<Button>("gravel_button");
            Button sandButton = root.Q<Button>("sand_button");
            Button charcoalButton = root.Q<Button>("charcoal_button");
            Button plantsButton = root.Q<Button>("plants_button");
            Button runFilterButton = root.Q<Button>("run_filter_button");
            Button[] buttons = { gravelButton, sandButton, charcoalButton, plantsButton };
            foreach (Button button in buttons)
            {
                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = button;
                    button.SendEvent(click);
                }
                yield return null;
            }

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = runFilterButton;
                runFilterButton.SendEvent(click);
            }
            yield return null;

            Button continueButton = root.Q<Button>("exit_button");
            Assert.NotNull(continueButton, "exit_button should exist on result screen.");
            Assert.AreEqual(DisplayStyle.Flex, continueButton.resolvedStyle.display, "Continue button should be visible on result screen.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = continueButton;
                continueButton.SendEvent(click);
            }
            yield return null;

            Assert.IsTrue(completedFired, "OnMinigameComplete should fire when exiting from the result screen.");
            Assert.GreaterOrEqual(completionValue, 0, "Completion reward should be 0 or greater.");
        }
    }
}
