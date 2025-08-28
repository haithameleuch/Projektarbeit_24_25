using System.Collections.Generic;
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
        
        private static BossSpawnerVoronoi _instance;

        public BossSpawnerVoronoi(Room bossRoom, List<GameObject> bossPrefabs, List<GameObject> obstaclePrefabs, Transform parent)
        {
            _instance = this;
            
            if (bossRoom == null || obstaclePrefabs.Count == 0 || bossPrefabs == null || bossPrefabs.Count == 0) return;

            _bossRoomId = bossRoom.id;

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

                // Report death
                var reporter = boss.AddComponent<EnemyDeathReporter>();
                reporter.Init(bossRoom.id, OnBossEnemyDied);

                spawned.Add(boss);
            }

            _bossInstancesPerRoom[bossRoom.id] = spawned;
            _alivePerRoom[bossRoom.id] = spawned.Count;
        }
        
        public void ActivateBossInRoom(Room room)
        {
            if (room == null || room.id != _bossRoomId) return;
            if (!_bossInstancesPerRoom.TryGetValue(room.id, out var bosses)) return;

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
            
            // All bosses defeated -> Boss doors open + save flag
            EventManager.Instance?.TriggerOpenBossDoors();
            SaveSystemManager.SetBossRoomOpen(true);
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
