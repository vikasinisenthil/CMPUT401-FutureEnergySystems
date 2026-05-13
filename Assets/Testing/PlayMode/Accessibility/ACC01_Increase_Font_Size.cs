using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Accessibility {
    [Category("Accessibility")]
    public class ACC01_Increase_Font_Size
    {
        private AccessibilitySettingsManager FindManager()
        {
            var mgr = Object.FindObjectOfType<AccessibilitySettingsManager>(true);
            Assert.IsNotNull(mgr,
                "ACC01 WIRING ERROR: No AccessibilitySettingsManager component found in the scene. " +
                "Add the component to a GameObject (e.g. GameController) and assign its references.");
            return mgr;
        }

        private T GetField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {obj.GetType().Name}");
            return (T)field.GetValue(obj);
        }

        private IEnumerator OpenSettingsPanel(UIDocument inGameDoc)
        {
            Button settingsBtn = inGameDoc.rootVisualElement.Q<Button>("settings_button");
            Assert.IsNotNull(settingsBtn, "settings_button not found in InGame UI.");
            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = settingsBtn;
                settingsBtn.SendEvent(click);
            }
            yield return null;
            yield return null;
        }

        private IEnumerator WaitForUIInit()
        {
            yield return null;
            yield return null;
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            PlayerPrefs.SetInt("ACC01_LargeText", 0);
            PlayerPrefs.Save();

            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;
            yield return WaitForUIInit();
        }

        [UnityTest]
        public IEnumerator InGameUI_Has_SettingsButton()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            Assert.IsNotNull(inGameDoc,
                "ACC01 WIRING ERROR: inGameUiDocument is not assigned on AccessibilitySettingsManager.");

            Button settingsBtn = inGameDoc.rootVisualElement.Q<Button>("settings_button");
            Assert.IsNotNull(settingsBtn,
                "InGame UI must contain a Button named 'settings_button'.");
            Assert.IsTrue(settingsBtn.enabledInHierarchy,
                "settings_button should be enabled.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsUI_Has_Toggle_And_CloseButton()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "ACC01 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "ACC01 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Toggle toggle = root.Q<Toggle>("large_text_toggle");
            Assert.IsNotNull(toggle,
                "Settings UI must contain a Toggle named 'large_text_toggle'.");

            Button closeBtn = root.Q<Button>("close_button");
            Assert.IsNotNull(closeBtn,
                "Settings UI must contain a Button named 'close_button'.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ClickSettingsButton_ShowsSettings_HidesInGame()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "ACC01 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "ACC01 WIRING ERROR: settingsUiDocument is not assigned.");

            Button settingsBtn = inGameDoc.rootVisualElement.Q<Button>("settings_button");
            Assert.IsNotNull(settingsBtn, "settings_button not found in InGame UI.");

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = settingsBtn;
                settingsBtn.SendEvent(click);
            }

            yield return null;
            
            var settingsOverlay = settingsDoc.rootVisualElement.Q<VisualElement>("settings_overlay");
            Assert.IsNotNull(settingsOverlay, "settings_overlay not found in settings UI");
            Assert.AreEqual(DisplayStyle.Flex,
                settingsOverlay.resolvedStyle.display,
                "Settings overlay should be visible after clicking settings_button.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ToggleLargeText_AppliesFontLargeStyleSheet()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            StyleSheet largeFontSheet = GetField<StyleSheet>(mgr, "largeFontStyleSheet");
            Assert.IsNotNull(inGameDoc,
                "ACC01 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "ACC01 WIRING ERROR: settingsUiDocument is not assigned.");
            Assert.IsNotNull(largeFontSheet,
                "ACC01 WIRING ERROR: largeFontStyleSheet is not assigned.");

            yield return OpenSettingsPanel(inGameDoc);

            Toggle toggle = settingsDoc.rootVisualElement.Q<Toggle>("large_text_toggle");
            Assert.IsNotNull(toggle, "large_text_toggle not found in Settings UI.");

            toggle.value = true;

            yield return null;

            Assert.IsTrue(
                settingsDoc.rootVisualElement.styleSheets.Contains(largeFontSheet),
                "FontLarge stylesheet should be applied to Settings rootVisualElement after toggle ON.");
            Assert.IsTrue(
                inGameDoc.rootVisualElement.styleSheets.Contains(largeFontSheet),
                "FontLarge stylesheet should be applied to InGame rootVisualElement after toggle ON.");

            toggle.value = false;

            yield return null;

            Assert.IsFalse(
                settingsDoc.rootVisualElement.styleSheets.Contains(largeFontSheet),
                "FontLarge stylesheet should be removed from Settings rootVisualElement after toggle OFF.");
            Assert.IsFalse(
                inGameDoc.rootVisualElement.styleSheets.Contains(largeFontSheet),
                "FontLarge stylesheet should be removed from InGame rootVisualElement after toggle OFF.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Persistence_LargeText_AppliedOnReload()
        {
            PlayerPrefs.SetInt("ACC01_LargeText", 1);
            PlayerPrefs.Save();

            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;
            yield return WaitForUIInit();

            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            StyleSheet largeFontSheet = GetField<StyleSheet>(mgr, "largeFontStyleSheet");
            Assert.IsNotNull(inGameDoc,
                "ACC01 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(largeFontSheet,
                "ACC01 WIRING ERROR: largeFontStyleSheet is not assigned.");

            Assert.IsTrue(
                inGameDoc.rootVisualElement.styleSheets.Contains(largeFontSheet),
                "FontLarge stylesheet should be applied automatically when ACC01_LargeText pref is 1.");

            PlayerPrefs.SetInt("ACC01_LargeText", 0);
            PlayerPrefs.Save();

            yield return null;
        }
    }}