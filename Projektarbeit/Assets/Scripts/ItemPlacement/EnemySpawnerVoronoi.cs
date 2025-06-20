using System.Collections.Generic;
using UnityEngine;

namespace ItemPlacement
{
    /// <summary>
    /// Spawns and manages enemies in Voronoi-based dungeon rooms.
    /// Enemies are spawned based on the room's size (incircle radius).
    /// </summary>
    public class EnemySpawnerVoronoi
    {
        /// <summary>
        /// Stores all enemies per room, keyed by room ID.
        /// </summary>
        private readonly Dictionary<int, List<GameObject>> _enemyInstancesPerRoom = new();

        /// <summary>
        /// Initializes the enemy spawner by instantiating enemies in all enemy rooms.
        /// The number of enemies depends on the room size (incircle radius).
        /// </summary>
        /// <param name="enemyRooms">List of enemy rooms</param>
        /// <param name="enemyPrefabs">List of enemy prefabs to randomly choose from</param>
        /// <param name="parent">Parent transform under which the enemies are spawned</param>
        public EnemySpawnerVoronoi(List<Room> enemyRooms, List<GameObject> enemyPrefabs, Transform parent)
        {
            foreach (var room in enemyRooms)
            {
                List<GameObject> enemiesInRoom = new();

                // estimate room size based on incircle radius
                var radius = room.getIncircleRadius();

                // determine the number of enemies (1 to 5 depending on radius)
                var enemyCount = Mathf.Clamp(Mathf.RoundToInt(radius * 0.8f), 1, 5);

                for (var i = 0; i < enemyCount; i++)
                {
                    var chosenPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

                    // Distribute enemies evenly in a circle around the room center
                    var angle = i * (360f / enemyCount);
                    var distanceFromCenter = Mathf.Min(radius * 0.6f, 3f); // stay inside the room
                    var xOffset = Mathf.Cos(angle * Mathf.Deg2Rad) * distanceFromCenter;
                    var zOffset = Mathf.Sin(angle * Mathf.Deg2Rad) * distanceFromCenter;

                    var spawnPosition = new Vector3(room.center.x + xOffset, 0f, room.center.y + zOffset);
                    var rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    var enemy = Object.Instantiate(chosenPrefab, spawnPosition, rotation, parent);
                    enemy.SetActive(false);

                    enemiesInRoom.Add(enemy);
                }

                _enemyInstancesPerRoom[room.id] = enemiesInRoom;
            }
        }
    
        /// <summary>
        /// Activates all enemies in the given room when the player enters it.
        /// </summary>
        /// <param name="room">Room whose enemies should be activated</param>
        public void ActivateEnemyInRoom(Room room)
        {
            if (!_enemyInstancesPerRoom.TryGetValue(room.id, out var enemies)) return;
            foreach (var enemy in enemies)
            {
                enemy.SetActive(true);
            }
        }
    }
}