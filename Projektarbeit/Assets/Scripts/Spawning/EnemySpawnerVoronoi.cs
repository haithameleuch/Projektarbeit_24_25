using System.Collections.Generic;
using Dungeon;
using Enemy;
using Manager;
using UnityEngine;

namespace Spawning
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
        /// stores all enemies that are alive per room, keyed by room ID.
        /// </summary>
        private readonly Dictionary<int, int> _alivePerRoom = new();
        
        /// <summary>
        /// static instance of the spawner
        /// </summary>
        private static EnemySpawnerVoronoi _instance;

        /// <summary>
        /// Initializes the enemy spawner by instantiating enemies in all enemy rooms.
        /// The number of enemies depends on the room size (incircle radius).
        /// </summary>
        /// <param name="enemyRooms">List of enemy rooms</param>
        /// <param name="enemyPrefabs">List of enemy prefabs to randomly choose from</param>
        /// <param name="parent">Parent transform under which the enemies are spawned</param>
        public EnemySpawnerVoronoi(List<Room> enemyRooms, List<GameObject> enemyPrefabs, Transform parent)
        {
            _instance = this;
            
            foreach (var room in enemyRooms)
            {
                List<GameObject> enemiesInRoom = new();

                // estimate room size based on incircle radius
                var radius = room.GetIncircleRadius();

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

                    var spawnPosition = new Vector3(room.Center.X + xOffset, 0f, room.Center.Y + zOffset);
                    var rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    var enemy = Object.Instantiate(chosenPrefab, spawnPosition, rotation, parent);
                    enemy.SetActive(false);
                    
                    // per-level scaling
                    var s = enemy.GetComponent<Stats.Stats>();
                    EnemyLevelScaler.Apply(s, isBoss: false);
                    
                    // Report death
                    var reporter = enemy.AddComponent<EnemyDeathReporter>();
                    reporter.Init(room.ID, OnEnemyDied);

                    enemiesInRoom.Add(enemy);
                }

                _enemyInstancesPerRoom[room.ID] = enemiesInRoom;
                _alivePerRoom[room.ID] = enemiesInRoom.Count;
            }
        }
    
        /// <summary>
        /// Activates all enemies in the given room when the player enters it.
        /// </summary>
        /// <param name="room">Room whose enemies should be activated</param>
        public void ActivateEnemyInRoom(Room room)
        {
            if (!_enemyInstancesPerRoom.TryGetValue(room.ID, out var enemies)) return;
            foreach (var enemy in enemies)
            {
                enemy.SetActive(true);
            }
        }
        
        /// <summary>
        /// function that tracks if all enemies in the corresponding room have been defeated, also decreases living amount
        /// </summary>
        /// <param name="roomId">room id of the room where an enemies have been defeated</param>
        private void OnEnemyDied(int roomId)
        {
            if (!_alivePerRoom.ContainsKey(roomId)) return;

            _alivePerRoom[roomId] = Mathf.Max(0, _alivePerRoom[roomId] - 1);

            if (_alivePerRoom[roomId] == 0)
            {
                // All enemies defeated -> doors open
                EventManager.Instance?.TriggerOpenDoors();
            }
        }
        
        /// <summary>
        /// increases amount of living enemies in the corresponding room because there are enemies that can spawn smaller enemies to their help
        /// </summary>
        /// <param name="roomId">id of room where living amount should be increased</param>
        public static void RegisterEnemyMinionSpawn(int roomId)
        {
            _instance?.IncrementAlive(roomId);
        }

        /// <summary>
        /// decreases amount of living enemies in the corresponding room because there are enemies that can spawn smaller enemies to their help
        /// </summary>
        /// <param name="roomId">id of room where living amount should be decreased</param>
        public static void RegisterEnemyMinionDeath(int roomId)
        {
            _instance?.OnEnemyDied(roomId);
        }

        /// <summary>
        /// function that increases the tracker of the living enemies per room
        /// </summary>
        /// <param name="roomId">id of room where living amount should be increased</param>
        private void IncrementAlive(int roomId)
        {
            if (!_alivePerRoom.ContainsKey(roomId))
                _alivePerRoom[roomId] = 0;
            _alivePerRoom[roomId] += 1;
        }
    }
}