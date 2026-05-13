using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Settings {
    [Category("Settings")]
    public class SET02_Main_Volume
    {
        private AccessibilitySettingsManager FindManager()
        {
            var mgr = Object.FindObjectOfType<AccessibilitySettingsManager>(true);
            Assert.IsNotNull(mgr,
                "SET02 WIRING ERROR: No AccessibilitySettingsManager component found in the scene. " +
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

        private float GetMainVolume()
        {
            return VolumeManager.Instance.GetMainVolume();
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
        public IEnumerator SettingsUI_Has_Main_Slider()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET02 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET02 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider slider = root.Q<Slider>("main_slider");
            Assert.IsNotNull(slider,
                "Settings UI must contain a main sound volume slider");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Adjust_Slider_Changes_Volume()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET02 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET02 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider mainSlider = root.Q<Slider>("main_slider");
            mainSlider.value = 0.434f;

            Assert.AreEqual(0.434f, GetMainVolume());

            yield return null;
        }

        [UnityTest]
        public IEnumerator Zero_Volume_Mutes_All_Sound()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET02 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET02 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider mainSlider = root.Q<Slider>("main_slider");
            mainSlider.value = 0.0f;

            Slider sfxSlider = root.Q<Slider>("sound_effect_slider");
            sfxSlider.value = 0.3f;
            
            Slider musicSlider = root.Q<Slider>("background_music");
            musicSlider.value = 0.5345f;

            Assert.AreEqual(0.0f, GetMainVolume());
            Assert.AreEqual(0.0f, VolumeManager.Instance.GetAdjustedMusicVolume());
            Assert.AreEqual(0.0f, VolumeManager.Instance.GetAdjustedSfxVolume());

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Volume_Save()
        {
            VolumeManager.Instance.SetMainVolume(0.343f);

            var vm = VolumeManager.Instance;

            UnitySetUp();

            Assert.AreEqual(0.343f, GetMainVolume());

            yield return null;
        }

        [UnityTest]
        public IEnumerator Main_Volume_Does_Not_Change_Other_Settings_Directly()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET02 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET02 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider mainSlider = root.Q<Slider>("main_slider");
            mainSlider.value = 0.5f;

            Slider musicSlider = root.Q<Slider>("background_music");
            musicSlider.value = 0.323f;

            Slider sfxSlider = root.Q<Slider>("sound_effect_slider");
            sfxSlider.value = 0.434f;

            Assert.AreEqual(0.434f, VolumeManager.Instance.GetRawSfxVolume());
            Assert.AreEqual(0.323f, VolumeManager.Instance.GetRawMusicVolume());

            yield return null;
        }

        [UnityTest]
        public IEnumerator Main_Volume_Acts_As_Multiplier()
        {
            var mgr = FindManager();
            UIDocument inGameDoc = GetField<UIDocument>(mgr, "inGameUiDocument");
            UIDocument settingsDoc = GetField<UIDocument>(mgr, "settingsUiDocument");
            Assert.IsNotNull(inGameDoc,
                "SET02 WIRING ERROR: inGameUiDocument is not assigned.");
            Assert.IsNotNull(settingsDoc,
                "SET02 WIRING ERROR: settingsUiDocument is not assigned on AccessibilitySettingsManager.");

            yield return OpenSettingsPanel(inGameDoc);

            VisualElement root = settingsDoc.rootVisualElement;
            Assert.IsNotNull(root, "Settings UIDocument rootVisualElement should not be null.");

            Slider mainSlider = root.Q<Slider>("main_slider");
            mainSlider.value = 0.5f;

            Slider musicSlider = root.Q<Slider>("background_music");
            musicSlider.value = 1.0f;

            Slider sfxSlider = root.Q<Slider>("sound_effect_slider");
            sfxSlider.value = 1.0f;

            Assert.AreEqual(0.5f, VolumeManager.Instance.GetAdjustedSfxVolume());
            Assert.AreEqual(0.5f, VolumeManager.Instance.GetAdjustedMusicVolume());

            yield return null;
        }
    }
}