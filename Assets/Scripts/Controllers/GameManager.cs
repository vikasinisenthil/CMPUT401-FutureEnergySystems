using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public enum GameMode { Singleplayer, Multiplayer }
public enum HeroType { Cyclist, Scientist, Ranger }
public enum Difficulty { Easy, Medium, Hard }
public enum PlayerColor { Blue, Red, Yellow }

public class GameManager : MonoBehaviour
{
    private const string CreditsScrollName = "credits_scroll";
    private const float CreditsAutoScrollSpeed = 18f;
    private const float CreditsAutoScrollPauseAfterManualInput = 1.5f;
    private const float CreditsAutoScrollStartDelay = 0.5f;

    public static GameManager Instance;
    public UIDocument mainMenuUiDocument;
    public UIDocument settingsUiDocument;
    public UIDocument howToPlayUiDocument;
    public UIDocument creditsUiDocument;

    public Difficulty difficulty;
    public GameMode Mode;
    public int PlayerCount;
    public HeroType[] SelectedHeroes;
    public int CurrentPickIndex;

    [Header("Remi Sprites")]
    public CharacterSprites remiBlueSprites;
    public CharacterSprites remiRedSprites;
    public CharacterSprites remiYellowSprites;

    [Header("Reg Sprites")]
    public CharacterSprites regBlueSprites;
    public CharacterSprites regRedSprites;
    public CharacterSprites regYellowSprites;

    [Header("Tommy Sprites")]
    public CharacterSprites tommyBlueSprites;
    public CharacterSprites tommyRedSprites;
    public CharacterSprites tommyYellowSprites;

    private ScrollView creditsScrollView;
    private float creditsAutoScrollResumeTime;
    private bool suppressCreditsManualScrollPause;

    private void Awake()
    {
        if (Instance != null) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        AdvanceCreditsAutoScroll();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (mainMenuUiDocument != null)
            {
                mainMenuUiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            }

            if (settingsUiDocument != null)
            {
                settingsUiDocument.rootVisualElement.style.display = DisplayStyle.None;
            }
            if (howToPlayUiDocument != null)
            {
                howToPlayUiDocument.rootVisualElement.style.display = DisplayStyle.None;
            }

            if (creditsUiDocument != null)
            {
                creditsUiDocument.rootVisualElement.style.display = DisplayStyle.None;
            }

            AccessibilitySettingsManager.ApplyLargeTextToDocument(mainMenuUiDocument);
            AccessibilitySettingsManager.ApplyLargeTextToDocument(settingsUiDocument);
            AccessibilitySettingsManager.ApplyLargeTextToDocument(howToPlayUiDocument);
            AccessibilitySettingsManager.ApplyLargeTextToDocument(creditsUiDocument);

            RegisterMainMenuButtons();
            HookLargeTextToggle();
            HookSettingsCloseButton();
            HookCreditsCloseButton();
            HookCreditsScrollView();
        }
        else
        {
            if (mainMenuUiDocument != null)
            {
                mainMenuUiDocument.rootVisualElement.style.display = DisplayStyle.None;
            }

            if (settingsUiDocument != null)
            {
                settingsUiDocument.rootVisualElement.style.display = DisplayStyle.None;
            }

            if (howToPlayUiDocument != null)
            {
                howToPlayUiDocument.rootVisualElement.style.display = DisplayStyle.None;
            }

            if (creditsUiDocument != null)
            {
                creditsUiDocument.rootVisualElement.style.display = DisplayStyle.None;
            }
        }
    }
    void Start()
    {
        RegisterMainMenuButtons();
        HookSettingsCloseButton();
        HookHowToPlayCloseButton();
        HookCreditsCloseButton();
        HookCreditsScrollView();
        HookLargeTextToggle();

        AccessibilitySettingsManager.ApplyLargeTextToDocument(mainMenuUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(settingsUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(howToPlayUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(creditsUiDocument);

        if (settingsUiDocument != null)
        {
            settingsUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        if (howToPlayUiDocument != null)
        {
            howToPlayUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        if (creditsUiDocument != null)
        {
            creditsUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void RegisterMainMenuButtons()
    {
        if (mainMenuUiDocument == null)
        {
            Debug.LogWarning("mainMenuUiDocument is not assigned!");
            return;
        }

        var root = mainMenuUiDocument.rootVisualElement;
        
        if (root == null)
        {
            Debug.LogWarning("mainMenuUiDocument root is null!");
            return;
        }

        Button singleplayerButton = root.Q<Button>("singleplayer_button");
        Button twoplayerButton = root.Q<Button>("twoplayer_button");
        Button threeplayerButton = root.Q<Button>("threeplayer_button");
        Button settingsButton = root.Q<Button>("settings_button");
        Button howToPlayButton = root.Q<Button>("howtoplay_button");
        Button creditsButton = root.Q<Button>("credits_button");

        if (singleplayerButton != null)
        {
            singleplayerButton.UnregisterCallback<ClickEvent>(OnSinglePlayerClickedEvent);
            singleplayerButton.RegisterCallback<ClickEvent>(OnSinglePlayerClickedEvent);
        }

        if (twoplayerButton != null)
        {
            twoplayerButton.UnregisterCallback<ClickEvent>(OnTwoPlayerClickedEvent);
            twoplayerButton.RegisterCallback<ClickEvent>(OnTwoPlayerClickedEvent);
        }

        if (threeplayerButton != null)
        {
            threeplayerButton.UnregisterCallback<ClickEvent>(OnThreePlayerClickedEvent);
            threeplayerButton.RegisterCallback<ClickEvent>(OnThreePlayerClickedEvent);
        }

        if (settingsButton != null)
        {
            settingsButton.UnregisterCallback<ClickEvent>(OnSettingsClickedEvent);
            settingsButton.RegisterCallback<ClickEvent>(OnSettingsClickedEvent);
        }

        if (howToPlayButton != null)
        {
            howToPlayButton.UnregisterCallback<ClickEvent>(OnHowToPlayClickedEvent);
            howToPlayButton.RegisterCallback<ClickEvent>(OnHowToPlayClickedEvent);
        }

        if (creditsButton != null)
        {
            creditsButton.UnregisterCallback<ClickEvent>(OnCreditsClickedEvent);
            creditsButton.RegisterCallback<ClickEvent>(OnCreditsClickedEvent);
        }

        HookSettingsCloseButton();
        HookHowToPlayCloseButton();
        HookCreditsCloseButton();
    }

    private void OnSinglePlayerClickedEvent(ClickEvent evt)
    {
        OnSinglePlayerClicked();
    }

    private void OnTwoPlayerClickedEvent(ClickEvent evt)
    {
        OnTwoPlayerClicked();
    }

    private void OnThreePlayerClickedEvent(ClickEvent evt)
    {
        OnThreePlayerClicked();
    }

    private void OnSettingsClickedEvent(ClickEvent evt)
    {
        OnSettingsClicked();
    }

    private void OnSettingsCloseClickedEvent(ClickEvent evt)
    {
        CloseSettings();
    }

    private void OnHowToPlayClickedEvent(ClickEvent evt)
    {
        OnHowToPlayClicked();
    }

    private void OnHowToPlayCloseClickedEvent(ClickEvent evt)
    {
        CloseHowToPlay();
    }

    private void OnCreditsClickedEvent(ClickEvent evt)
    {
        OnCreditsClicked();
    }

    private void OnCreditsCloseClickedEvent(ClickEvent evt)
    {
        CloseCredits();
    }

    public void OnSinglePlayerClicked()
    {
        Debug.Log("Single Player Clicked");
        AudioManager.Instance.PlayConfirm();
        Mode = GameMode.Singleplayer;
        PlayerCount = 1;
        SelectedHeroes = new HeroType[1];
        CurrentPickIndex = 0;
        SceneManager.LoadScene("CharacterSelect");
    }

    public void OnTwoPlayerClicked()
    {
        Debug.Log("Two Player Clicked");
        AudioManager.Instance.PlayConfirm();
        Mode = GameMode.Multiplayer;
        PlayerCount = 2;
        SelectedHeroes = new HeroType[2];
        CurrentPickIndex = 0;
        SceneManager.LoadScene("CharacterSelect");
    }

    public void OnThreePlayerClicked()
    {
        Debug.Log("Three Player Clicked");
        AudioManager.Instance.PlayConfirm();
        Mode = GameMode.Multiplayer;
        PlayerCount = 3;
        SelectedHeroes = new HeroType[3];
        CurrentPickIndex = 0;
        SceneManager.LoadScene("CharacterSelect");
    }

    private void OnSettingsClicked()
    {
        Debug.Log("Settings Clicked");
        AudioManager.Instance.PlayConfirm();
        
        if (settingsUiDocument == null)
        {
            Debug.LogError("settingsUiDocument is null!");
            return;
        }
        
        if (mainMenuUiDocument == null)
        {
            Debug.LogError("mainMenuUiDocument is null!");
            return;
        }
        
        var settingsRoot = settingsUiDocument.rootVisualElement;
        var mainMenuRoot = mainMenuUiDocument.rootVisualElement;
        
        if (settingsRoot == null)
        {
            Debug.LogError("settingsUiDocument rootVisualElement is null!");
            return;
        }
        
        if (mainMenuRoot == null)
        {
            Debug.LogError("mainMenuUiDocument rootVisualElement is null!");
            return;
        }
        
        settingsRoot.style.display = DisplayStyle.Flex;
        HookLargeTextToggle();
        AccessibilitySettingsManager.ApplyLargeTextToDocument(mainMenuUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(settingsUiDocument);

        Slider backgroundMusicSlider = settingsRoot.Q<Slider>("background_music");
        if (backgroundMusicSlider != null)
        {
            backgroundMusicSlider.SetValueWithoutNotify(VolumeManager.Instance.GetRawMusicVolume());
            backgroundMusicSlider.RegisterValueChangedCallback(evt => VolumeManager.Instance.SetMusicVolume(evt.newValue));
        }

        Slider soundEffectSlider = settingsRoot.Q<Slider>("sound_effect_slider");
        if (soundEffectSlider != null)
        {
            soundEffectSlider.SetValueWithoutNotify(VolumeManager.Instance.GetRawSfxVolume());
            soundEffectSlider.RegisterValueChangedCallback(evt => VolumeManager.Instance.SetSfxVolume(evt.newValue));
        }

        Slider mainSlider = settingsRoot.Q<Slider>("main_slider");
        if (mainSlider != null)
        {
            mainSlider.SetValueWithoutNotify(VolumeManager.Instance.GetMainVolume());
            mainSlider.RegisterValueChangedCallback(evt => VolumeManager.Instance.SetMainVolume(evt.newValue));
        }

        Toggle muteToggle = settingsRoot.Q<Toggle>("mute_toggle");
        if (muteToggle != null)
        {
            Debug.Log("Setup mute toggle");
            muteToggle.SetValueWithoutNotify(VolumeManager.Instance.GetMuteStatus());
            muteToggle.RegisterValueChangedCallback(evt =>
            {
                VolumeManager.Instance.SetMuteStatus(evt.newValue);

                if (evt.newValue)
                {
                    settingsRoot.Q<VisualElement>("grayed_out_box").style.opacity = 0.5f;
                    backgroundMusicSlider.SetEnabled(false);
                    soundEffectSlider.SetEnabled(false);
                    mainSlider.SetEnabled(false);
                } else
                {
                    settingsRoot.Q<VisualElement>("grayed_out_box").style.opacity = 1.0f;
                    backgroundMusicSlider.SetEnabled(true);
                    soundEffectSlider.SetEnabled(true);
                    mainSlider.SetEnabled(true);
                }
            });
        } else
        {
            Debug.Log("Mute toggle is null");
        }

        Slider ttsSlider = settingsRoot.Q<Slider>("tts_volume_slider");
        if (ttsSlider != null)
        {
            ttsSlider.SetValueWithoutNotify(VolumeManager.Instance.GetRawTTSVolume());
            ttsSlider.RegisterValueChangedCallback(evt => VolumeManager.Instance.SetTTSVolume(evt.newValue));
        } else Debug.LogError("TTS Slider is null");
    }

    private void CloseSettings()
    {
        Debug.Log("Settings Closed");
        AudioManager.Instance.PlayConfirm();
        
        if (settingsUiDocument != null && mainMenuUiDocument != null)
        {
            settingsUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void OnHowToPlayClicked()
    {
        Debug.Log("How to Play Clicked");
        AudioManager.Instance.PlayConfirm();

        if (howToPlayUiDocument == null)
        {
            Debug.LogError("howToPlayUiDocument is null!");
            return;
        }

        var htpRoot = howToPlayUiDocument.rootVisualElement;
        if (htpRoot == null)
        {
            Debug.LogError("howToPlayUiDocument rootVisualElement is null!");
            return;
        }

        htpRoot.style.display = DisplayStyle.Flex;
        AccessibilitySettingsManager.ApplyLargeTextToDocument(howToPlayUiDocument);
    }

    private void CloseHowToPlay()
    {
        Debug.Log("How to Play Closed");
        AudioManager.Instance.PlayConfirm();

        if (howToPlayUiDocument != null)
        {
            howToPlayUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void HookHowToPlayCloseButton()
    {
        if (howToPlayUiDocument == null)
        {
            Debug.LogWarning("howToPlayUiDocument is null");
            return;
        }

        var htpRoot = howToPlayUiDocument.rootVisualElement;
        if (htpRoot == null)
        {
            Debug.LogWarning("howToPlayUiDocument rootVisualElement is null");
            return;
        }

        Button closeButton = htpRoot.Q<Button>("close_button");
        if (closeButton != null)
        {
            closeButton.UnregisterCallback<ClickEvent>(OnHowToPlayCloseClickedEvent);
            closeButton.RegisterCallback<ClickEvent>(OnHowToPlayCloseClickedEvent);
        }
        else
        {
            Debug.LogWarning("close_button not found in How to Play UI");
        }
    }

    private void OnCreditsClicked()
    {
        Debug.Log("Credits Clicked");
        AudioManager.Instance.PlayConfirm();

        if (creditsUiDocument == null)
        {
            Debug.LogError("creditsUiDocument is null!");
            return;
        }

        var creditsRoot = creditsUiDocument.rootVisualElement;

        if (creditsRoot == null)
        {
            Debug.LogError("creditsUiDocument rootVisualElement is null!");
            return;
        }

        creditsRoot.style.display = DisplayStyle.Flex;
        AccessibilitySettingsManager.ApplyLargeTextToDocument(creditsUiDocument);
        ResetCreditsScrollPosition();
    }

    private void CloseCredits()
    {
        Debug.Log("Credits Closed");
        AudioManager.Instance.PlayConfirm();

        if (creditsUiDocument != null)
        {
            creditsUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void HookCreditsCloseButton()
    {
        if (creditsUiDocument == null)
        {
            Debug.LogWarning("creditsUiDocument is null");
            return;
        }

        var creditsRoot = creditsUiDocument.rootVisualElement;

        if (creditsRoot == null)
        {
            Debug.LogWarning("creditsUiDocument rootVisualElement is null");
            return;
        }

        Button closeButton = creditsRoot.Q<Button>("credits_close_button");

        if (closeButton != null)
        {
            closeButton.UnregisterCallback<ClickEvent>(OnCreditsCloseClickedEvent);
            closeButton.RegisterCallback<ClickEvent>(OnCreditsCloseClickedEvent);
        }
        else
        {
            Debug.LogWarning("credits_close_button not found in credits UI");
        }
    }

    private void HookCreditsScrollView()
    {
        if (creditsUiDocument == null)
        {
            creditsScrollView = null;
            return;
        }

        var creditsRoot = creditsUiDocument.rootVisualElement;
        if (creditsRoot == null)
        {
            creditsScrollView = null;
            return;
        }

        creditsScrollView = creditsRoot.Q<ScrollView>(CreditsScrollName);
        if (creditsScrollView == null)
        {
            Debug.LogWarning("credits_scroll not found in credits UI");
            return;
        }

        creditsScrollView.UnregisterCallback<WheelEvent>(OnCreditsManualScrollInput);
        creditsScrollView.RegisterCallback<WheelEvent>(OnCreditsManualScrollInput);

    }

    private void OnCreditsManualScrollInput(WheelEvent evt)
    {
        PauseCreditsAutoScroll();
    }

    private void PauseCreditsAutoScroll()
    {
        creditsAutoScrollResumeTime = Time.unscaledTime + CreditsAutoScrollPauseAfterManualInput;
    }

    private void ResetCreditsScrollPosition()
    {
        if (creditsScrollView == null)
        {
            return;
        }

        suppressCreditsManualScrollPause = true;
        creditsScrollView.scrollOffset = Vector2.zero;
        suppressCreditsManualScrollPause = false;
        creditsAutoScrollResumeTime = Time.unscaledTime + CreditsAutoScrollStartDelay;
    }

    private void AdvanceCreditsAutoScroll()
    {
        if (creditsUiDocument == null || creditsScrollView == null)
        {
            return;
        }

        var creditsRoot = creditsUiDocument.rootVisualElement;
        if (creditsRoot == null || creditsRoot.resolvedStyle.display == DisplayStyle.None)
        {
            return;
        }

        if (Time.unscaledTime < creditsAutoScrollResumeTime)
        {
            return;
        }

        var verticalScroller = creditsScrollView.verticalScroller;
        if (verticalScroller == null || verticalScroller.highValue <= 0f)
        {
            return;
        }

        float nextOffsetY = creditsScrollView.scrollOffset.y + (CreditsAutoScrollSpeed * Time.unscaledDeltaTime);
        float maxOffsetY = verticalScroller.highValue;

        if (nextOffsetY >= maxOffsetY)
        {
            nextOffsetY = 0f;
            creditsAutoScrollResumeTime = Time.unscaledTime + CreditsAutoScrollStartDelay;
        }

        suppressCreditsManualScrollPause = true;
        creditsScrollView.scrollOffset = new Vector2(creditsScrollView.scrollOffset.x, nextOffsetY);
        suppressCreditsManualScrollPause = false;
    }

    private void HookSettingsCloseButton()
    {
        if (settingsUiDocument == null)
        {
            Debug.LogWarning("settingsUiDocument is null");
            return;
        }
        
        var settingsRoot = settingsUiDocument.rootVisualElement;
        
        if (settingsRoot == null)
        {
            Debug.LogWarning("settingsUiDocument rootVisualElement is null");
            return;
        }
        
        Button closeButton = settingsRoot.Q<Button>("close_button");
        
        if (closeButton != null)
        {
            closeButton.UnregisterCallback<ClickEvent>(OnSettingsCloseClickedEvent);
            closeButton.RegisterCallback<ClickEvent>(OnSettingsCloseClickedEvent);
        }
        else
        {
            Debug.LogWarning("close_button not found in settings UI");
        }
    }


    private void HookLargeTextToggle()
    {
        if (settingsUiDocument == null)
        {
            Debug.LogWarning("settingsUiDocument is null");
            return;
        }

        var settingsRoot = settingsUiDocument.rootVisualElement;

        if (settingsRoot == null)
        {
            Debug.LogWarning("settingsUiDocument rootVisualElement is null");
            return;
        }

        Toggle largeTextToggle = settingsRoot.Q<Toggle>("large_text_toggle");

        if (largeTextToggle != null)
        {
            largeTextToggle.SetValueWithoutNotify(AccessibilitySettingsManager.IsLargeTextEnabled());
            largeTextToggle.UnregisterValueChangedCallback(OnLargeTextToggleChanged);
            largeTextToggle.RegisterValueChangedCallback(OnLargeTextToggleChanged);
        }
        else
        {
            Debug.LogWarning("large_text_toggle not found in settings UI");
        }
    }

    private void OnLargeTextToggleChanged(ChangeEvent<bool> evt)
    {
        PlayerPrefs.SetInt(AccessibilitySettingsManager.PREF_LARGE_TEXT, evt.newValue ? 1 : 0);
        PlayerPrefs.Save();

        AccessibilitySettingsManager.ApplyLargeTextToDocument(mainMenuUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(settingsUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(howToPlayUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(creditsUiDocument);
    }

    public CharacterSprites GetCharacterSprites(HeroType heroType, int playerIndex = 0) 
    {
        if (Mode == GameMode.Singleplayer)
        {
            switch (heroType) 
            {
                case HeroType.Cyclist:
                    return remiBlueSprites;
                case HeroType.Scientist:
                    return regRedSprites;
                case HeroType.Ranger:
                    return tommyYellowSprites;
                default:
                    return remiBlueSprites;
            }
        }
        
        PlayerColor color = playerIndex == 0 ? PlayerColor.Blue : 
                        playerIndex == 1 ? PlayerColor.Red : 
                        PlayerColor.Yellow;
        
        switch (heroType)
        {
            case HeroType.Cyclist:
                return color == PlayerColor.Blue ? remiBlueSprites :
                    color == PlayerColor.Red ? remiRedSprites :
                    remiYellowSprites;
            case HeroType.Scientist:
                return color == PlayerColor.Blue ? regBlueSprites :
                    color == PlayerColor.Red ? regRedSprites :
                    regYellowSprites;
            case HeroType.Ranger:
                return color == PlayerColor.Blue ? tommyBlueSprites :
                    color == PlayerColor.Red ? tommyRedSprites :
                    tommyYellowSprites;
            default:
                return remiBlueSprites;
        }
    }
}

[System.Serializable]
public class CharacterSprites {
    public Sprite idleSprite;
    public Sprite[] walkSprites;
    public Sprite sickSprite;
}
