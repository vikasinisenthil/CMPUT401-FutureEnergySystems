using UnityEngine;
using UnityEngine.UIElements;

public class AccessibilitySettingsManager : MonoBehaviour
{
    public const string PREF_LARGE_TEXT = "ACC01_LargeText";
    private const string LARGE_TEXT_CLASS = "large-text";

    [Header("UI Documents")]
    [SerializeField] private UIDocument inGameUiDocument;
    [SerializeField] private UIDocument settingsUiDocument;

    [Header("Font Override Style")]
    [SerializeField] private StyleSheet largeFontStyleSheet;

    private static StyleSheet cachedLargeFontStyleSheet;
    private bool largeTextEnabled;

    public static bool IsLargeTextEnabled()
    {
        return PlayerPrefs.GetInt(PREF_LARGE_TEXT, 0) == 1;
    }

    private static StyleSheet GetSharedLargeFontStyleSheet()
    {
        if (cachedLargeFontStyleSheet == null)
        {
            cachedLargeFontStyleSheet = Resources.Load<StyleSheet>("FontLarge");
        }

        return cachedLargeFontStyleSheet;
    }

    public static void ApplyLargeTextToDocument(UIDocument doc)
    {
        if (doc == null) return;
        ApplyLargeTextToRoot(doc.rootVisualElement, null);
    }

    public static void ApplyLargeTextToDocument(UIDocument doc, StyleSheet styleSheet)
    {
        if (doc == null) return;
        ApplyLargeTextToRoot(doc.rootVisualElement, styleSheet);
    }

    public static void ApplyLargeTextToRoot(VisualElement root)
    {
        ApplyLargeTextToRoot(root, null);
    }

    public static void ApplyLargeTextToRoot(VisualElement root, StyleSheet styleSheet)
    {
        if (root == null) return;

        StyleSheet styleSheetToUse = styleSheet != null ? styleSheet : GetSharedLargeFontStyleSheet();
        if (styleSheetToUse == null) return;

        bool enabled = IsLargeTextEnabled();
        bool hasSheet = root.styleSheets.Contains(styleSheetToUse);

        if (enabled)
        {
            if (!hasSheet)
                root.styleSheets.Add(styleSheetToUse);

            root.AddToClassList(LARGE_TEXT_CLASS);
        }
        else
        {
            if (hasSheet)
                root.styleSheets.Remove(styleSheetToUse);

            root.RemoveFromClassList(LARGE_TEXT_CLASS);
        }
    }

    private void Awake()
    {
        largeTextEnabled = IsLargeTextEnabled();
    }

    private void Start()
    {
        if (settingsUiDocument == null || inGameUiDocument == null) return;

        var overlay = settingsUiDocument.rootVisualElement.Q<VisualElement>("settings_overlay");
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
        }

        HookInGameSettingsButton();
        HookSettingsUI();
        ApplyFontSettingToAllDocuments();
    }

    private void HookInGameSettingsButton()
    {
        if (inGameUiDocument == null) return;

        Button settingsBtn = inGameUiDocument.rootVisualElement.Q<Button>("settings_button");
        if (settingsBtn == null) return;

        settingsBtn.UnregisterCallback<ClickEvent>(OnInGameSettingsClicked);
        settingsBtn.RegisterCallback<ClickEvent>(OnInGameSettingsClicked);
    }

    private void OnInGameSettingsClicked(ClickEvent evt)
    {
        OpenSettings();
    }

    private void HookSettingsUI()
    {
        if (settingsUiDocument == null) return;

        VisualElement root = settingsUiDocument.rootVisualElement;

        Toggle largeToggle = root.Q<Toggle>("large_text_toggle");
        if (largeToggle != null)
        {
            largeToggle.SetValueWithoutNotify(largeTextEnabled);
            largeToggle.UnregisterValueChangedCallback(OnLargeTextToggleChanged);
            largeToggle.RegisterValueChangedCallback(OnLargeTextToggleChanged);
        }

        Button closeBtn = root.Q<Button>("close_button");
        if (closeBtn != null)
        {
            closeBtn.UnregisterCallback<ClickEvent>(OnCloseSettingsClicked);
            closeBtn.RegisterCallback<ClickEvent>(OnCloseSettingsClicked);
        }
    }

    private void OnLargeTextToggleChanged(ChangeEvent<bool> evt)
    {
        SetLargeText(evt.newValue);
    }

    private void OnCloseSettingsClicked(ClickEvent evt)
    {
        CloseSettings();
    }

    private void OpenSettings()
    {
        if (settingsUiDocument == null || inGameUiDocument == null) return;

        var overlay = settingsUiDocument.rootVisualElement.Q<VisualElement>("settings_overlay");
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
        }

        var settingsRoot = settingsUiDocument.rootVisualElement;

        Slider backgroundMusicSlider = settingsRoot.Q<Slider>("background_music");
        if (backgroundMusicSlider != null)
        {
            backgroundMusicSlider.SetValueWithoutNotify(VolumeManager.Instance.GetRawMusicVolume());
            backgroundMusicSlider.RegisterValueChangedCallback(evt => VolumeManager.Instance.SetMusicVolume(evt.newValue));
        }
        else Debug.LogError("Music Slider is null");

        Slider soundEffectSlider = settingsRoot.Q<Slider>("sound_effect_slider");
        if (soundEffectSlider != null)
        {
            soundEffectSlider.SetValueWithoutNotify(VolumeManager.Instance.GetRawSfxVolume());
            soundEffectSlider.RegisterValueChangedCallback(evt => VolumeManager.Instance.SetSfxVolume(evt.newValue));
        }
        else Debug.LogError("SFX Slider is null");

        Slider mainSlider = settingsRoot.Q<Slider>("main_slider");
        if (mainSlider != null)
        {
            mainSlider.SetValueWithoutNotify(VolumeManager.Instance.GetMainVolume());
            mainSlider.RegisterValueChangedCallback(evt => VolumeManager.Instance.SetMainVolume(evt.newValue));
        }
        else Debug.LogError("Main Slider is null");

        Toggle muteToggle = settingsRoot.Q<Toggle>("mute_toggle");
        if (muteToggle != null)
        {
            muteToggle.SetValueWithoutNotify(VolumeManager.Instance.GetMuteStatus());
            muteToggle.RegisterValueChangedCallback(evt =>
            {
                VolumeManager.Instance.SetMuteStatus(evt.newValue);

                if (evt.newValue)
                {
                    settingsRoot.Q<VisualElement>("grayed_out_box").style.opacity = 0.5f;
                    if (backgroundMusicSlider != null) backgroundMusicSlider.SetEnabled(false);
                    if (soundEffectSlider != null) soundEffectSlider.SetEnabled(false);
                    if (mainSlider != null) mainSlider.SetEnabled(false);
                }
                else
                {
                    settingsRoot.Q<VisualElement>("grayed_out_box").style.opacity = 1.0f;
                    if (backgroundMusicSlider != null) backgroundMusicSlider.SetEnabled(true);
                    if (soundEffectSlider != null) soundEffectSlider.SetEnabled(true);
                    if (mainSlider != null) mainSlider.SetEnabled(true);
                }
            });
        }
        else
        {
            Debug.Log("Mute toggle is null");
        }

        Slider ttsSlider = settingsRoot.Q<Slider>("tts_volume_slider");
        if (ttsSlider != null)
        {
            ttsSlider.SetValueWithoutNotify(VolumeManager.Instance.GetRawTTSVolume());
            ttsSlider.RegisterValueChangedCallback(evt => VolumeManager.Instance.SetTTSVolume(evt.newValue));
        }
        else Debug.LogError("TTS Slider is null");
    }

    private void CloseSettings()
    {
        if (settingsUiDocument == null || inGameUiDocument == null) return;

        var overlay = settingsUiDocument.rootVisualElement.Q<VisualElement>("settings_overlay");
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
        }
    }

    private void SetLargeText(bool enabled)
    {
        largeTextEnabled = enabled;

        PlayerPrefs.SetInt(PREF_LARGE_TEXT, largeTextEnabled ? 1 : 0);
        PlayerPrefs.Save();

        ApplyFontSettingToAllDocuments();
    }

    private void ApplyFontSettingToAllDocuments()
    {
        UIDocument[] docs = FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (UIDocument doc in docs)
        {
            ApplyLargeTextToDocument(doc, largeFontStyleSheet);
        }
    }
}