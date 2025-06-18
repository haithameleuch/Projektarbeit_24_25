using System.Collections.Generic;
using UnityEngine;
using Spawning;

public class MiniGameSpawnerVoronoi : ISpawnerVoronoi
{
    private readonly List<Room> _rooms;
    private readonly List<GameObject> _minigamePrefabs;
    private readonly Transform _parent;

    public MiniGameSpawnerVoronoi(List<Room> rooms, List<GameObject> prefabs, Transform parent)
    {
        _rooms = rooms;
        _minigamePrefabs = prefabs;
        _parent = parent;
    }

    public void SpawnInRoom()
    {
        foreach (Room room in _rooms)
        {
            if (_minigamePrefabs.Count == 0) return;

            GameObject chosenPrefab = _minigamePrefabs[Random.Range(0, _minigamePrefabs.Count)];

            Vector3 spawnPosition = new Vector3(room.center.x, 1.5f, room.center.y);
            Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject spawned = Object.Instantiate(chosenPrefab, spawnPosition, rotation, _parent);
            spawned.SetActive(true);
        }
    }
}