using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

#if UNITY_EDITOR
/// <summary>
/// Editor-only utility script to automatically set up minigame objects
/// for each green square on the board. Run from the menu: Tools > Setup Green Square Minigames.
///
/// Square 12 gets the real PlantTreesMinigame (MG.01).
/// Square 17 gets the real PublicTransportationMinigame (MG.03).
/// All other green squares get PlaceholderMinigame until their real minigames are built.
/// Square 6 gets StayInsideCleanAirMinigame (MG.01).
/// Square 12 gets PlantTreesMinigame (MG.02).
/// Other green squares use PlaceholderMinigame until implemented.
/// </summary>
public class SetupGreenSquareMinigames
{
    /// <summary>
    /// Green square configuration: square name, minigame display name, description, and whether it uses a real minigame.
    /// isRealMinigame = true means it uses a dedicated minigame script instead of the placeholder.
    /// </summary>
    private static readonly List<(string squareName, string minigameName, string description, bool isRealMinigame)> GreenSquareConfigs =
        new List<(string squareName, string minigameName, string description, bool isRealMinigame)>()
    {
        ("BoardSquare6",  "Stay Inside & Clean the Air!", "Swipe or tap to blow smoky air away — pretend you are the wind!",           true),
        ("BoardSquare12", "Plant Trees!",                 "Plant trees to absorb CO2 and clean the air!",                             true),
        ("BoardSquare17", "Public Transportation",        "Choose the cleanest way to travel and improve air quality.",                true),
        ("BoardSquare20", "Using Clean Energy",           "Switch to solar and wind power to reduce pollution! (Coming soon)",         false),
        ("BoardSquare24", "Use Masks",                    "Wear a mask to protect yourself from polluted air! (Coming soon)",          false),
        ("BoardSquare28", "Ride a Bike",                  "Ride a bike instead of driving to reduce emissions! (Coming soon)",         false),
        ("BoardSquare36", "Save Energy",                  "Turn off lights and appliances to save energy! (Coming soon)",              false)
    };

    [MenuItem("Tools/Setup Green Square Minigames")]
    public static void Setup()
    {
        // Find the PanelSettings asset
        string[] panelSettingsGuids = AssetDatabase.FindAssets("PanelSettings t:PanelSettings");
        PanelSettings panelSettings = null;
        if (panelSettingsGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(panelSettingsGuids[0]);
            panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        }

        // Find the PlaceholderMinigame.uxml asset
        string[] placeholderUxmlGuids = AssetDatabase.FindAssets("PlaceholderMinigame t:VisualTreeAsset");
        VisualTreeAsset placeholderUxml = null;
        if (placeholderUxmlGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(placeholderUxmlGuids[0]);
            placeholderUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }

        // Find the StayInsideCleanAirMinigame.uxml asset
        string[] stayInsideUxmlGuids = AssetDatabase.FindAssets("StayInsideCleanAirMinigame t:VisualTreeAsset");
        VisualTreeAsset stayInsideUxml = null;
        if (stayInsideUxmlGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(stayInsideUxmlGuids[0]);
            stayInsideUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }

        // Find the PlantTreesMinigame.uxml asset
        string[] plantTreesUxmlGuids = AssetDatabase.FindAssets("PlantTreesMinigame t:VisualTreeAsset");
        VisualTreeAsset plantTreesUxml = null;
        if (plantTreesUxmlGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(plantTreesUxmlGuids[0]);
            plantTreesUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }

        // Find the PublicTransportationMinigame.uxml asset
        string[] publicTransportUxmlGuids = AssetDatabase.FindAssets("PublicTransportationMinigame t:VisualTreeAsset");
        VisualTreeAsset publicTransportUxml = null;
        if (publicTransportUxmlGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(publicTransportUxmlGuids[0]);
            publicTransportUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }

        if (placeholderUxml == null)
        {
            Debug.LogError("SetupGreenSquareMinigames: Could not find PlaceholderMinigame.uxml!");
            return;
        }

        // Remove any existing minigame objects to avoid duplicates
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (GameObject obj in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.name.StartsWith("Minigame_")
                || obj.name == "PlaceholderMinigameObject"
                || obj.name == "PlantTreesMinigameObject"
                || obj.name == "PublicTransportationMinigameObject"
                || obj.name == "StayInsideCleanAirMinigameObject")
            {
                toDestroy.Add(obj);
            }
        }
        foreach (GameObject obj in toDestroy)
        {
            Undo.DestroyObjectImmediate(obj);
        }

        // Create a minigame object for each green square
        foreach (var config in GreenSquareConfigs)
        {
            // Find the BoardSquare in the scene
            GameObject boardObj = FindBoardSquare(config.squareName);
            if (boardObj == null)
            {
                Debug.LogWarning($"SetupGreenSquareMinigames: Could not find {config.squareName} in scene. Skipping.");
                continue;
            }

            BoardSquare boardSquare = boardObj.GetComponent<BoardSquare>();
            if (boardSquare == null)
            {
                Debug.LogWarning($"SetupGreenSquareMinigames: {config.squareName} has no BoardSquare component. Skipping.");
                continue;
            }

            // Create a new GameObject for this minigame
            string objName = $"Minigame_{config.minigameName.Replace(" ", "").Replace("!", "").Replace("&", "And")}";
            GameObject minigameObj = new GameObject(objName);
            Undo.RegisterCreatedObjectUndo(minigameObj, $"Create {objName}");

            // Add UIDocument component
            UIDocument uiDoc = minigameObj.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;

            if (config.isRealMinigame && config.squareName == "BoardSquare6")
            {
                if (stayInsideUxml == null)
                {
                    Debug.LogError("SetupGreenSquareMinigames: Could not find StayInsideCleanAirMinigame.uxml! Falling back to placeholder.");
                    uiDoc.visualTreeAsset = placeholderUxml;
                    AddPlaceholderComponent(minigameObj, uiDoc, config);
                }
                else
                {
                    uiDoc.visualTreeAsset = stayInsideUxml;
                    StayInsideCleanAirMinigame stayInside = minigameObj.AddComponent<StayInsideCleanAirMinigame>();
                    stayInside.uiDocument = uiDoc;
                    Debug.Log($"SetupGreenSquareMinigames: Created StayInsideCleanAirMinigame (MG.01) on {config.squareName}");
                }
            }
            else if (config.isRealMinigame && config.squareName == "BoardSquare12")
            {
                // MG.02: Plant Trees
                if (plantTreesUxml == null)
                {
                    Debug.LogError("SetupGreenSquareMinigames: Could not find PlantTreesMinigame.uxml! Falling back to placeholder.");
                    uiDoc.visualTreeAsset = placeholderUxml;
                    AddPlaceholderComponent(minigameObj, uiDoc, config);
                }
                else
                {
                    uiDoc.visualTreeAsset = plantTreesUxml;
                    PlantTreesMinigame plantTrees = minigameObj.AddComponent<PlantTreesMinigame>();
                    plantTrees.uiDocument = uiDoc;
                    Debug.Log($"SetupGreenSquareMinigames: Created PlantTreesMinigame (MG.02) on {config.squareName}");
                }
            }
            else if (config.isRealMinigame && config.squareName == "BoardSquare17")
            {
                if (publicTransportUxml == null)
                {
                    Debug.LogError("SetupGreenSquareMinigames: Could not find PublicTransportationMinigame.uxml! Falling back to placeholder.");
                    uiDoc.visualTreeAsset = placeholderUxml;
                    AddPlaceholderComponent(minigameObj, uiDoc, config);
                }
                else
                {
                    uiDoc.visualTreeAsset = publicTransportUxml;
                    PublicTransportationMinigame publicTransportation = minigameObj.AddComponent<PublicTransportationMinigame>();
                    publicTransportation.uiDocument = uiDoc;
                    Debug.Log($"SetupGreenSquareMinigames: Created REAL PublicTransportationMinigame and assigned to {config.squareName}");
                }
            }
            else
            {
                // Placeholder for other green squares
                uiDoc.visualTreeAsset = placeholderUxml;
                AddPlaceholderComponent(minigameObj, uiDoc, config);
            }

            // Assign this minigame to the BoardSquare
            boardSquare.minigameObject = minigameObj;
            EditorUtility.SetDirty(boardSquare);

            Debug.Log($"SetupGreenSquareMinigames: Created {objName} and assigned to {config.squareName}");
        }

        Debug.Log("SetupGreenSquareMinigames: Done! Green squares: 6 = Stay Inside (MG.01), 12 = Plant Trees (MG.02). Save the scene.");
    }

    /// <summary>
    /// Adds a PlaceholderMinigame component with the given configuration.
    /// </summary>
    private static void AddPlaceholderComponent(GameObject obj, UIDocument uiDoc,
        (string squareName, string minigameName, string description, bool isRealMinigame) config)
    {
        PlaceholderMinigame placeholder = obj.AddComponent<PlaceholderMinigame>();
        placeholder.minigameName = config.minigameName;
        placeholder.minigameDescription = config.description;
        placeholder.uiDocument = uiDoc;
        placeholder.pollutionReductionAmount = 1;
    }

    /// <summary>
    /// Finds a BoardSquare GameObject by searching all objects in the scene.
    /// Handles the fact that board squares might be nested under a parent "Board" object.
    /// </summary>
    private static GameObject FindBoardSquare(string squareName)
    {
        // Search all BoardSquare components in the scene
        BoardSquare[] allSquares = GameObject.FindObjectsByType<BoardSquare>(FindObjectsSortMode.None);
        foreach (BoardSquare bs in allSquares)
        {
            if (bs.gameObject.name == squareName)
            {
                return bs.gameObject;
            }
        }

        // Fallback: try GameObject.Find
        return GameObject.Find(squareName);
    }
}

#endif
