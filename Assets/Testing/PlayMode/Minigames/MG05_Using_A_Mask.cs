using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Minigames
{
    [Category("Minigames")]
    public class MG05_Using_A_Mask
    {
        private const string UseMaskUxmlPath = "Assets/UI Toolkit/Minigame/UseMaskMinigame.uxml";

        private GameObject testObject;
        private PanelSettings panelSettings;
        private UIDocument uiDocument;
        private UseMaskMinigame useMaskMinigame;

        private VisualElement root;
        private VisualElement playArea;
        private VisualElement faceDropZone;
        private VisualElement correctMask;
        private VisualElement wrongMask1;
        private VisualElement wrongMask2;

        private Label attemptsLabel;
        private Label resultLabel;
        private Label roundResultLabel;

        private static void Click(VisualElement target)
        {
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = target;
                target.SendEvent(click);
            }
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UseMaskUxmlPath);
            Assert.NotNull(visualTree, $"Could not load VisualTreeAsset at path: {UseMaskUxmlPath}");

            testObject = new GameObject("MG05_TestObject");

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            uiDocument = testObject.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = visualTree;

            useMaskMinigame = testObject.AddComponent<UseMaskMinigame>();
            useMaskMinigame.uiDocument = uiDocument;
            useMaskMinigame.maxAttempts = 2;

            yield return null;
            yield return null;
            yield return null;

            useMaskMinigame.StartMinigame();

            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            Assert.NotNull(useMaskMinigame.uiDocument, "UseMaskMinigame should have a UIDocument assigned.");
            root = useMaskMinigame.uiDocument.rootVisualElement;
            Assert.NotNull(root, "UseMaskMinigame rootVisualElement should not be null.");

            Button beginButton = root.Q<Button>("begin_button");
            if (beginButton != null)
            {
                using (ClickEvent beginClick = ClickEvent.GetPooled())
                {
                    beginClick.target = beginButton;
                    beginButton.SendEvent(beginClick);
                }
            }

            yield return null;
            yield return null;

            playArea = root.Q<VisualElement>("play_area");
            faceDropZone = root.Q<VisualElement>("face_drop_zone");
            correctMask = root.Q<VisualElement>("mask_correct");
            wrongMask1 = root.Q<VisualElement>("mask_wrong_1");
            wrongMask2 = root.Q<VisualElement>("mask_wrong_2");

            attemptsLabel = root.Q<Label>("attempts_label");
            roundResultLabel = root.Q<Label>("round_result_label");
            resultLabel = root.Q<Label>("result_label");

            Assert.NotNull(playArea, "play_area should exist.");
            Assert.NotNull(faceDropZone, "face_drop_zone should exist.");
            Assert.NotNull(correctMask, "mask_correct should exist.");
            Assert.NotNull(wrongMask1, "mask_wrong_1 should exist.");
            Assert.NotNull(wrongMask2, "mask_wrong_2 should exist.");
            Assert.NotNull(attemptsLabel, "attempts_label should exist.");
            Assert.NotNull(roundResultLabel, "round_result_label should exist.");
            Assert.NotNull(resultLabel, "result_label should exist.");

            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            if (testObject != null)
            {
                UnityEngine.Object.Destroy(testObject);
            }

            if (panelSettings != null)
            {
                UnityEngine.Object.Destroy(panelSettings);
            }

            yield return null;
        }

        private MethodInfo GetPrivateMethod(string methodName)
        {
            MethodInfo method = typeof(UseMaskMinigame).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.NotNull(method, $"Private method '{methodName}' was not found.");
            return method;
        }

        private void InvokePrivate(string methodName, params object[] args)
        {
            MethodInfo method = GetPrivateMethod(methodName);
            method.Invoke(useMaskMinigame, args);
        }

        private T GetPrivateField<T>(string fieldName)
        {
            FieldInfo field = typeof(UseMaskMinigame).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.NotNull(field, $"Private field '{fieldName}' was not found.");
            return (T)field.GetValue(useMaskMinigame);
        }

        private IEnumerator WaitUntilOrTimeout(Func<bool> condition, float timeoutSeconds = 10.0f)
        {
            float startTime = Time.realtimeSinceStartup;

            while (!condition() && Time.realtimeSinceStartup - startTime < timeoutSeconds)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator UseMaskMinigameExistsInScene()
        {
            Assert.NotNull(useMaskMinigame, "UseMaskMinigame should exist.");
            Assert.IsTrue(useMaskMinigame.IsActive, "UseMaskMinigame should be active after StartMinigame().");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MinigameShowsFaceAndThreeMaskOptions()
        {
            Assert.NotNull(faceDropZone, "Face drop zone should be present.");
            Assert.NotNull(correctMask, "Correct mask should be present.");
            Assert.NotNull(wrongMask1, "Wrong mask 1 should be present.");
            Assert.NotNull(wrongMask2, "Wrong mask 2 should be present.");

            Label title = root.Q<Label>("minigame_title");
            Label desc = root.Q<Label>("minigame_description");

            Assert.NotNull(title, "minigame_title should exist.");
            Assert.NotNull(desc, "minigame_description should exist.");
            Assert.True(title.text.Contains("Use a Mask"), "Title should mention 'Use a Mask'.");
            Assert.True(desc.text.ToLower().Contains("mask"), "Description should mention masks.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator IncorrectMaskResetsToStartAndShowsFeedback()
        {
            InvokePrivate("InitializeMaskPositions");
            for (int i = 0; i < 10; i++) {
                yield return null;
            }

            Dictionary<VisualElement, Vector2> startPositions =
                GetPrivateField<Dictionary<VisualElement, Vector2>>("startPositions");

            Assert.True(startPositions.ContainsKey(wrongMask1), "Wrong mask 1 should have a cached start position.");
            Vector2 cachedStartPos = startPositions[wrongMask1];

            wrongMask1.style.left = cachedStartPos.x + 60f;
            wrongMask1.style.top = cachedStartPos.y - 20f;
            for (int i = 0; i < 10; i++) {
                yield return null;
            }

            InvokePrivate("HandleIncorrectMaskDropped", wrongMask1);
            for (int i = 0; i < 10; i++) {
                yield return null;
            }

            Assert.AreEqual("Attempts Left: 1", attemptsLabel.text, "Attempts label should decrease after one wrong attempt.");
            Assert.True(
                roundResultLabel.text.Contains("Incorrect"),
                $"Round result label should show incorrect feedback, but was: {roundResultLabel.text}"
            );
        }

        [UnityTest]
        public IEnumerator CorrectMaskSnapsToFace()
        {
            Vector2 startPos = new Vector2(correctMask.resolvedStyle.left, correctMask.resolvedStyle.top);

            InvokePrivate("HandleCorrectMaskDropped");
            yield return null;

            float maskWidth = correctMask.resolvedStyle.width;
            float maskHeight = correctMask.resolvedStyle.height;
            Rect faceRect = faceDropZone.layout;

            float expectedLeft = faceRect.x + (faceRect.width - maskWidth) * 0.5f;
            float expectedTop = faceRect.y + faceRect.height * 0.62f - maskHeight * 0.5f;

            Vector2 snappedPos = new Vector2(correctMask.resolvedStyle.left, correctMask.resolvedStyle.top);

            Assert.AreNotEqual(startPos.x, snappedPos.x, "Correct mask should move from its starting X position.");
            Assert.AreNotEqual(startPos.y, snappedPos.y, "Correct mask should move from its starting Y position.");
            Assert.AreEqual(expectedLeft, snappedPos.x, 0.5f, "Correct mask should snap to the expected X position on the face.");
            Assert.AreEqual(expectedTop, snappedPos.y, 0.5f, "Correct mask should snap to the expected Y position on the face.");
        }

        [UnityTest]
        public IEnumerator SuccessCompletesMinigameWithPollutionReductionOfOne()
        {
            int completionValue = int.MinValue;
            bool completed = false;

            useMaskMinigame.OnMinigameComplete += value =>
            {
                completionValue = value;
                completed = true;
            };

            InvokePrivate("HandleCorrectMaskDropped");
            yield return null;

            Button exit = root.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist on end page");
            Click(exit);
            yield return WaitUntilOrTimeout(() => completed);

            Assert.IsTrue(completed, "Minigame should complete after correct mask is dropped.");
            Assert.AreEqual(1, completionValue, "Successful completion should return pollution reduction of 1.");
            Assert.IsFalse(useMaskMinigame.IsActive, "Minigame should no longer be active after success.");
        }

        [UnityTest]
        public IEnumerator FailureAfterTwoIncorrectAttemptsCompletesWithNoPollutionChange()
        {
            int completionValue = int.MinValue;
            bool completed = false;

            useMaskMinigame.OnMinigameComplete += value =>
            {
                completionValue = value;
                completed = true;
            };

            InvokePrivate("HandleIncorrectMaskDropped", wrongMask1);
            yield return null;

            InvokePrivate("HandleIncorrectMaskDropped", wrongMask2);
            yield return null;

            Button exit = root.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist on end page");
            Click(exit);
            yield return WaitUntilOrTimeout(() => completed);

            Assert.IsTrue(completed, "Minigame should complete after running out of attempts.");
            Assert.AreEqual(0, completionValue, "Failure should return 0 pollution change.");
            Assert.AreEqual("Attempts Left: 0", attemptsLabel.text, "Attempts label should show 0 after all attempts are used.");
            Assert.IsFalse(useMaskMinigame.IsActive, "Minigame should no longer be active after failure.");
        }

        [UnityTest]
        public IEnumerator CorrectMaskWaitsBeforeShowingEndPage()
        {
            useMaskMinigame.successResultDelay = 0.2f;

            InvokePrivate("HandleCorrectMaskDropped");
            yield return null;

            VisualElement resultContainer = root.Q<VisualElement>("result_container");
            Assert.NotNull(resultContainer, "result_container should exist.");
            Assert.AreNotEqual(
                DisplayStyle.Flex,
                resultContainer.style.display.value,
                "End page should not show immediately after correct drop."
            );

            yield return new WaitForSeconds(0.25f);

            Assert.AreEqual(
                DisplayStyle.Flex,
                resultContainer.style.display.value,
                "End page should appear after the configured success delay."
            );
        }

        [UnityTest]
        public IEnumerator TimeUpThenExitCompletesWithZeroReduction()
        {
            int completionValue = int.MinValue;
            bool completed = false;
            bool exited = false;

            useMaskMinigame.OnMinigameComplete += value =>
            {
                completionValue = value;
                completed = true;
            };
            useMaskMinigame.OnMinigameExited += () => exited = true;

            useMaskMinigame.timeLimit = 0.1f;
            InvokePrivate("StartRound");
            yield return null;
            yield return new WaitForSeconds(0.2f);

            Button exit = root.Q<Button>("exit_button");
            Assert.NotNull(exit, "exit_button should exist on end page.");
            Click(exit);
            yield return WaitUntilOrTimeout(() => completed);

            Assert.IsTrue(completed, "OnMinigameComplete should fire after time-up end-page exit.");
            Assert.IsFalse(exited, "OnMinigameExited should not fire from end-page exit.");
            Assert.AreEqual(0, completionValue, "Time-up should complete with zero pollution reduction.");
        }
    }
}
