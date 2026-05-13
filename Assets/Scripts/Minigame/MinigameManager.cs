using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the selection and launching of minigames when a player lands on a green square (ABE.01).
///
/// Setup in Unity Editor:
/// 1. Attach this component to the same GameObject as GameController.
/// 2. In the Inspector, add GameObjects that have a component implementing IMinigame
///    to the "minigameObjects" list.
/// 3. GameController will call LaunchRandomMinigame() when the player lands on green.
///
/// The manager handles:
/// - Random selection from the available minigame pool
/// - Event subscription/cleanup to prevent memory leaks
/// - Graceful failure when no minigames are configured or a launch fails
/// </summary>
public class MinigameManager : MonoBehaviour
{
    /// <summary>
    /// List of GameObjects that have a component implementing IMinigame.
    /// Set these in the Unity Editor by dragging minigame GameObjects here.
    /// </summary>
    public List<GameObject> minigameObjects = new List<GameObject>();

    /// <summary>
    /// Fired when a minigame completes successfully with a pollution reduction amount.
    /// GameController subscribes to this to apply score changes.
    /// </summary>
    public event Action<int> OnMinigameResult;

    /// <summary>
    /// Fired when a minigame is exited or fails to load (no score change should occur).
    /// GameController subscribes to this to resume gameplay without score changes.
    /// </summary>
    public event Action OnMinigameNoResult;

    private IMinigame currentMinigame;

    /// <summary>
    /// Returns true if a minigame is currently running.
    /// Prevents launching a second minigame while one is active.
    /// </summary>
    public bool IsMinigameActive => currentMinigame != null && currentMinigame.IsActive;

    /// <summary>
    /// Launches a specific minigame assigned to a particular green square.
    /// Each green square on the board has its own minigame (e.g. "Plant Trees" on square 12).
    /// Returns true if the minigame was successfully launched, false otherwise.
    /// If the minigame is null or fails, OnMinigameNoResult is invoked.
    /// </summary>
    /// <param name="minigameObj">The specific minigame GameObject to launch. Must implement IMinigame.</param>
    public bool LaunchMinigame(GameObject minigameObj)
    {
        if (IsMinigameActive)
        {
            Debug.LogWarning("MinigameManager: A minigame is already running. Ignoring launch request.");
            return false;
        }

        if (minigameObj == null)
        {
            Debug.LogError("MinigameManager: Minigame GameObject is null. Resuming game.");
            OnMinigameNoResult?.Invoke();
            return false;
        }

        currentMinigame = minigameObj.GetComponent<IMinigame>();

        if (currentMinigame == null)
        {
            Debug.LogError($"MinigameManager: GameObject '{minigameObj.name}' does not implement IMinigame. Resuming game.");
            OnMinigameNoResult?.Invoke();
            return false;
        }

        return StartCurrentMinigame();
    }

    /// <summary>
    /// Launches a random minigame from the available pool (fallback if no specific minigame is assigned).
    /// Returns true if a minigame was successfully launched, false otherwise.
    /// If no minigames are available or the selected one fails, OnMinigameNoResult is invoked.
    /// </summary>
    public bool LaunchRandomMinigame()
    {
        if (IsMinigameActive)
        {
            Debug.LogWarning("MinigameManager: A minigame is already running. Ignoring launch request.");
            return false;
        }

        GameObject minigameObj = null;

        if (minigameObjects != null && minigameObjects.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, minigameObjects.Count);
            minigameObj = minigameObjects[index];
        }

        // Fallback: if list empty or selected ref is null, find any minigame in the scene (e.g. for tests or broken refs).
        if (minigameObj == null)
        {
            PlaceholderMinigame fallback = FindFirstObjectByType<PlaceholderMinigame>(FindObjectsInactive.Include);
            if (fallback == null)
                fallback = FindObjectOfType<PlaceholderMinigame>(true);
            if (fallback != null)
                minigameObj = fallback.gameObject;
        }

        if (minigameObj == null)
        {
            Debug.LogWarning("MinigameManager: No minigames configured or found in scene. Resuming game.");
            OnMinigameNoResult?.Invoke();
            return false;
        }

        currentMinigame = minigameObj.GetComponent<IMinigame>();

        if (currentMinigame == null)
        {
            Debug.LogError($"MinigameManager: GameObject '{minigameObj.name}' does not implement IMinigame. Resuming game.");
            OnMinigameNoResult?.Invoke();
            return false;
        }

        return StartCurrentMinigame();
    }

    /// <summary>
    /// Subscribes to the current minigame's events and starts it.
    /// Shared by both LaunchMinigame and LaunchRandomMinigame.
    /// </summary>
    private bool StartCurrentMinigame()
    {
        // Subscribe to the minigame's completion and exit events
        currentMinigame.OnMinigameComplete += HandleMinigameComplete;
        currentMinigame.OnMinigameExited += HandleMinigameExited;

        Debug.Log($"MinigameManager: Launching minigame '{currentMinigame.MinigameName}'");

        try
        {
            currentMinigame.StartMinigame();

            var minigameBehaviour = currentMinigame as MonoBehaviour;
            var minigameDocument = minigameBehaviour != null ? minigameBehaviour.GetComponent<UIDocument>() : null;

            if (minigameDocument == null && minigameBehaviour != null)
            {
                minigameDocument = minigameBehaviour.GetComponentInChildren<UIDocument>(true);
            }

            AccessibilitySettingsManager.ApplyLargeTextToDocument(minigameDocument);
        }
        catch (Exception e)
        {
            Debug.LogError($"MinigameManager: Failed to start minigame '{currentMinigame.MinigameName}': {e.Message}");
            CleanupCurrentMinigame();
            OnMinigameNoResult?.Invoke();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles successful minigame completion. Forwards the pollution reduction to GameController.
    /// </summary>
    private void HandleMinigameComplete(int pollutionReduction)
    {
        Debug.Log($"MinigameManager: Minigame '{currentMinigame?.MinigameName}' completed. Pollution reduction: {pollutionReduction}");
        CleanupCurrentMinigame();
        OnMinigameResult?.Invoke(pollutionReduction);
    }

    /// <summary>
    /// Handles minigame exit without completion. Signals GameController to resume with no score change.
    /// </summary>
    private void HandleMinigameExited()
    {
        Debug.Log($"MinigameManager: Minigame '{currentMinigame?.MinigameName}' exited without completion.");
        CleanupCurrentMinigame();
        OnMinigameNoResult?.Invoke();
    }

    /// <summary>
    /// Unsubscribes from the current minigame's events and clears the reference.
    /// Prevents memory leaks and duplicate event handling.
    /// </summary>
    private void CleanupCurrentMinigame()
    {
        if (currentMinigame != null)
        {
            currentMinigame.OnMinigameComplete -= HandleMinigameComplete;
            currentMinigame.OnMinigameExited -= HandleMinigameExited;
            currentMinigame = null;
        }
    }
}