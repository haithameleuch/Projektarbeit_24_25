using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomData", menuName = "Scriptable Objects/Dungeon/RoomData")]
public class RoomData : ScriptableObject
{
    public string roomName;
    public RoomType roomType;
    public GameObject roomPrefab;

    [Tooltip("The minimum number of times this room should appear in the dungeon generation.")]
    public int minCount = 0;

    [Tooltip("The maximum number of times this room may appear in the dungeon generation (0 = no limit)")]
    public int maxCount = 0;
}