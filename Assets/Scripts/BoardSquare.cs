using UnityEngine;

public enum BoardSquareColor
{
    BLUE,
    GREEN,
    GRAY
}

/// <summary>
/// ABE.11 / Ranger: Type of grey spot.
/// Car/Traffic spots grant the Cyclist zero pollution damage.
/// Wildfire spots grant the Ranger zero pollution damage.
/// Only used when color is GRAY.
/// </summary>
public enum GreySpotType
{
    Generic,
    CarTraffic,
    Wildfire
}

public class BoardSquare : MonoBehaviour
{
    public BoardSquareColor color;
    public GameObject gameController;

    /// <summary>
    /// Used when color is GRAY.
    /// Car/Traffic spots can protect Cyclist.
    /// Wildfire spots can protect Ranger.
    /// Can be set in the Editor or by GameController at Start.
    /// </summary>
    public GreySpotType greySpotType = GreySpotType.Generic;

    /// <summary>
    /// Custom text shown on grey cards. Examples:
    /// "Oh no! There's train smoke!"
    /// "A wildfire is burning nearby!"
    /// "Traffic exhaust fills the air!"
    /// Leave empty to use default text: "You are near a pollution source"
    /// Only used when color is GRAY.
    /// </summary>
    [TextArea(2, 4)]
    public string grayCardText = "";

    /// <summary>
    /// The specific minigame GameObject assigned to this green square.
    /// Set in the Unity Editor for each green square (e.g. PlantTrees for square 12,
    /// RideBike for square 28). Must have a component implementing IMinigame.
    /// Only used when color is GREEN; ignored for BLUE and GRAY squares.
    /// </summary>
    public GameObject minigameObject;

    public void LandOn()
    {
        switch (color)
        {
            case BoardSquareColor.BLUE:
                gameController.GetComponent<GameController>().LandedOnBlueSquare();
                break;

            case BoardSquareColor.GREEN:
                // Trigger the specific minigame assigned to this green square
                gameController.GetComponent<GameController>().LandedOnGreenSquare(minigameObject);
                break;

            case BoardSquareColor.GRAY:
                gameController.GetComponent<GameController>().LandedOnGraySquare(greySpotType, grayCardText);
                break;
        }
    }
}