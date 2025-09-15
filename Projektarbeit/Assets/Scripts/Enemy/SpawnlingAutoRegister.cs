using Dungeon;
using System;
using Manager;
using Spawning;
using UnityEngine;

namespace Enemy
{
    public class SpawnlingAutoRegister : MonoBehaviour
    {
        /// <summary>
        /// Unity Awake method called when the GameObject is initialized.
        /// Sets up the EnemyDeathReporter and registers the Spawnling with the current room's spawner.
        /// </summary>
        private void Awake()
        {
            // Get the GameManager instance
            var gm = GameManagerVoronoi.Instance;
            
            // Get the current room from the GameManager
            var room = gm?.CurrentRoom;
            if (room == null) return;
            
            // Ensure an EnemyDeathReporter exists on this Spawnling
            var reporter = GetComponent<EnemyDeathReporter>();
            if (reporter == null) reporter = gameObject.AddComponent<EnemyDeathReporter>();
            
            // Register Spawnling spawn and death callbacks depending on the room type
            switch (room.Type)
            {
                case RoomType.Boss:
                    reporter.Init(room.ID, BossSpawnerVoronoi.RegisterBossMinionDeath);
                    BossSpawnerVoronoi.RegisterBossMinionSpawn(room.ID);
                    break;
                case RoomType.Enemy:
                    reporter.Init(room.ID, EnemySpawnerVoronoi.RegisterEnemyMinionDeath);
                    EnemySpawnerVoronoi.RegisterEnemyMinionSpawn(room.ID);
                    break;
                case RoomType.Start:
                case RoomType.Normal:
                case RoomType.Item:
                case RoomType.MiniGame:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
