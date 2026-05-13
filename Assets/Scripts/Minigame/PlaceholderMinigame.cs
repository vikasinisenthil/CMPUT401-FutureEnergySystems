using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A simple placeholder minigame for testing the ABE.01 minigame framework.
/// Replaced on most green squares by dedicated minigames (e.g. MG.01 Stay Inside, MG.02 Plant Trees).
///
/// The placeholder gives the player two choices:
/// - "Complete" button: awards a configurable pollution reduction
/// - "Exit" button: exits with no score change
///
/// Setup in Unity Editor:
/// 1. Create a new GameObject named "PlaceholderMinigameObject"
/// 2. Add a UIDocument component and assign PlaceholderMinigame.uxml as the Source Asset
/// 3. Add this PlaceholderMinigame script component
/// 4. Drag the UIDocument component into the "uiDocument" field
/// 5. Add this GameObject to MinigameManager's minigameObjects list
/// </summary>
public class PlaceholderMinigame : MonoBehaviour, IMinigame
{
    // ----- IMinigame interface -----

    public string MinigameName => minigameName;

    public event Action<int> OnMinigameComplete;
    public event Action OnMinigameExited;

    public bool IsActive => isActive;

    // ----- Configuration -----

    /// <summary>
    /// Display name for this minigame (e.g. "Plant Trees!", "Ride a Bike").
    /// Set in the Unity Editor for each green square's placeholder.
    /// </summary>
    public string minigameName = "Placeholder Minigame";

    /// <summary>
    /// Description text shown in the minigame UI (e.g. "Plant trees to clean the air!").
    /// Set in the Unity Editor for each green square's placeholder.
    /// </summary>
    public string minigameDescription = "This is a placeholder minigame. Real minigames will replace this!";

    /// <summary>
    /// The UIDocument component for this minigame's UI. Set in the Unity Editor.
    /// </summary>
    public UIDocument uiDocument;

    /// <summary>
    /// The pollution reduction amount awarded when the player completes this placeholder minigame.
    /// Real minigames compute reduction from performance (see StayInsideCleanAirMinigame, PlantTreesMinigame).
    /// </summary>
    public int pollutionReductionAmount = 1;

    // ----- Internal state -----

    private bool isActive;
    private VisualElement root;

    void Start()
    {
        // Hide the minigame UI on startup
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    void LateUpdate()
    {
        // Ensure the minigame UI stays hidden until StartMinigame() is called.
        // Handles the case where rootVisualElement wasn't ready in Start().
        if (!isActive && uiDocument != null && uiDocument.rootVisualElement != null
            && uiDocument.rootVisualElement.style.display != DisplayStyle.None)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// Starts the placeholder minigame by showing its UI and registering button callbacks.
    /// ABE.01: The player cannot continue their turn until this minigame is completed or exited.
    /// </summary>
    public void StartMinigame()
    {
        if (uiDocument == null)
        {
            Debug.LogError("PlaceholderMinigame: UIDocument is not assigned. Cannot start minigame.");
            OnMinigameExited?.Invoke();
            return;
        }

        isActive = true;
        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("PlaceholderMinigame: rootVisualElement is null. Cannot display minigame UI.");
            isActive = false;
            OnMinigameExited?.Invoke();
            return;
        }

        // Show the minigame UI
        root.style.display = DisplayStyle.Flex;

        // Set the title and description for this specific green square
        Label titleLabel = root.Q<Label>("minigame_title");
        if (titleLabel != null)
        {
            titleLabel.text = minigameName;
        }

        Label descriptionLabel = root.Q<Label>("minigame_description");
        if (descriptionLabel != null)
        {
            descriptionLabel.text = minigameDescription;
        }

        // Clear the result label
        Label resultLabel = root.Q<Label>("result_label");
        if (resultLabel != null)
        {
            resultLabel.text = "";
        }

        // Register the "Complete" button
        Button completeButton = root.Q<Button>("complete_button");
        if (completeButton != null)
        {
            completeButton.text = $"Complete Minigame (-{pollutionReductionAmount} Pollution)";
            completeButton.SetEnabled(true);
            completeButton.RegisterCallback<ClickEvent>(HandleCompleteClicked);
        }

        // Register the "Exit" button
        Button exitButton = root.Q<Button>("exit_button");
        if (exitButton != null)
        {
            exitButton.SetEnabled(true);
            exitButton.RegisterCallback<ClickEvent>(HandleExitClicked);
        }

        Debug.Log("PlaceholderMinigame: Started. Complete or exit to continue the game.");
    }

    /// <summary>
    /// Handles the "Complete" button click.
    /// ABE.01: Completing the minigame returns a result that determines pollution reduction.
    /// </summary>
    private void HandleCompleteClicked(ClickEvent evt)
    {
        if (!isActive) return;

        Debug.Log($"PlaceholderMinigame: Completed with pollution reduction of {pollutionReductionAmount}");

        // Show result feedback
        Label resultLabel = root.Q<Label>("result_label");
        if (resultLabel != null)
        {
            resultLabel.text = $"Pollution reduced by {pollutionReductionAmount}!";
        }

        // Disable buttons to prevent double-clicks
        DisableButtons();

        // Play confirmation sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

        // Hide UI and fire completion event
        HideUI();
        isActive = false;
        OnMinigameComplete?.Invoke(pollutionReductionAmount);
    }

    /// <summary>
    /// Handles the "Exit" button click.
    /// ABE.01: If the minigame is exited by the user, the score is not changed and game resumes.
    /// </summary>
    private void HandleExitClicked(ClickEvent evt)
    {
        if (!isActive) return;

        Debug.Log("PlaceholderMinigame: Exited without completing. No score change.");

        // Disable buttons to prevent double-clicks
        DisableButtons();

        // Hide UI and fire exit event
        HideUI();
        isActive = false;
        OnMinigameExited?.Invoke();
    }

    /// <summary>
    /// Disables the complete and exit buttons and unregisters callbacks to prevent multiple clicks.
    /// </summary>
    private void DisableButtons()
    {
        Button completeButton = root.Q<Button>("complete_button");
        if (completeButton != null)
        {
            completeButton.UnregisterCallback<ClickEvent>(HandleCompleteClicked);
            completeButton.SetEnabled(false);
        }

        Button exitButton = root.Q<Button>("exit_button");
        if (exitButton != null)
        {
            exitButton.UnregisterCallback<ClickEvent>(HandleExitClicked);
            exitButton.SetEnabled(false);
        }
    }

    /// <summary>
    /// Hides the minigame UI overlay.
    /// </summary>
    private void HideUI()
    {
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
        }
    }
}
