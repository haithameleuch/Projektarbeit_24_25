// LEGACY CODE !!!

using Dungeon;
using UnityEngine;

/// <summary>
/// Represents the data for a specific room in a dungeon, including its type, prefab, and spawn constraints.
/// </summary>
[CreateAssetMenu(fileName = "NewRoomData", menuName = "Scriptable Objects/Dungeon/RoomData")]
public class RoomData : ScriptableObject
{
    /// <summary>
    /// The name of the room, used for identification or debugging purposes.
    /// </summary>
    public string roomName;
    
    /// <summary>
    /// The type of the room, determining its purpose or content.
    /// </summary>
    public RoomType roomType;
    
    /// <summary>
    /// The prefab associated with this room, representing its visual and functional design.
    /// </summary>
    public GameObject roomPrefab;

    /// <summary>
    /// The minimum number of times this room should appear during dungeon generation.
    /// </summary>
    [Tooltip("The minimum number of times this room should appear in the dungeon generation.")]
    public int minCount = 0;

    /// <summary>
    /// The maximum number of times this room may appear during dungeon generation. A value of 0 means there is no limit.
    /// </summary>
    [Tooltip("The maximum number of times this room may appear in the dungeon generation (0 = no limit)")]
    public int maxCount = 0;
}