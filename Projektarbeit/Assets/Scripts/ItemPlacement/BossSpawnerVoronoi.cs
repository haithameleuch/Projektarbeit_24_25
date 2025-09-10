using System.Collections.Generic;
using Dungeon;
using Enemy;
using UnityEngine;
using Saving;

namespace ItemPlacement
{
    public class BossSpawnerVoronoi
    {
        private readonly Dictionary<int, List<GameObject>> _bossInstancesPerRoom = new();
        private readonly Dictionary<int, int> _alivePerRoom = new();
        private readonly int _bossRoomId = -1;

        private readonly Transform _parent;
        private readonly GameObject _levelExitPrefab;
        private Room _bossRoomRef;
        private bool _exitSpawned;
        
        private static BossSpawnerVoronoi _instance;

        public BossSpawnerVoronoi(Room bossRoom, List<GameObject> bossPrefabs, List<GameObject> obstaclePrefabs, Transform parent, GameObject levelExitPrefab = null)
        {
            _instance = this;
            
            _parent = parent;
            _levelExitPrefab = levelExitPrefab;
            _bossRoomRef = bossRoom;
            
            var bossCleared = SaveSystemManager.GetBossCleared();
            
            if (bossRoom == null || obstaclePrefabs == null || obstaclePrefabs.Count == 0) return;
            if (!bossCleared && (bossPrefabs == null || bossPrefabs.Count == 0)) return;

            _bossRoomId = bossRoom.ID;

            // random 1–5 Obstacles
            var center = new Vector3(bossRoom.Center.X, 0f, bossRoom.Center.Y);
            var roomCircleRadius = bossRoom.GetIncircleRadius();
            
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
            var radius = Mathf.Min(bossRoom.GetIncircleRadius() * 0.6f, 3f);

            for (var i = 0; i < count; i++)
            {
                var prefab = bossPrefabs[i];

                Vector3 spawnPos;
                if (count == 1)
                {
                    spawnPos = new Vector3(bossRoom.Center.X, 0f, bossRoom.Center.Y);
                }
                else
                {
                    var angle = i * (360f / count) * Mathf.Deg2Rad;
                    var x = Mathf.Cos(angle) * radius;
                    var z = Mathf.Sin(angle) * radius;
                    spawnPos = new Vector3(bossRoom.Center.X + x, 0f, bossRoom.Center.Y + z);
                }

                var rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                var boss = Object.Instantiate(prefab, spawnPos, rotation, parent);
                boss.SetActive(false);
                
                // per-level scaling
                var s = boss.GetComponent<Stats>();
                EnemyLevelScaler.Apply(s, isBoss: true);

                // Report death
                var reporter = boss.AddComponent<EnemyDeathReporter>();
                reporter.Init(bossRoom.ID, OnBossEnemyDied);

                spawned.Add(boss);
            }

            _bossInstancesPerRoom[bossRoom.ID] = spawned;
            _alivePerRoom[bossRoom.ID] = spawned.Count;
        }
        
        public void ActivateBossInRoom(Room room)
        {
            if (room == null || room.ID != _bossRoomId) return;
            if (!_bossInstancesPerRoom.TryGetValue(room.ID, out var bosses)) return;

            foreach (var b in bosses)
            {
                if (b) b.SetActive(true);
            }
        }

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
        
        private void SpawnLevelExit()
        {
            if (_bossRoomRef == null || _levelExitPrefab == null) return;

            var pos = new Vector3(_bossRoomRef.Center.X + 0.75f, 0f, _bossRoomRef.Center.Y);
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
        
        public static void RegisterBossMinionSpawn(int roomId)
        {
            _instance?.IncrementAlive(roomId);
        }

        public static void RegisterBossMinionDeath(int roomId)
        {
            _instance?.OnBossEnemyDied(roomId);
        }
        
        private void IncrementAlive(int roomId)
        {
            if (!_alivePerRoom.ContainsKey(roomId))
                _alivePerRoom[roomId] = 0;
            _alivePerRoom[roomId] += 1;
        }
    }
}
