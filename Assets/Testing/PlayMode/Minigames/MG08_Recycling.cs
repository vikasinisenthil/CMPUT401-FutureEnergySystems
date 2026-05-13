using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Minigames
{
    [Category("Minigames")]
    public class MG08_Recycling
    {
        private static void Click(VisualElement target)
        {
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = target;
                target.SendEvent(click);
            }
        }

        private static IEnumerator BeginRound(RecyclingMinigame mg)
        {
            Button begin = mg.uiDocument.rootVisualElement.Q<Button>("begin_button");
            Assert.NotNull(begin, "begin_button should exist.");
            Click(begin);
            yield return null;
        }

        private static RecyclingMinigame.BinType WrongBinFor(RecyclingMinigame.BinType target)
        {
            return target switch
            {
                RecyclingMinigame.BinType.LeftTop => RecyclingMinigame.BinType.RightBottom,
                RecyclingMinigame.BinType.LeftBottom => RecyclingMinigame.BinType.RightTop,
                RecyclingMinigame.BinType.RightTop => RecyclingMinigame.BinType.LeftBottom,
                _ => RecyclingMinigame.BinType.LeftTop
            };
        }

        private static void PointerDown(VisualElement target)
        {
            using (PointerDownEvent down = PointerDownEvent.GetPooled())
            {
                down.target = target;
                target.SendEvent(down);
            }
        }

        private static void PointerMove(VisualElement target)
        {
            using (PointerMoveEvent move = PointerMoveEvent.GetPooled())
            {
                move.target = target;
                target.SendEvent(move);
            }
        }

        private static void PointerUp(VisualElement target)
        {
            using (PointerUpEvent up = PointerUpEvent.GetPooled())
            {
                up.target = target;
                target.SendEvent(up);
            }
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
        public IEnumerator RecyclingMinigameExists()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg, "RecyclingMinigame should exist in BoardScene.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator LandingOnGreenSquareCanTriggerRecyclingMinigame()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg, "RecyclingMinigame should exist.");

            GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
            MinigameManager mm = GameObject.Find("GameController")?.GetComponent<MinigameManager>();
            Assert.NotNull(gc, "GameController should exist.");
            Assert.NotNull(mm, "MinigameManager should exist.");

            mm.minigameObjects.Clear();
            mm.minigameObjects.Add(mg.gameObject);

            gc.LandedOnGreenSquare();
            yield return null;

            Assert.IsTrue(mm.IsMinigameActive, "Landing on green square should activate configured Recycling minigame.");
        }

        [UnityTest]
        public IEnumerator StartShowsFourBinsAndCenterItem()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            VisualElement root = mg.uiDocument.rootVisualElement;
            Assert.NotNull(root.Q<VisualElement>("bin_left_top"));
            Assert.NotNull(root.Q<VisualElement>("bin_left_bottom"));
            Assert.NotNull(root.Q<VisualElement>("bin_right_top"));
            Assert.NotNull(root.Q<VisualElement>("bin_right_bottom"));

            Label item = root.Q<Label>("current_trash_label");
            Assert.NotNull(item);
            Assert.IsFalse(string.IsNullOrWhiteSpace(item.text), "A center trash item should be visible at round start.");
        }

        [UnityTest]
        public IEnumerator CorrectSortAddsOnePoint()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            int start = mg.GetScore();
            mg.ForceSortCurrentItemTo(mg.GetCurrentItemTarget());
            yield return null;

            Assert.AreEqual(start + 1, mg.GetScore(), "Correct sort should increase score by 1.");
        }

        [UnityTest]
        public IEnumerator WrongSortSubtractsOnePoint()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            int start = mg.GetScore();
            mg.ForceSortCurrentItemTo(WrongBinFor(mg.GetCurrentItemTarget()));
            yield return null;

            Assert.AreEqual(start - 1, mg.GetScore(), "Wrong sort should decrease score by 1.");
        }

        [UnityTest]
        public IEnumerator NextItemAppearsAfterSorting()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            string firstItem = mg.GetCurrentItemLabel();
            mg.ForceSortCurrentItemTo(mg.GetCurrentItemTarget());
            yield return null;

            string secondItem = mg.GetCurrentItemLabel();
            Assert.AreNotEqual(firstItem, secondItem, "After sorting one item, next item should appear.");
            Assert.AreEqual(1, mg.GetItemsSortedCount(), "Exactly one item should be completed.");
        }

        [UnityTest]
        public IEnumerator DragInteractionCapturesAndReleasesPointerOnTrashItem()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            VisualElement root = mg.uiDocument.rootVisualElement;
            Label trashItem = root.Q<Label>("current_trash_label");
            Assert.NotNull(trashItem, "current_trash_label should exist.");

            // Simulate a drag gesture path with pointer events.
            PointerDown(trashItem);
            yield return null;
            Assert.IsTrue(trashItem.HasPointerCapture(0), "Trash item should capture pointer during drag.");

            PointerMove(trashItem);
            yield return null;

            PointerUp(trashItem);
            yield return null;
            Assert.IsFalse(trashItem.HasPointerCapture(0), "Trash item should release pointer after drag ends.");
        }

        [UnityTest]
        public IEnumerator MinigameEndsAfterAllItemsAndDisplaysFinalScore()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            int total = mg.GetTotalItems();
            for (int i = 0; i < total; i++)
            {
                mg.ForceSortCurrentItemTo(mg.GetCurrentItemTarget());
            }
            yield return null;

            VisualElement result = mg.uiDocument.rootVisualElement.Q<VisualElement>("result_container");
            Label finalScore = mg.uiDocument.rootVisualElement.Q<Label>("result_score");

            Assert.NotNull(result, "result_container should exist.");
            Assert.NotNull(finalScore, "result_score should exist.");
            Assert.AreEqual(DisplayStyle.Flex, result.resolvedStyle.display, "Result container should be shown after all items.");
            Assert.IsTrue(finalScore.text.ToLower().Contains("final score"), "Final score should be displayed.");
        }

        [UnityTest]
        public IEnumerator ReductionTiersMatchSpec()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            Assert.AreEqual(2, mg.ComputePollutionReduction(7), "Score >= 7 should reduce AQHI by 2.");
            Assert.AreEqual(2, mg.ComputePollutionReduction(10), "Score >= 7 should reduce AQHI by 2.");
            Assert.AreEqual(1, mg.ComputePollutionReduction(4), "Score >= 4 should reduce AQHI by 1.");
            Assert.AreEqual(1, mg.ComputePollutionReduction(6), "Score >= 4 should reduce AQHI by 1.");
            Assert.AreEqual(0, mg.ComputePollutionReduction(3), "Score < 4 should not change AQHI.");
            Assert.AreEqual(0, mg.ComputePollutionReduction(-2), "Score < 4 should not change AQHI.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ContinueCompletesWithExpectedReduction()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            bool completed = false;
            int reduction = int.MinValue;
            mg.OnMinigameComplete += value =>
            {
                completed = true;
                reduction = value;
            };

            mg.StartMinigame();
            yield return null;
            yield return BeginRound(mg);

            int total = mg.GetTotalItems();
            for (int i = 0; i < total; i++)
            {
                mg.ForceSortCurrentItemTo(mg.GetCurrentItemTarget());
            }
            yield return null;

            Button continueButton = mg.uiDocument.rootVisualElement.Q<Button>("exit_button");
            Assert.NotNull(continueButton, "exit_button should exist on result page.");
            Click(continueButton);
            yield return null;

            Assert.IsTrue(completed, "Continue should complete the minigame.");
            Assert.AreEqual(2, reduction, "Perfect/high score run should reduce AQHI by 2.");
        }

        [UnityTest]
        public IEnumerator StartPageHasNoExitButton()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;

            Button introExit = mg.uiDocument.rootVisualElement.Q<Button>("intro_exit_button");
            Assert.IsNull(introExit, "Start page should not have an exit button.");
        }

        [UnityTest]
        public IEnumerator InstructionsUseDragOnlyNoSwipe()
        {
            RecyclingMinigame mg = Object.FindFirstObjectByType<RecyclingMinigame>();
            Assert.NotNull(mg);

            mg.StartMinigame();
            yield return null;

            VisualElement root = mg.uiDocument.rootVisualElement;
            Label introDescription = root.Q<Label>("minigame_description");
            Assert.NotNull(introDescription, "minigame_description should exist on intro page.");
            Assert.IsTrue(introDescription.text.ToLower().Contains("drag"), "Intro instructions should mention drag.");
            Assert.IsFalse(introDescription.text.ToLower().Contains("swipe"), "Intro instructions should not mention swipe.");

            yield return BeginRound(mg);

            Label gameplayFeedback = root.Q<Label>("feedback_label");
            Assert.NotNull(gameplayFeedback, "feedback_label should exist.");
            Assert.IsTrue(gameplayFeedback.text.ToLower().Contains("drag"), "Gameplay instructions should mention drag.");
            Assert.IsFalse(gameplayFeedback.text.ToLower().Contains("swipe"), "Gameplay instructions should not mention swipe.");
        }
    }
}
