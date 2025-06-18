using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerVoronoi
{
    private readonly Transform _parent;
    private readonly List<GameObject> _enemyPrefabs;
    
    private readonly Dictionary<int, GameObject> _enemyInstances = new Dictionary<int, GameObject>();

    public EnemySpawnerVoronoi(List<Room> enemyRooms, List<GameObject> enemyPrefabs, Transform parent)
    {
        _enemyPrefabs = enemyPrefabs;
        _parent = parent;

        foreach (var room in enemyRooms)
        {
            GameObject chosenPrefab = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Count)];
            Vector3 spawnPosition = new Vector3(room.center.x, 0.0f, room.center.y);
            Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject enemy = Object.Instantiate(chosenPrefab, spawnPosition, rotation, _parent);
            enemy.SetActive(false); // deactivate enemy
            _enemyInstances[room.id] = enemy;
        }
    }

    public void ActivateEnemyInRoom(Room room)
    {
        if (_enemyInstances.TryGetValue(room.id, out var enemy))
        {
            enemy.SetActive(true);
        }
    }
}