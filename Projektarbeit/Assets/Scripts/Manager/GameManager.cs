using System.Collections.Generic;
using UnityEngine;

//TODO KOMMENTAR
public class GameManager : MonoBehaviour
{
    //TODO KOMMENTAR
    public static GameManager Instance { get; private set; }
    
    //TODO KOMMENTAR
    private HashSet<Vector2Int> _visitedRooms = new();
    
    //TODO: KOMMENTAR
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

    //TODO: KOMMENTAR
    public void MarkRoomVisited(Vector2Int roomCoordinate)
    {
        _visitedRooms.Add(roomCoordinate);
    }
    
    //TODO KOMMENTAR
    public bool IsRoomVisited(Vector2Int roomCoordinate)
    {
        return _visitedRooms.Contains(roomCoordinate);
    }

    //TODO KOMMENTAR
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

    //TODO KOMMENTAR
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
