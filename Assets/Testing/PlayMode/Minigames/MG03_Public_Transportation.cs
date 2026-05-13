using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Minigames
{
    [Category("Minigames")]
    public class MG03_Public_Transportation
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
        public IEnumerator LandingOnGreenSquareDisplaysPublicTransportationMinigame()
        {
            GameObject boardSquare17 = GameObject.Find("BoardSquare17");
            Assert.NotNull(boardSquare17, "BoardSquare17 should exist in BoardScene.");

            BoardSquare square = boardSquare17.GetComponent<BoardSquare>();
            Assert.NotNull(square, "BoardSquare17 should have a BoardSquare component.");
            Assert.NotNull(square.minigameObject, "BoardSquare17 should have a minigame assigned.");

            var minigame = square.minigameObject.GetComponent<PublicTransportationMinigame>();
            Assert.NotNull(minigame, "BoardSquare17 should launch the Public Transportation minigame.");

            minigame.StartMinigame();
            yield return null;

            Assert.AreEqual(
                DisplayStyle.Flex,
                minigame.uiDocument.rootVisualElement.resolvedStyle.display,
                "Public Transportation minigame should be displayed when launched.");
        }

        [UnityTest]
        public IEnumerator PlayerSeesThreeTransportationLanesCarBusAndMetro()
        {
            var minigame = Object.FindFirstObjectByType<PublicTransportationMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            VisualElement root = minigame.uiDocument.rootVisualElement;
            Assert.NotNull(root.Q<VisualElement>("lane_car"), "Car lane should be visible.");
            Assert.NotNull(root.Q<VisualElement>("lane_bus"), "Bus lane should be visible.");
            Assert.NotNull(root.Q<VisualElement>("lane_metro"), "Metro lane should be visible.");
        }

        [UnityTest]
        public IEnumerator PlayerCanMoveBetweenLanesDuringTheMinigame()
        {
            var minigame = Object.FindFirstObjectByType<PublicTransportationMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            Assert.AreEqual(1, minigame.GetCurrentLaneIndex(), "Player should start in the Bus lane.");

            minigame.MovePlayerLane(-1);
            Assert.AreEqual(0, minigame.GetCurrentLaneIndex(), "Player should be able to move to the Car lane.");

            minigame.MovePlayerLane(2);
            Assert.AreEqual(2, minigame.GetCurrentLaneIndex(), "Player should be able to move to the Metro lane.");
        }

        [UnityTest]
        public IEnumerator FinishingInMetroLaneReducesPollutionByTwo()
        {
            var minigame = Object.FindFirstObjectByType<PublicTransportationMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            minigame.MovePlayerLane(1);
            Assert.AreEqual(2, minigame.GetCurrentLaneIndex(), "Player should be in Metro lane before resolution.");

            InvokePrivateMethod(minigame, "ResolveSuccess");
            yield return null;

            Label resultScore = minigame.uiDocument.rootVisualElement.Q<Label>("result_score");
            Assert.NotNull(resultScore, "Result score label should exist.");
            StringAssert.Contains("2", resultScore.text, "Finishing in Metro lane should reduce pollution by 2.");
        }

        [UnityTest]
        public IEnumerator FinishingInBusLaneReducesPollutionByOne()
        {
            var minigame = Object.FindFirstObjectByType<PublicTransportationMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            Assert.AreEqual(1, minigame.GetCurrentLaneIndex(), "Player should start in Bus lane.");

            InvokePrivateMethod(minigame, "ResolveSuccess");
            yield return null;

            Label resultScore = minigame.uiDocument.rootVisualElement.Q<Label>("result_score");
            Assert.NotNull(resultScore, "Result score label should exist.");
            StringAssert.Contains("1", resultScore.text, "Finishing in Bus lane should reduce pollution by 1.");
        }

        [UnityTest]
        public IEnumerator FinishingInCarLaneDoesNotReducePollution()
        {
            var minigame = Object.FindFirstObjectByType<PublicTransportationMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            minigame.MovePlayerLane(-1);
            Assert.AreEqual(0, minigame.GetCurrentLaneIndex(), "Player should be in Car lane before resolution.");

            InvokePrivateMethod(minigame, "ResolveSuccess");
            yield return null;

            Label resultScore = minigame.uiDocument.rootVisualElement.Q<Label>("result_score");
            Assert.NotNull(resultScore, "Result score label should exist.");
            StringAssert.Contains("0", resultScore.text, "Finishing in Car lane should not reduce pollution.");
        }

        [UnityTest]
        public IEnumerator HittingObstacleShowsEndFeedbackWithNoPollutionReduction()
        {
            var minigame = Object.FindFirstObjectByType<PublicTransportationMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            InvokePrivateMethod(minigame, "ResolveCrash");
            yield return null;

            VisualElement root = minigame.uiDocument.rootVisualElement;
            Label resultHeadline = root.Q<Label>("result_headline");
            Label resultScore = root.Q<Label>("result_score");

            Assert.NotNull(resultHeadline, "Result headline should exist.");
            Assert.NotNull(resultScore, "Result score label should exist.");
            StringAssert.Contains("no safe lane", resultHeadline.text.ToLower(), "Crash feedback should be shown.");
            StringAssert.Contains("0", resultScore.text, "Crashing should give no pollution reduction.");
        }

        [UnityTest]
        public IEnumerator AfterViewingEndFeedbackPlayerCanDismissMinigameAndReturnToBoard()
        {
            var minigame = Object.FindFirstObjectByType<PublicTransportationMinigame>();
            Assert.NotNull(minigame);

            StartGameplay(minigame);
            yield return null;

            minigame.MovePlayerLane(1);
            InvokePrivateMethod(minigame, "ResolveSuccess");
            yield return null;

            bool completionRaised = false;
            int completionScore = -1;
            minigame.OnMinigameComplete += score =>
            {
                completionRaised = true;
                completionScore = score;
            };

            Button exitButton = minigame.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(exitButton, "Exit button should be available on the end feedback page.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = exitButton;
                exitButton.SendEvent(click);
            }

            yield return null;

            Assert.IsTrue(completionRaised, "Dismissing the end feedback should complete the minigame and return control to the board.");
            Assert.AreEqual(2, completionScore, "Dismissing from Metro result should return the expected pollution reduction.");
            Assert.AreEqual(
                DisplayStyle.None,
                minigame.uiDocument.rootVisualElement.resolvedStyle.display,
                "Minigame UI should hide after dismissing the end feedback.");
        }

        private static void StartGameplay(PublicTransportationMinigame minigame)
        {
            minigame.StartMinigame();

            VisualElement root = minigame.uiDocument.rootVisualElement;
            Button beginButton = root.Q<Button>("begin_button");
            Assert.NotNull(beginButton, "Begin button should exist on the intro page.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = beginButton;
                beginButton.SendEvent(click);
            }
        }

        private static void InvokePrivateMethod(PublicTransportationMinigame minigame, string methodName)
        {
            MethodInfo method = typeof(PublicTransportationMinigame).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"Expected private method {methodName} to exist.");
            method.Invoke(minigame, null);
        }
    }
}
