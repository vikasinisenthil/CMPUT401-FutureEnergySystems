using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Settings {
    [Category("Settings")]
    public class SET03_SFX_Volume
    {
        private AccessibilitySettingsManager FindManager()
        {
            var mgr = Object.FindObjectOfType<AccessibilitySettingsManager>(true);
            Assert.IsNotNull(mgr,
                "SET03 WIRING ERROR: No AccessibilitySettingsManager component found in the scene. " +
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

        private float GetSFXVolume()
        {
            return VolumeManager.Instance.GetRawSfxVolume();
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("Assets/Scenes/MainMenu.unity", LoadSceneMode.Single);

            yield return null;

            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsUI_Has_SFX_Slider()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET03 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET03 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider slider = root.Q<Slider>("sound_effect_slider");
            Assert.IsNotNull(slider,
                "Settings UI must contain a sound effect volume slider");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Adjust_Slider_Changes_Volume()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET03 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET03 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider mainSlider = root.Q<Slider>("main_slider");
            mainSlider.value = 1.0f;

            Slider musicSlider = root.Q<Slider>("sound_effect_slider");
            musicSlider.value = 0.434f;

            Assert.AreEqual(0.434f, GetSFXVolume());

            yield return null;
        }

        [UnityTest]
        public IEnumerator Zero_Volume_Mutes_Sound_Effects()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET03 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET03 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider mainSlider = root.Q<Slider>("main_slider");
            mainSlider.value = 1.0f;

            Slider musicSlider = root.Q<Slider>("sound_effect_slider");
            musicSlider.value = 0.0f;

            Assert.AreEqual(0.0f, GetSFXVolume());

            yield return null;
        }

        [UnityTest]
        public IEnumerator Zero_Main_Volume_Mutes_Sound_Effects()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET03 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET03 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider mainSlider = root.Q<Slider>("main_slider");
            mainSlider.value = 0.0f;

            Slider musicSlider = root.Q<Slider>("sound_effect_slider");
            musicSlider.value = 1.0f;

            Assert.AreEqual(0.0f, VolumeManager.Instance.GetAdjustedSfxVolume());

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Volume_Save()
        {
            VolumeManager.Instance.SetSfxVolume(0.343f);

            var vm = VolumeManager.Instance;

            UnitySetUp();

            Assert.AreEqual(0.343f, GetSFXVolume());

            yield return null;
        }

        [UnityTest]
        public IEnumerator SFX_Slider_Does_Not_Effect_Music()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET03 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET03 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider mainSlider = root.Q<Slider>("main_slider");
            mainSlider.value = 1.0f;

            Slider musicSlider = root.Q<Slider>("background_music");
            musicSlider.value = 0.323f;

            Slider sfxSlider = root.Q<Slider>("sound_effect_slider");
            sfxSlider.value = 0.434f;

            Assert.AreEqual(0.434f, GetSFXVolume());
            Assert.AreEqual(0.323f, VolumeManager.Instance.GetRawMusicVolume());

            yield return null;
        }
    }
}