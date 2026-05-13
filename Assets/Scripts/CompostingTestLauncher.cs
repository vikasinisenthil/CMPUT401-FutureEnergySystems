using UnityEngine;

public class CompostingTestLauncher : MonoBehaviour
{
    public CompostingMinigame compostingMinigame;

    private void Start()
    {
        if (compostingMinigame != null)
        {
            compostingMinigame.StartMinigame();
        }
        else
        {
            Debug.LogError("Composting minigame reference is missing.");
        }
    }
}