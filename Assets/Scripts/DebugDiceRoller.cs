using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class DebugDiceRoller : MonoBehaviour
{
    public GameController gameController;
    public UIDocument inGameUiDocument;

    void Update()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            RollSpecificNumber(1);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            RollSpecificNumber(2);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            RollSpecificNumber(3);
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            RollSpecificNumber(4);
        }
        else if (keyboard.digit5Key.wasPressedThisFrame)
        {
            RollSpecificNumber(5);
        }
        else if (keyboard.digit6Key.wasPressedThisFrame)
        {
            RollSpecificNumber(6);
        }
        else if (keyboard.digit0Key.wasPressedThisFrame)
        {
            RollSpecificNumber(40);
        }
        
        #endif
    }

    private void RollSpecificNumber(int number)
    {
        if (gameController == null || inGameUiDocument == null)
        {
            Debug.LogWarning("DebugDiceRoller: GameController or UIDocument not assigned!");
            return;
        }

        if (gameController.player.moving)
        {
            Debug.Log("Player already moving!");
            return;
        }

        Debug.Log($"Debug Roll: {number}");

        int startSquare = gameController.player.boardSquareIndex;
        int endSquare = Mathf.Clamp(startSquare + number, 0, gameController.boardSquares.Count - 1);

        if (!gameController.MovePlayer(number, triggerLandOn: true, showCountdown: true))
        {
            return;
        }

        FinalScoreLogger.Instance?.LogRoll(number, startSquare, endSquare);
        AudioManager.Instance?.PlayDiceRoll();

        var rollDiceButton = inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
        if (rollDiceButton != null)
        {
            rollDiceButton.text = number.ToString();
            rollDiceButton.style.fontSize = 48;
        }
    }
}