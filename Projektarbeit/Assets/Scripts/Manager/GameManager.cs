using System.Collections.Generic;
using UnityEngine;

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
}
