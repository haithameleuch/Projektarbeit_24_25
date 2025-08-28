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
            
            switch (room.type)
            {
                case RoomType.Boss:
                    reporter.Init(room.id, BossSpawnerVoronoi.RegisterBossMinionDeath);
                    BossSpawnerVoronoi.RegisterBossMinionSpawn(room.id);
                    break;
                case RoomType.Enemy:
                    reporter.Init(room.id, EnemySpawnerVoronoi.RegisterEnemyMinionDeath);
                    EnemySpawnerVoronoi.RegisterEnemyMinionSpawn(room.id);
                    break;
            }
        }
    }
}
