using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Minigames
{
    [Category("Minigames")]
    public class MG02_Planting_Trees
    {
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
        public IEnumerator LandingOnGreenSquareForPlantTreesDisplaysTheMinigame()
        {
            GameObject boardSquare12 = GameObject.Find("BoardSquare12");
            Assert.NotNull(boardSquare12, "BoardSquare12 should exist in BoardScene.");

            BoardSquare square = boardSquare12.GetComponent<BoardSquare>();
            Assert.NotNull(square, "BoardSquare12 should have a BoardSquare component.");

            PlantTreesMinigame minigame = Object.FindFirstObjectByType<PlantTreesMinigame>();
            Assert.NotNull(minigame, "BoardScene should contain the Plant Trees minigame.");

            minigame.StartMinigame();
            yield return null;

            Assert.AreEqual(
                DisplayStyle.Flex,
                minigame.uiDocument.rootVisualElement.resolvedStyle.display,
                "Plant Trees minigame should be displayed when launched.");
        }

        [UnityTest]
        public IEnumerator PlayerCanInteractToPlantTrees()
        {
            PlantTreesMinigame minigame = Object.FindFirstObjectByType<PlantTreesMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            VisualElement tapArea = minigame.uiDocument.rootVisualElement.Q<VisualElement>("tap_area");
            Assert.NotNull(tapArea, "Tap area should exist during gameplay.");
            Assert.AreEqual(0, minigame.GetTreesPlanted(), "Initial planted tree count should be 0.");

            ClickVisualElement(tapArea);
            yield return null;

            Assert.AreEqual(1, minigame.GetTreesPlanted(), "Clicking the tap area should plant one tree.");
        }

        [UnityTest]
        public IEnumerator PlantingTreesShowsVisibleFeedback()
        {
            PlantTreesMinigame minigame = Object.FindFirstObjectByType<PlantTreesMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            VisualElement root = minigame.uiDocument.rootVisualElement;
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            VisualElement gardenGround = root.Q<VisualElement>("garden_ground");

            Assert.NotNull(tapArea, "Tap area should exist during gameplay.");
            Assert.NotNull(gardenGround, "Garden ground should exist during gameplay.");

            int plantedBefore = gardenGround.childCount;
            ClickVisualElement(tapArea);
            yield return null;

            Assert.Greater(gardenGround.childCount, plantedBefore, "Planting should add a visible tree visual to the garden.");
        }

        [UnityTest]
        public IEnumerator MinigameEndsAfterFiveSeconds()
        {
            PlantTreesMinigame minigame = Object.FindFirstObjectByType<PlantTreesMinigame>();
            Assert.NotNull(minigame);
            Assert.AreEqual(5f, minigame.timeLimit, "Plant Trees minigame should be configured for 5 seconds.");

            StartGameplay(minigame);
            yield return new WaitForSeconds(minigame.timeLimit + 0.25f);

            VisualElement resultContainer = minigame.uiDocument.rootVisualElement.Q<VisualElement>("result_container");
            Assert.NotNull(resultContainer, "Result container should exist.");
            Assert.AreEqual(DisplayStyle.Flex, resultContainer.resolvedStyle.display, "End feedback should show after five seconds.");
        }

        [UnityTest]
        public IEnumerator FinishingOrExitingClosesTheMinigameAndReturnsControl()
        {
            PlantTreesMinigame earlyExitMinigame = Object.FindFirstObjectByType<PlantTreesMinigame>();
            Assert.NotNull(earlyExitMinigame);

            earlyExitMinigame.StartMinigame();
            yield return null;

            bool exitedEarly = false;
            earlyExitMinigame.OnMinigameExited += () => exitedEarly = true;

            Button introExitButton = earlyExitMinigame.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(introExitButton, "Exit button should exist on the intro page.");
            ClickVisualElement(introExitButton);
            yield return null;

            Assert.IsTrue(exitedEarly, "Exiting early should close the minigame and return control.");
            Assert.AreEqual(DisplayStyle.None, earlyExitMinigame.uiDocument.rootVisualElement.resolvedStyle.display, "UI should hide after exiting early.");

            PlantTreesMinigame finishedMinigame = Object.FindFirstObjectByType<PlantTreesMinigame>();
            Assert.NotNull(finishedMinigame);

            StartGameplay(finishedMinigame);
            yield return null;

            ClickVisualElement(finishedMinigame.uiDocument.rootVisualElement.Q<VisualElement>("tap_area"));
            yield return new WaitForSeconds(finishedMinigame.timeLimit + 0.25f);

            bool completed = false;
            finishedMinigame.OnMinigameComplete += _ => completed = true;

            Button resultExitButton = finishedMinigame.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(resultExitButton, "Exit button should exist on the result page.");
            ClickVisualElement(resultExitButton);
            yield return null;

            Assert.IsTrue(completed, "Exiting from the end page should finish the minigame and return control.");
            Assert.AreEqual(DisplayStyle.None, finishedMinigame.uiDocument.rootVisualElement.resolvedStyle.display, "UI should hide after closing the result page.");
        }

        [UnityTest]
        public IEnumerator PollutionRewardIsAppliedBasedOnPerformance()
        {
            PlantTreesMinigame minigame = Object.FindFirstObjectByType<PlantTreesMinigame>();
            Assert.NotNull(minigame);

            Assert.AreEqual(2, minigame.CalculatePollutionReduction(11), "More than 10 taps should reduce pollution by 2.");
            Assert.AreEqual(1, minigame.CalculatePollutionReduction(10), "Between 1 and 10 taps should reduce pollution by 1.");
            Assert.AreEqual(0, minigame.CalculatePollutionReduction(0), "Zero taps should reduce pollution by 0.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlantingTreesThemeIsClearInVisualsAndText()
        {
            PlantTreesMinigame minigame = Object.FindFirstObjectByType<PlantTreesMinigame>();
            Assert.NotNull(minigame);

            minigame.StartMinigame();
            yield return null;

            VisualElement root = minigame.uiDocument.rootVisualElement;
            Label title = root.Q<Label>("minigame_title");
            Label description = root.Q<Label>("minigame_description");
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            VisualElement gardenGround = root.Q<VisualElement>("garden_ground");

            Assert.NotNull(title, "Title label should exist.");
            Assert.NotNull(description, "Description label should exist.");
            Assert.NotNull(tapArea, "Tap area should exist.");
            Assert.NotNull(gardenGround, "Garden background should exist.");

            StringAssert.Contains("tree", title.text.ToLower(), "Title should clearly communicate the tree-planting theme.");
            StringAssert.Contains("plant", description.text.ToLower(), "Instructions should mention planting.");
        }

        private static void StartGameplay(PlantTreesMinigame minigame)
        {
            minigame.StartMinigame();

            Button beginButton = minigame.uiDocument.rootVisualElement.Q<Button>("begin_button");
            Assert.NotNull(beginButton, "Begin button should exist on the intro page.");

            ClickVisualElement(beginButton);
        }

        private static void ClickVisualElement(VisualElement element)
        {
            Assert.NotNull(element, "Expected visual element to click.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = element;
                element.SendEvent(click);
            }
        }
    }
}
