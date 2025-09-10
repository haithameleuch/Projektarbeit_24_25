using Dungeon;
using Manager;
using ItemPlacement;
using UnityEngine;

namespace Enemy
{
    public class SpawnlingAutoRegister : MonoBehaviour
    {
        private void Awake()
        {
            var gm = GameManagerVoronoi.Instance;
            var room = gm?.CurrentRoom;
            if (room == null) return;
            
            var reporter = GetComponent<EnemyDeathReporter>();
            if (reporter == null) reporter = gameObject.AddComponent<EnemyDeathReporter>();
            
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
            }
        }
    }
}
