using Saving;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Applies per-level scaling to enemies/bosses at spawn time.
    /// Level 1 = base prefab values, from Level 2 upwards add fixed increments.
    /// </summary>
    public static class EnemyLevelScaler
    {
        // Per-level increments (from level 2 upwards)
        private const float EnemyHpPerLevel  = 20f;
        private const float EnemyDmgPerLevel = 1f;

        private const float BossHpPerLevel   = 100f;
        private const float BossDmgPerLevel  = 2f;

        /// <summary>
        /// Ensure stat lists have at least N entries to avoid index errors.
        /// </summary>
        private static void EnsureMinSlots(Stats s, int minCount)
        {
            var max = s.GetMaxStatsList();
            var cur = s.GetCurStatsList();
            while (max.Count < minCount) max.Add(0f);
            while (cur.Count < minCount) cur.Add(0f);
        }

        /// <summary>
        /// Apply deterministic scaling based on current level.
        /// </summary>
        public static void Apply(Stats s, bool isBoss = false)
        {
            if (s == null) return;

            var level = Mathf.Max(1, SaveSystemManager.GetLevel());
            var steps = Mathf.Max(0, level - 1);

            if (steps == 0) return;

            EnsureMinSlots(s, 3); // 0=Health, 1=Damage, 2=Speed

            var hpAdd  = steps * (isBoss ? BossHpPerLevel  : EnemyHpPerLevel);
            var dmgAdd = steps * (isBoss ? BossDmgPerLevel : EnemyDmgPerLevel);

            // Health
            s.IncreaseMaxStat(0, hpAdd);
            s.IncreaseCurStat(0, hpAdd);

            // Damage
            s.IncreaseMaxStat(1, dmgAdd);
            s.IncreaseCurStat(1, dmgAdd);
        }
    }
}