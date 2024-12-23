/// <summary>
/// Interface for object spawners in a room. Allows flexibility for spawning different types of objects like items or enemies.
/// </summary>
public interface ISpawner
{
    /// <summary>
    /// Spawns objects in a specified room.
    /// </summary>
    /// <param name="room">The room in which objects are spawned.</param>
    /// <param name="isStartRoom">Indicates whether the room is the starting room (no objects should be spawned).</param>
    void SpawnInRoom(RoomBehaviour room, bool isStartRoom);
}