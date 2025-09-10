using System.Collections.Generic;
using Enemy;
using UnityEngine;
using Saving;

namespace ItemPlacement
{
    /// <summary>
    /// class to manage boss & obstacle spawning, setting the boss active and inactive
    /// and also spawning the exit prefab to leave current level and proceed
    /// </summary>
    public class BossSpawnerVoronoi
    {
        /// <summary>
        /// Stores all bosses per room by room id as the key which are spawned
        /// </summary>
        private readonly Dictionary<int, List<GameObject>> _bossInstancesPerRoom = new();
        /// <summary>
        /// Stores the amount of enemies which are alive per boss room by room id as the key
        /// </summary>
        private readonly Dictionary<int, int> _alivePerRoom = new();
        /// <summary>
        /// stores the id of the current boss room 
        /// </summary>
        private readonly int _bossRoomId = -1;

        /// <summary>
        /// Parent transform under which the enemies are spawned
        /// </summary>
        private readonly Transform _parent;
        
        /// <summary>
        /// Prefab which will be spawned after the boss has been defeated
        /// </summary>
        private readonly GameObject _levelExitPrefab;
        
        /// <summary>
        /// Room metadata object reference of the current boss room
        /// </summary>
        private Room _bossRoomRef;
        
        /// <summary>
        /// private bool member if the exit has been spawned already 
        /// </summary>
        private bool _exitSpawned;
        
        /// <summary>
        /// static instance of BossSpawnerVoronoi
        /// </summary>
        private static BossSpawnerVoronoi _instance;

        /// <summary>
        /// constructor of Boss Spawner Voronoi which will set the members, creates the static instance,
        /// spawnes the boss prefab & obstacles in the corresponding bossRoom
        /// </summary>
        /// <param name="bossRoom">room data of boss room into which the boss will be spawned</param>
        /// <param name="bossPrefabs">List of boss prefabs which will be used to spawn </param>
        /// <param name="obstaclePrefabs">List of prefabs that will be spawned as obstacles</param>
        /// <param name="parent">Parent transform under which the enemies are spawned</param>
        /// <param name="levelExitPrefab">Prefab which will be spawned after beating the boss</param>
        public BossSpawnerVoronoi(Room bossRoom, List<GameObject> bossPrefabs, List<GameObject> obstaclePrefabs, Transform parent, GameObject levelExitPrefab = null)
        {
            _instance = this;
            
            _parent = parent;
            _levelExitPrefab = levelExitPrefab;
            _bossRoomRef = bossRoom;
            
            var bossCleared = SaveSystemManager.GetBossCleared();
            
            if (bossRoom == null || obstaclePrefabs == null || obstaclePrefabs.Count == 0) return;
            if (!bossCleared && (bossPrefabs == null || bossPrefabs.Count == 0)) return;

            _bossRoomId = bossRoom.id;

            // random 1–5 Obstacles
            var center = new Vector3(bossRoom.center.x, 0f, bossRoom.center.y);
            var roomCircleRadius = bossRoom.getIncircleRadius();
            
            // count scaled with room size (1–5)
            var obsCount = Mathf.Clamp(Mathf.RoundToInt(roomCircleRadius * 0.8f), 1, 5);
            
            const float wallPadding = 0.6f;
            var bossRing  = Mathf.Min(roomCircleRadius * 0.6f, 3f);
            var obsRing   = Mathf.Clamp(bossRing + 0.8f, 1f, roomCircleRadius - wallPadding);

            for (var i = 0; i < obsCount; i++)
            {
                var angleDeg = i * (360f / obsCount);
                var rad      = angleDeg * Mathf.Deg2Rad;

                var pos = center + new Vector3(Mathf.Cos(rad) * obsRing, 0f, Mathf.Sin(rad) * obsRing);
                var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                
                var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
                Object.Instantiate(prefab, pos, rot, parent);
            }

            // If boss already cleared -> spawn exit and return (no bosses)
            if (bossCleared)
            {
                SpawnLevelExit();
                _exitSpawned = true;
                return;
            }

            List<GameObject> spawned = new();
            
            var count = bossPrefabs.Count;
            var radius = Mathf.Min(bossRoom.getIncircleRadius() * 0.6f, 3f);

            for (var i = 0; i < count; i++)
            {
                var prefab = bossPrefabs[i];

                Vector3 spawnPos;
                if (count == 1)
                {
                    spawnPos = new Vector3(bossRoom.center.x, 0f, bossRoom.center.y);
                }
                else
                {
                    var angle = i * (360f / count) * Mathf.Deg2Rad;
                    var x = Mathf.Cos(angle) * radius;
                    var z = Mathf.Sin(angle) * radius;
                    spawnPos = new Vector3(bossRoom.center.x + x, 0f, bossRoom.center.y + z);
                }

                var rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                var boss = Object.Instantiate(prefab, spawnPos, rotation, parent);
                boss.SetActive(false);
                
                // per-level scaling
                var s = boss.GetComponent<Stats>();
                EnemyLevelScaler.Apply(s, isBoss: true);

                // Report death
                var reporter = boss.AddComponent<EnemyDeathReporter>();
                reporter.Init(bossRoom.id, OnBossEnemyDied);

                spawned.Add(boss);
            }

            _bossInstancesPerRoom[bossRoom.id] = spawned;
            _alivePerRoom[bossRoom.id] = spawned.Count;
        }
        
        /// <summary>
        /// sets the boss' gameobjects in the corresponding room active 
        /// </summary>
        /// <param name="room">id to room where gameobjects/enemies should be activated</param>
        public void ActivateBossInRoom(Room room)
        {
            if (room == null || room.id != _bossRoomId) return;
            if (!_bossInstancesPerRoom.TryGetValue(room.id, out var bosses)) return;

            foreach (var b in bosses)
            {
                if (b) b.SetActive(true);
            }
        }

        /// <summary>
        /// function that tracks if all enemies in the corresponding boss room have been defeated and the exit prefab has been spawned
        /// if yes it returns, else it will spawn the exit and sets _exitSpawned to true
        /// decreases living amount of the room
        /// </summary>
        /// <param name="roomId">room id of the boss room where the boss has been defeated</param>
        private void OnBossEnemyDied(int roomId)
        {
            if (!_alivePerRoom.ContainsKey(roomId)) return;

            _alivePerRoom[roomId] = Mathf.Max(0, _alivePerRoom[roomId] - 1);

            if (_alivePerRoom[roomId] != 0 || roomId != _bossRoomId) return;
            
            // All bosses defeated -> Boss doors open
            EventManager.Instance?.TriggerOpenBossDoors();
            SaveSystemManager.SetBossRoomOpen(true);
            SaveSystemManager.SetBossCleared(true);

            if (_exitSpawned) return;
            SpawnLevelExit();
            _exitSpawned = true;
        }
        
        /// <summary>
        /// instantiates level exit prefab to the correspoinding boss room and uses the parent of the room
        /// </summary>
        private void SpawnLevelExit()
        {
            if (_bossRoomRef == null || _levelExitPrefab == null) return;

            var pos = new Vector3(_bossRoomRef.center.x + 0.75f, 0f, _bossRoomRef.center.y);
            var rot = Quaternion.identity;

            Transform safeParent = null;
            if (_parent != null && _parent.gameObject != null &&
                _parent.gameObject.scene.IsValid() && _parent.gameObject.scene.isLoaded)
            {
                safeParent = _parent;
            }
            
            var exit = Object.Instantiate(_levelExitPrefab, pos, rot, safeParent);
            
            if (exit.GetComponent<LevelExitInteraction>() == null)
                exit.AddComponent<LevelExitInteraction>();
        }
        
        /// <summary>
        /// increases amount of living enemies in the corresponding room because the boss can spawn smaller enemies to his help
        /// </summary>
        /// <param name="roomId">id of boss room where living amount should be increased</param>
        public static void RegisterBossMinionSpawn(int roomId)
        {
            _instance?.IncrementAlive(roomId);
        }

        /// <summary>
        /// reduces amount of living enemies in the corresponding room because the boss can spawn smaller enemies to his help
        /// </summary>
        /// <param name="roomId">id of boss room where living amount should be decreased</param>
        public static void RegisterBossMinionDeath(int roomId)
        {
            _instance?.OnBossEnemyDied(roomId);
        }
        
        /// <summary>
        /// function that increases the tracker of the living enemies per room
        /// </summary>
        /// <param name="roomId">id of boss room where living amount should be increased</param>
        private void IncrementAlive(int roomId)
        {
            if (!_alivePerRoom.ContainsKey(roomId))
                _alivePerRoom[roomId] = 0;
            _alivePerRoom[roomId] += 1;
        }
    }
}
