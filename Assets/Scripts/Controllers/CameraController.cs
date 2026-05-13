using UnityEngine;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -1);
    
    [Header("Overview Settings")]
    [SerializeField] private float overviewPadding = 2f;
    [SerializeField] private float minOrthographicSize = 5f;
    [SerializeField] private float maxOrthographicSize = 15f;
    
    private Camera cam;
    private GameController gc;
    private Vector3 targetPosition;
    private float targetOrthographicSize;
    private bool isFollowingPlayer = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController: No Camera component found!");
            return;
        }

        gc = FindObjectOfType<GameController>();
        if (gc == null)
        {
            Debug.LogError("CameraController: GameController not found!");
            return;
        }

        // Start with overview
        UpdateOverviewPosition();
    }

    void LateUpdate()
    {
        if (cam == null || gc == null) return;

        // Determine if we should follow a player
        bool shouldFollow = false;
        Player activePlayer = null;

        if (gc.player != null && gc.player.moving)
        {
            shouldFollow = true;
            activePlayer = gc.player;
        }

        if (shouldFollow && activePlayer != null && activePlayer.gameObject != null)
        {
            FollowPlayer(activePlayer);
            isFollowingPlayer = true;
        }
        else
        {
            if (isFollowingPlayer)
            {
                // Just stopped following, recalculate overview
                UpdateOverviewPosition();
                isFollowingPlayer = false;
            }
        }

        // Smoothly move camera
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        
        // Smoothly zoom camera
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthographicSize, Time.deltaTime * zoomSpeed);
        }
    }

    private void FollowPlayer(Player player)
    {
        // Set target position to follow the player
        targetPosition = player.gameObject.transform.position + offset;
        
        // Zoom in when following
        targetOrthographicSize = minOrthographicSize;
    }

    public void UpdateOverviewPosition()
    {
        if (gc == null) return;

        List<Vector3> playerPositions = new List<Vector3>();

        // Collect all player positions
        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer)
        {
            foreach (var player in gc.players)
            {
                if (player != null && player.gameObject != null)
                {
                    playerPositions.Add(player.gameObject.transform.position);
                }
            }
        }
        else
        {
            // Singleplayer
            if (gc.player != null && gc.player.gameObject != null)
            {
                playerPositions.Add(gc.player.gameObject.transform.position);
            }
        }

        if (playerPositions.Count == 0)
        {
            // No players found, use default position
            targetPosition = Vector3.zero + offset;
            targetOrthographicSize = minOrthographicSize;
            return;
        }

        // Calculate bounds of all players
        Bounds bounds = new Bounds(playerPositions[0], Vector3.zero);
        foreach (var pos in playerPositions)
        {
            bounds.Encapsulate(pos);
        }

        // Center the camera based on the board center + the angled offset
        targetPosition = bounds.center + offset;

        // Adjust Zoom logic for -45 degree camera angle
        // Board is in X-Y plane, camera looks down at -45 degrees
        float angle = 45f * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(angle); // ~0.707
        
        // At -45 degrees rotation:
        // - X spread stays the same (horizontal on screen)
        // - Y spread gets compressed by cos(45) on screen (depth into the board)
        float horizontalSpread = bounds.size.x;
        float depthSpread = bounds.size.y * cosAngle;

        // Use the larger dimension to set the orthographic size
        float requiredSize = Mathf.Max(horizontalSpread, depthSpread) / 2f + overviewPadding;
        
        targetOrthographicSize = Mathf.Clamp(requiredSize, minOrthographicSize, maxOrthographicSize);
    }
}