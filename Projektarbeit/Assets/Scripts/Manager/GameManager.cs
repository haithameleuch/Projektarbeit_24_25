using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the overall game state, including tracking visited rooms
/// and handling room-specific logic when entering a room.
/// Implements a singleton pattern for global access.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the GameManager, ensuring there is only one instance in the scene.
    /// Provides global access to game management functionality.
    /// </summary>
    public static GameManager Instance { get; private set; }
    
    /// <summary>
    /// Tracks the coordinates of rooms that have been visited by the player.
    /// Ensures rooms are not reprocessed unnecessarily.
    /// </summary>
    private HashSet<Vector2Int> _visitedRooms = new();
    
    /// <summary>
    /// Ensures there is only one instance of the GameManager in the scene.
    /// If another instance exists, it will be destroyed.
    /// Persists the instance across scenes for consistent game state management.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Marks a room as visited by adding its coordinates to the visited rooms set.
    /// Prevents duplicate processing of the same room.
    /// </summary>
    /// <param name="roomCoordinate">The grid coordinate of the room to mark as visited.</param>
    public void MarkRoomVisited(Vector2Int roomCoordinate)
    {
        _visitedRooms.Add(roomCoordinate);
    }
    
    /// <summary>
    /// Checks if a room at the specified coordinates has already been visited.
    /// </summary>
    /// <param name="roomCoordinate">The grid coordinate of the room to check.</param>
    /// <returns>True if the room has been visited; otherwise, false.</returns>
    public bool IsRoomVisited(Vector2Int roomCoordinate)
    {
        return _visitedRooms.Contains(roomCoordinate);
    }

    /// <summary>
    /// Handles the logic for entering a room. Marks the room as visited, updates its state,
    /// and processes its type to trigger specific game logic.
    /// </summary>
    /// <param name="roomCoordinate">The grid coordinate of the room being entered.</param>
    public void HandleRoomEntry(Vector2Int roomCoordinate)
    {
        var roomBehaviour = DungeonGenerator.Instance.GetRoomBehaviour(roomCoordinate);
        if (roomBehaviour == null)
        {
            Debug.LogWarning($"Room {roomCoordinate} could not be found.");
            return;
        }
        
        if (roomBehaviour.GetVisited())
        {
            Debug.Log($"Room {roomCoordinate} has already been visited!");
            return;
        }

        roomBehaviour.MarkVisited();
        MarkRoomVisited(roomCoordinate);

        ProcessRoomType(roomBehaviour.RoomData?.roomType);
    }

    /// <summary>
    /// Processes the logic for a specific room type when the room is entered.
    /// Handles actions like closing doors, starting battles, or triggering events.
    /// </summary>
    /// <param name="roomType">The type of the room being entered.</param>
    private void ProcessRoomType(RoomType? roomType)
    {
        if (!roomType.HasValue)
        {
            Debug.LogWarning("Room type is null.");
            return;
        }

        switch (roomType.Value)
        {
            case RoomType.Start:
                Debug.Log("Enter Start Room");
                break;
            case RoomType.Normal:
                Debug.Log("Enter Normal Room -> doors should close");
                break;
            case RoomType.Item:
                Debug.Log("Enter Item Room -> doors should close");
                break;
            case RoomType.MiniGame:
                Debug.Log("Enter MiniGame Room -> doors should close");
                break;
            case RoomType.Enemy:
                Debug.Log("Enter Enemy Room -> doors should close");
                break;
            case RoomType.Boss:
                Debug.Log("Enter Boss Room -> doors should close");
                break;
            default:
                Debug.Log("Unknown room type");
                break;
        }
    }
}
