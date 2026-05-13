using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class CharacterSelectionController : MonoBehaviour
{
    public static CharacterSelectionController Instance;

    public UIDocument characterSelectionUiDocument;
    public UIDocument characterInfoUiDocument;
    public UIDocument difficultySelectUiDocument;

    private VisualElement root;
    private VisualElement infoRoot;
    private VisualElement difficultyRoot;

    private Label prompt;
    private Label footerPrompt;
    private VisualElement infoPopup;
    private Label infoName;
    private Label infoSubtitle;
    private VisualElement infoImage;
    private Label infoDescription;
    private Label infoAbilities;
    private Label infoBest;
    private Button infoCloseButton;

    private VisualElement difficultyPopup;

    private HeroType selectedHero;

    public Texture2D remiBlue;
    public Texture2D remiRed;
    public Texture2D remiYellow;
    public Texture2D regBlue;
    public Texture2D regRed;
    public Texture2D regYellow;
    public Texture2D tommyBlue;
    public Texture2D tommyRed;
    public Texture2D tommyYellow;

    void Start()
    {
        root = characterSelectionUiDocument.rootVisualElement;
        infoRoot = characterInfoUiDocument.rootVisualElement;
        difficultyRoot = difficultySelectUiDocument.rootVisualElement;

        AccessibilitySettingsManager.ApplyLargeTextToDocument(characterSelectionUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(characterInfoUiDocument);
        AccessibilitySettingsManager.ApplyLargeTextToDocument(difficultySelectUiDocument);

        prompt = root.Q<Label>("screen_title");
        footerPrompt = root.Q<Label>("screen_footer");
        UpdatePromptAndColors();

        if (footerPrompt != null)
        {
            footerPrompt.text = "Tap a hero to see their powers!";

            bool pulseOn = false;
            footerPrompt.schedule.Execute(() =>
            {
                pulseOn = !pulseOn;
                footerPrompt.EnableInClassList("screen-footer-pulse", pulseOn);
            }).Every(700);
        }

        VisualElement remiCard = root.Q<VisualElement>("remi_card");
        VisualElement regCard = root.Q<VisualElement>("reg_card");
        VisualElement tommyCard = root.Q<VisualElement>("tommy_card");
        VisualElement backButton = root.Q<VisualElement>("back_button");
        VisualElement remiSelectButton = root.Q<VisualElement>("select_remi");
        VisualElement regSelectButton = root.Q<VisualElement>("select_reg");
        VisualElement tommySelectButton = root.Q<VisualElement>("select_tommy");

        remiCard.RegisterCallback<ClickEvent>(evt => OnRemiClicked());
        regCard.RegisterCallback<ClickEvent>(evt => OnRegClicked());
        tommyCard.RegisterCallback<ClickEvent>(evt => OnTommyClicked());
        backButton.RegisterCallback<ClickEvent>(evt => OnBackClicked());
        remiSelectButton.RegisterCallback<ClickEvent>(evt => 
        {
            evt.StopPropagation();
            OnRemiSelected();
        });
        regSelectButton.RegisterCallback<ClickEvent>(evt => 
        {
            evt.StopPropagation();
            OnRegSelected();
        });
        tommySelectButton.RegisterCallback<ClickEvent>(evt => 
        {
            evt.StopPropagation();
            OnTommySelected();
        });

        infoPopup = infoRoot.Q<VisualElement>("info_popup");
        infoName = infoRoot.Q<Label>("info_name");
        infoSubtitle = infoRoot.Q<Label>("info-subtitle");
        infoImage = infoRoot.Q<VisualElement>("info_image");
        infoDescription = infoRoot.Q<Label>("info_description");
        infoAbilities = infoRoot.Q<Label>("info_abilities");
        infoBest = infoRoot.Q<Label>("info_best_for");
        infoCloseButton = infoRoot.Q<Button>("close_info");

        if (infoPopup != null) infoPopup.style.display = DisplayStyle.None;
        if (infoCloseButton != null) 
        {
            infoCloseButton.clicked += () => 
            {
                infoPopup.style.display = DisplayStyle.None;
                AudioManager.Instance.PlayConfirm();
            };
        }

        difficultyPopup = difficultyRoot.Q<VisualElement>("difficulty_popup");
        
        Button easyButton = difficultyRoot.Q<Button>("easy_button");
        Button mediumButton = difficultyRoot.Q<Button>("medium_button");
        Button hardButton = difficultyRoot.Q<Button>("hard_button");
        Button difBackButton = difficultyRoot.Q<Button>("back_button");

        easyButton.clicked += () => OnDifficultySelected(Difficulty.Easy);
        mediumButton.clicked += () => OnDifficultySelected(Difficulty.Medium);
        hardButton.clicked += () => OnDifficultySelected(Difficulty.Hard);
        difBackButton.clicked += () => 
        {
            difficultyPopup.style.display = DisplayStyle.None;
            AudioManager.Instance.PlayConfirm();
        };
    }

    private void UpdatePromptAndColors()
    {
        if (GameManager.Instance == null) return;
        
        root.RemoveFromClassList("picking-player-1");
        root.RemoveFromClassList("picking-player-2");
        root.RemoveFromClassList("picking-player-3");
        root.RemoveFromClassList("p1-title");
        root.RemoveFromClassList("p2-title");
        root.RemoveFromClassList("p3-title");

        root.RemoveFromClassList("remi-blue");
        root.RemoveFromClassList("remi-red");
        root.RemoveFromClassList("remi-yellow");
        root.RemoveFromClassList("reg-blue");
        root.RemoveFromClassList("reg-red");
        root.RemoveFromClassList("reg-yellow");
        root.RemoveFromClassList("tommy-blue");
        root.RemoveFromClassList("tommy-red");
        root.RemoveFromClassList("tommy-yellow");
        
        // Update prompt text
        if (GameManager.Instance.Mode == GameMode.Multiplayer)
        {
            int playerNum = GameManager.Instance.CurrentPickIndex + 1;
            prompt.text = $"Player {playerNum}, choose your hero!";
            
            // Add appropriate player color class
            root.AddToClassList($"picking-player-{playerNum}");
            root.AddToClassList($"p{playerNum}-title");
            
            if (playerNum == 1) {
                root.AddToClassList("remi-blue");
                root.AddToClassList("reg-blue");
                root.AddToClassList("tommy-blue");
            } else if (playerNum == 2) {
                root.AddToClassList("remi-red");
                root.AddToClassList("reg-red");
                root.AddToClassList("tommy-red");
            } else if (playerNum == 3) {
                root.AddToClassList("remi-yellow");
                root.AddToClassList("reg-yellow");
                root.AddToClassList("tommy-yellow");
            }
        }
        else
        {
            prompt.text = "Choose your hero!";
            root.AddToClassList("remi-blue");
            root.AddToClassList("reg-red");
            root.AddToClassList("tommy-yellow");
        }
    }

    public void OnBackClicked()
    {
        SceneManager.LoadScene("MainMenu");
        Debug.Log("Back Button Clicked");
        AudioManager.Instance.PlayConfirm();
    }
    
    public void OnRemiClicked()
    {
        Texture2D image = GetCharacterImage(HeroType.Cyclist);
        ShowCharacterInfo("Remi the Cyclist", 
            "\"Bike Power! Zero tailpipe smoke.\"",
            image,
            "Remi rides fast and clean, using fresh airflow to push through pollution and traffic.",
            "Speed Boost: When Remi lands on a \"Ride Bike!\" path or a Green Spot, they automatically move +1 extra space.\nTraffic Weaver: When Remi lands on a Car/Traffic Grey Spot, they take zero pollution damage.",
            "Players who want faster movement and protection from traffic pollution.");
    }

    public void OnRegClicked()
    {
        Texture2D image = GetCharacterImage(HeroType.Scientist);
        ShowCharacterInfo("Reg the Scientist",
            "\"I can see the invisible!\"",
            image,
            "Reginald uses sensors to understand air quality and spot the best choices when things get tricky.",
            "Forecasting: Twice per game, Reginald can use a Sensor to reveal the correct answer on a difficult Quiz Card.",
            "Players who like strategy and getting quiz questions right.");
    }

    public void OnTommyClicked()
    {
        Texture2D image = GetCharacterImage(HeroType.Ranger);
        ShowCharacterInfo("Tommy the Ranger",
            "\"Trees are the lungs of the Earth.\"",
            image,
            "Tommy protects nature and knows how to breathe through smoke—plus they help clean the air even faster.",
            "Fire Resistance: If Tommy lands on a Wildfire spot, they take no pollution damage.\nPhotosynthesis Boost: When Tommy lands on a Plant Trees (Green) Spot, they remove double pollution.",
            "Players who want strong defense against wildfires and extra pollution healing.");
    }

    private Texture2D GetCharacterImage(HeroType heroType)
    {
        // In singleplayer, use fixed colors
        if (GameManager.Instance.Mode == GameMode.Singleplayer)
        {
            switch (heroType)
            {
                case HeroType.Cyclist:
                    return remiBlue;
                case HeroType.Scientist:
                    return regRed;
                case HeroType.Ranger:
                    return tommyYellow;
                default:
                    return remiBlue;
            }
        }
        
        // In multiplayer, use current player's color
        int currentPlayer = GameManager.Instance.CurrentPickIndex;
        
        switch (heroType)
        {
            case HeroType.Cyclist:
                return currentPlayer == 0 ? remiBlue :
                    currentPlayer == 1 ? remiRed :
                    remiYellow;
            case HeroType.Scientist:
                return currentPlayer == 0 ? regBlue :
                    currentPlayer == 1 ? regRed :
                    regYellow;
            case HeroType.Ranger:
                return currentPlayer == 0 ? tommyBlue :
                    currentPlayer == 1 ? tommyRed :
                    tommyYellow;
            default:
                return remiBlue;
        }
    }

    public void OnRemiSelected()
    {
        selectedHero = HeroType.Cyclist;
        OnSelectCharacter();
    }

    public void OnRegSelected()
    {
        selectedHero = HeroType.Scientist;
        OnSelectCharacter();
    }

    public void OnTommySelected()
    {
        selectedHero = HeroType.Ranger;
        OnSelectCharacter();
    }

    private void ShowCharacterInfo(string name, string subtitle, Texture2D image, string description, string abilities, string bestFor)
    {
        infoName.text = name;
        infoSubtitle.text = subtitle;
        infoImage.style.backgroundImage = image;
        infoDescription.text = description;
        infoAbilities.text = abilities;
        infoBest.text = bestFor;
        
        infoPopup.style.display = DisplayStyle.Flex;
        Debug.Log($"{name} Clicked");
        AudioManager.Instance.PlayConfirm();
    }

    private void OnSelectCharacter()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.SelectedHeroes == null)
        {
            Debug.LogError("GameManager not found or SelectedHeroes is null!");
            return;
        }

        // Save selection
        int idx = gm.CurrentPickIndex;
        if (idx < gm.PlayerCount)
        {
            gm.SelectedHeroes[idx] = selectedHero;
            gm.CurrentPickIndex++;
            
            Debug.Log($"Character {idx + 1} selected: {selectedHero}");
            AudioManager.Instance.PlayConfirm();
        }

        // Check if all players have selected
        if (gm.CurrentPickIndex >= gm.PlayerCount)
        {
            // All selections done - show difficulty selection
            Debug.Log("All characters selected. Showing difficulty selection.");
            difficultyPopup.style.display = DisplayStyle.Flex;
        }
        else
        {
            // More players need to select - show message or loop back
            UpdatePromptAndColors();
            AudioManager.Instance.PlayConfirm();
            // You could show a message here or just wait for next selection
        }
    }

    private void OnDifficultySelected(Difficulty difficulty)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.difficulty = difficulty;
            Debug.Log($"Difficulty selected: {difficulty}");
            AudioManager.Instance.PlayConfirm();
            
            // Load the board scene
            if (GameManager.Instance.Mode == GameMode.Singleplayer) {
                Debug.Log("Loading Singleplayer...");
                //SceneManager.LoadScene("SingleBoard");
            } else if (GameManager.Instance.Mode == GameMode.Multiplayer) {
                Debug.Log("Loading Multiplayer...");
                //SceneManager.LoadScene("MultiBoard");
            }
            SceneManager.LoadScene("BoardScene");
        }
    }
}