using System;

/// <summary>
/// Interface that all minigames must implement to integrate with the green square
/// minigame system (ABE.01).
///
/// When a player lands on a green square, a minigame implementing this interface
/// will be launched by MinigameManager. The minigame must signal completion or
/// exit via the provided events so the main game can resume.
///
/// Concrete implementations: PlaceholderMinigame (test), StayInsideCleanAirMinigame (MG.01), PlantTreesMinigame (MG.02), etc.
/// </summary>
public interface IMinigame
{
    /// <summary>
    /// Display name of this minigame (e.g. "Plant Trees", "Wind Fan", "Install Air Filters").
    /// Used for logging and UI display.
    /// </summary>
    string MinigameName { get; }

    /// <summary>
    /// Fired when the minigame is completed successfully.
    /// The int parameter is the pollution reduction amount based on player performance.
    /// A value of 0 means no reduction; higher values mean better performance.
    /// Must fire exactly once per StartMinigame() call (mutually exclusive with OnMinigameExited).
    /// </summary>
    event Action<int> OnMinigameComplete;

    /// <summary>
    /// Fired when the minigame is exited early by the player or fails to load.
    /// No pollution score change should occur when this event fires.
    /// Must fire exactly once per StartMinigame() call (mutually exclusive with OnMinigameComplete).
    /// </summary>
    event Action OnMinigameExited;

    /// <summary>
    /// Starts the minigame. The minigame is responsible for showing its own UI
    /// and managing its own gameplay loop. When finished, it must fire either
    /// OnMinigameComplete or OnMinigameExited exactly once.
    /// </summary>
    void StartMinigame();

    /// <summary>
    /// Returns true if the minigame is currently active/running.
    /// Used by MinigameManager to prevent launching overlapping minigames.
    /// </summary>
    bool IsActive { get; }
}
