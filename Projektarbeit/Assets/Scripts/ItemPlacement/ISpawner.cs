using UnityEngine;

public interface ISpawner
{
    void SpawnInRoom(RoomBehaviour room, bool isStartRoom);
}