using System.Collections.Generic;
using UnityEngine;
using Spawning;

/// <summary>
/// Spawns minigame prefabs into specified rooms in a Voronoi dungeon.
/// A random prefab is placed in each room at a fixed height.
/// </summary>
public class MiniGameSpawnerVoronoi : ISpawnerVoronoi
{
    /// <summary>
    /// List of rooms where minigames should be spawned.
    /// </summary>
    private readonly List<Room> _rooms;
    
    /// <summary>
    /// List of available minigame prefabs to choose from.
    /// </summary>
    private readonly List<GameObject> _minigamePrefabs;
    
    /// <summary>
    /// Parent transform under which the minigames are spawned.
    /// </summary>
    private readonly Transform _parent;
    
    /// <summary>
    /// Deterministic random generator based on a fixed seed.
    /// </summary>
    private readonly System.Random _rng;

    /// <summary>
    /// Initializes the spawner with a list of rooms, minigame prefabs, a parent transform, and a required seed.
    /// </summary>
    /// <param name="rooms">Rooms to spawn minigames into.</param>
    /// <param name="prefabs">List of available minigame prefabs.</param>
    /// <param name="parent">Parent transform for spawned objects.</param>
    /// <param name="seed">Seed for deterministic random generation.</param>
    public MiniGameSpawnerVoronoi(List<Room> rooms, List<GameObject> prefabs, Transform parent, int seed = 42)
    {
        _rooms = rooms;
        _minigamePrefabs = prefabs;
        _parent = parent;
        _rng = new System.Random(seed); // TODO: adjust seed later!!!
    }

    /// <summary>
    /// Spawns one random minigame prefab in each minigame room.
    /// Placement is at room center with fixed height and random Y rotation.
    /// </summary>
    public void SpawnInRoom()
    {
        if (_minigamePrefabs.Count == 0) return;

        foreach (Room room in _rooms)
        {
            int prefabIndex = _rng.Next(_minigamePrefabs.Count);
            GameObject chosenPrefab = _minigamePrefabs[prefabIndex];

            Vector3 spawnPosition = new Vector3(room.center.x, 1.5f, room.center.y);
            float randomYRotation = (float)_rng.NextDouble() * 360f;
            Quaternion rotation = Quaternion.Euler(0f, randomYRotation, 0f);

            GameObject spawned = Object.Instantiate(chosenPrefab, spawnPosition, rotation, _parent);
            spawned.SetActive(true);
        }
    }
}