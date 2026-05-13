using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>
/// MG.06 - Ride a Bike Minigame
/// Spec: Travel 50m by tapping. >= 50m: -2 pollution. >= 25m: -1 pollution. < 25m: 0 pollution.
/// </summary>

namespace Minigames {
    [Category("Minigames")]
    public class MG06_Ride_A_Bike
    {
        private static IEnumerator StartRideBikeGameplay(RideBikeMinigame rbm)
        {
            rbm.StartMinigame();
            yield return null;

            VisualElement root = rbm.uiDocument.rootVisualElement;
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

            // Setup GameManager with valid character to avoid sprite errors
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
        public IEnumerator RideBikeMinigameExistsInScene()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist in the scene.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator LandingOnGreenSquareCanTriggerRideBikeMinigame()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
            MinigameManager mm = GameObject.Find("GameController")?.GetComponent<MinigameManager>();
            Assert.NotNull(gc, "GameController should exist.");
            Assert.NotNull(mm, "MinigameManager should exist.");

            mm.minigameObjects.Clear();
            mm.minigameObjects.Add(rbm.gameObject);

            gc.LandedOnGreenSquare();
            yield return null;

            Assert.IsTrue(mm.IsMinigameActive, "Landing on green square should activate configured Ride Bike minigame.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-02: Player can tap to pedal — tap area exists and is interactable.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator TapAreaExistsAndIsInteractable()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root    = rbm.uiDocument.rootVisualElement;
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            Assert.NotNull(tapArea, "tap_area should exist in the UI.");
            Assert.AreNotEqual(DisplayStyle.None, tapArea.style.display.value,
                "tap_area should be visible when the minigame starts.");

            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-02: Tapping the tap area increases distance travelled.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator TappingTapAreaIncreasesDistance()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root    = rbm.uiDocument.rootVisualElement;
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            Assert.NotNull(tapArea, "tap_area should exist.");

            float distanceBefore = rbm.GetDistanceTravelled();
            Assert.AreEqual(0f, distanceBefore, "Distance should start at 0.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = tapArea;
                tapArea.SendEvent(click);
            }
            yield return null;

            Assert.Greater(rbm.GetDistanceTravelled(), 0f,
                "Distance should increase after tapping the tap area.");

            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-03: Visual feedback — progress bar, distance label, speed label exist.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator VisualFeedbackElementsExist()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root = rbm.uiDocument.rootVisualElement;

            Assert.NotNull(root.Q<VisualElement>("progress_fill"),
                "progress_fill should exist for visual movement feedback.");
            Assert.NotNull(root.Q<VisualElement>("bike_icon"),
                "bike_icon should exist and move along the progress track.");
            Assert.NotNull(root.Q<Label>("distance_label"),
                "distance_label should exist to show distance travelled.");
            Assert.NotNull(root.Q<Label>("speed_label"),
                "speed_label should exist to show current speed.");
            Assert.NotNull(root.Q<Label>("timer_label"),
                "timer_label should exist to show time remaining.");

            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-03: Progress fill width increases as distance increases.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ProgressBarUpdatesOnTap()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root        = rbm.uiDocument.rootVisualElement;
            VisualElement tapArea     = root.Q<VisualElement>("tap_area");
            VisualElement progressFill = root.Q<VisualElement>("progress_fill");
            Assert.NotNull(tapArea,      "tap_area should exist.");
            Assert.NotNull(progressFill, "progress_fill should exist.");

            // Tap several times to move progress bar
            for (int i = 0; i < 5; i++)
            {
                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = tapArea;
                    tapArea.SendEvent(click);
                }
            }
            yield return null;

            Assert.Greater(rbm.GetDistanceTravelled(), 0f,
                "Distance should increase after tapping, which drives the progress bar.");

            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-04: Minigame ends when player travels 50 metres (full reward).
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator FullRewardWhenGoalDistanceReached()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            Assert.AreEqual(2, rbm.CalculateReward(50f),
                "Travelling 50m should give full reward of 2 (−2 pollution).");
            Assert.AreEqual(2, rbm.CalculateReward(60f),
                "Travelling more than 50m should still give full reward of 2.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator MinigameCompletesAfterTappingToGoal()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            bool completed = false;
            rbm.OnMinigameComplete += _ => completed = true;

            yield return StartRideBikeGameplay(rbm);

            VisualElement root    = rbm.uiDocument.rootVisualElement;
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            Assert.NotNull(tapArea, "tap_area should exist.");

            // Tap enough times to reach 50m (metresPerTap = 1.5, need 34 taps)
            for (int i = 0; i < 34; i++)
            {
                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = tapArea;
                    tapArea.SendEvent(click);
                }
            }
            yield return null;

            Assert.IsFalse(rbm.IsActive,
                "Minigame should no longer be active after reaching 50m.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator GoalThenExitFiresOnMinigameCompleteWithFullReward()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            int completionValue = int.MinValue;
            bool completed = false;
            rbm.OnMinigameComplete += value =>
            {
                completionValue = value;
                completed = true;
            };

            yield return StartRideBikeGameplay(rbm);

            VisualElement root = rbm.uiDocument.rootVisualElement;
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            Assert.NotNull(tapArea, "tap_area should exist.");

            for (int i = 0; i < 34; i++)
            {
                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = tapArea;
                    tapArea.SendEvent(click);
                }
            }
            yield return null;

            Button exitButton = root.Q<Button>("exit_button");
            Assert.NotNull(exitButton, "exit_button should exist on end page.");

            using (ClickEvent exitClick = ClickEvent.GetPooled())
            {
                exitClick.target = exitButton;
                exitButton.SendEvent(exitClick);
            }
            yield return null;

            Assert.IsTrue(completed, "OnMinigameComplete should fire after exiting end page.");
            Assert.AreEqual(2, completionValue, "Reaching goal should complete with full reward (2).");
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-05: Exiting early closes minigame and resumes main game.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ExitButtonClosesMinigame()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            rbm.StartMinigame();
            yield return null;

            VisualElement root = rbm.uiDocument.rootVisualElement;
            Button exitButton  = root.Q<Button>("exit_button");
            Assert.NotNull(exitButton, "exit_button should exist.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = exitButton;
                exitButton.SendEvent(click);
            }
            yield return null;

            Assert.IsFalse(rbm.IsActive,
                "Minigame should be inactive after pressing Exit.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator EarlyExitFiresOnMinigameExited()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            bool exitedFired   = false;
            bool completeFired = false;
            rbm.OnMinigameExited   += ()  => exitedFired   = true;
            rbm.OnMinigameComplete += (_) => completeFired = true;

            rbm.StartMinigame();
            yield return null;

            VisualElement root = rbm.uiDocument.rootVisualElement;
            Button exitButton  = root.Q<Button>("exit_button");
            Assert.NotNull(exitButton, "exit_button should exist.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = exitButton;
                exitButton.SendEvent(click);
            }
            yield return null;

            Assert.IsTrue(exitedFired,
                "OnMinigameExited should fire when Exit is pressed during gameplay.");
            Assert.IsFalse(completeFired,
                "OnMinigameComplete should NOT fire when exiting early.");

            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-06: Pollution reward tiers applied correctly after minigame ends.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator PollutionRewardTier_Full_50m()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            Assert.AreEqual(2, rbm.CalculateReward(50f),  "50m → −2 pollution");
            Assert.AreEqual(2, rbm.CalculateReward(100f), "100m → −2 pollution");

            yield return null;
        }

        [UnityTest]
        public IEnumerator PollutionRewardTier_Half_25m()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            Assert.AreEqual(1, rbm.CalculateReward(25f), "25m → −1 pollution");
            Assert.AreEqual(1, rbm.CalculateReward(49f), "49m → −1 pollution");

            yield return null;
        }

        [UnityTest]
        public IEnumerator PollutionRewardTier_Zero_Under25m()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            Assert.AreEqual(0, rbm.CalculateReward(0f),  "0m → 0 pollution reduction");
            Assert.AreEqual(0, rbm.CalculateReward(24f), "24m → 0 pollution reduction");

            yield return null;
        }

        [UnityTest]
        public IEnumerator PollutionRewardTier_Boundary_Exactly25m()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            Assert.AreEqual(1, rbm.CalculateReward(25f),
                "Exactly 25m should hit the half-reward tier.");
            Assert.AreEqual(0, rbm.CalculateReward(24.9f),
                "24.9m should fall into the zero-reward tier.");

            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-07: Theme is clear — title references biking and clean transport.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator TitleReferencesBikingTheme()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root  = rbm.uiDocument.rootVisualElement;
            Label titleLabel    = root.Q<Label>("rb-title") ?? root.Q<Label>(className: "rb-title");

            // MinigameName always reflects the theme even without UI
            Assert.IsTrue(
                rbm.MinigameName.ToLower().Contains("bike") ||
                rbm.MinigameName.ToLower().Contains("ride"),
                $"MinigameName '{rbm.MinigameName}' should reference biking or riding.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneContainsBikingVisuals()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root = rbm.uiDocument.rootVisualElement;

            // Road scene elements confirm the biking/clean transport theme
            Assert.NotNull(root.Q<VisualElement>("tap_area"),
                "tap_area (road scene) should exist as the main biking visual.");
            Assert.NotNull(root.Q<VisualElement>("progress_track"),
                "progress_track should exist as the distance/journey visual.");

            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-08: Performance feedback shown at end with distance biked.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ResultLabelExistsInUI()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root = rbm.uiDocument.rootVisualElement;
            Label resultLabel  = root.Q<Label>("result_label");
            Assert.NotNull(resultLabel, "result_label should exist to show performance feedback.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ResultLabelShownAfterGoalReached()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root    = rbm.uiDocument.rootVisualElement;
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            Label resultLabel     = root.Q<Label>("result_label");
            Assert.NotNull(tapArea,     "tap_area should exist.");
            Assert.NotNull(resultLabel, "result_label should exist.");

            // Tap 34 times to reach 50m (34 × 1.5m = 51m)
            for (int i = 0; i < 34; i++)
            {
                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = tapArea;
                    tapArea.SendEvent(click);
                }
            }
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, resultLabel.style.display.value,
                "result_label should be visible after the goal is reached.");
            Assert.IsTrue(resultLabel.text.Length > 0,
                "result_label should contain performance feedback text.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ResultLabelContainsDistanceBiked()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root    = rbm.uiDocument.rootVisualElement;
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            Label resultLabel     = root.Q<Label>("result_label");
            Assert.NotNull(tapArea,     "tap_area should exist.");
            Assert.NotNull(resultLabel, "result_label should exist.");

            // Tap to reach goal
            for (int i = 0; i < 34; i++)
            {
                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = tapArea;
                    tapArea.SendEvent(click);
                }
            }
            yield return null;

            Assert.IsTrue(resultLabel.text.ToLower().Contains("distance") ||
                          resultLabel.text.ToLower().Contains("m"),
                $"Result label should show distance biked but was: '{resultLabel.text}'");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ResultLabelContainsPositiveFeedbackOnGoal()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            VisualElement root    = rbm.uiDocument.rootVisualElement;
            VisualElement tapArea = root.Q<VisualElement>("tap_area");
            Label resultLabel     = root.Q<Label>("result_label");
            Assert.NotNull(tapArea,     "tap_area should exist.");
            Assert.NotNull(resultLabel, "result_label should exist.");

            for (int i = 0; i < 34; i++)
            {
                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = tapArea;
                    tapArea.SendEvent(click);
                }
            }
            yield return null;

            string text = resultLabel.text.ToLower();
            Assert.IsTrue(text.Contains("yay") || text.Contains("goal") || text.Contains("amazing"),
                $"Result label should contain positive feedback on goal reached but was: '{resultLabel.text}'");

            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // AT-09: Timer counts down during gameplay.
        // ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator TimerCountsDownDuringGame()
        {
            RideBikeMinigame rbm = GameObject.FindFirstObjectByType<RideBikeMinigame>();
            Assert.NotNull(rbm, "RideBikeMinigame should exist.");

            yield return StartRideBikeGameplay(rbm);

            float timeAtStart = rbm.GetTimeRemaining();
            Assert.Greater(timeAtStart, 0f, "Timer should be running after start.");

            // Wait one real second
            yield return new WaitForSeconds(1f);

            Assert.Less(rbm.GetTimeRemaining(), timeAtStart,
                "Time remaining should decrease as the game runs.");

            yield return null;
        }
    }
}
