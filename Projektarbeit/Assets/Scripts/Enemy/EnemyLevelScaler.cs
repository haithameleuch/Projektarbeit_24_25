using Manager;
using Saving;
using Stats;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Applies per-level scaling to enemies/bosses at spawn time.
    /// Level 1 = base prefab values, from Level 2 upwards add fixed increments.
    /// </summary>
    public static class EnemyLevelScaler
    {
        /// <summary>
        /// Additional health granted per level (starting from level 2) for regular enemies.
        /// </summary>
        private const float EnemyHpPerLevel  = 20f;
        
        /// <summary>
        /// Additional damage granted per level (starting from level 2) for regular enemies.
        /// </summary>
        private const float EnemyDmgPerLevel = 1f;

        /// <summary>
        /// Additional health granted per level (starting from level 2) for bosses.
        /// </summary>
        private const float BossHpPerLevel   = 100f;
        
        /// <summary>
        /// Additional damage granted per level (starting from level 2) for bosses.
        /// </summary>
        private const float BossDmgPerLevel  = 2f;

        /// <summary>
        /// Ensures that the <see cref="Stats"/> object has at least the required number
        /// of stat slots in both its maximum and current stat lists, preventing index errors.
        /// </summary>
        /// <param name="s">The <see cref="Stats"/> object to expand if needed.</param>
        /// <param name="minCount">Minimum number of stat slots required.</param>
        private static void EnsureMinSlots(Stats.Stats s, int minCount)
        {
            var max = s.GetMaxStatsList();
            var cur = s.GetCurStatsList();
            while (max.Count < minCount) max.Add(0f);
            while (cur.Count < minCount) cur.Add(0f);
        }

        /// <summary>
        /// Applies deterministic stat scaling to an enemy or boss based on the current game level.
        /// </summary>
        /// <param name="s">The <see cref="Stats"/> object of the enemy or boss.</param>
        /// <param name="isBoss">If <c>true</c>, applies boss-level increments; otherwise, enemy increments.</param>
        public static void Apply(Stats.Stats s, bool isBoss = false)
        {
            if (s == null) return;

            // Fetch current level, minimum 1
            var level = Mathf.Max(1, SaveSystemManager.GetLevel());
            var steps = Mathf.Max(0, level - 1);

            // No scaling at level 1
            if (steps == 0) return;

            EnsureMinSlots(s, 3); // 0=Health, 1=Damage, 2=Speed

            // Determine per-level increments
            var hpAdd  = steps * (isBoss ? BossHpPerLevel  : EnemyHpPerLevel);
            var dmgAdd = steps * (isBoss ? BossDmgPerLevel : EnemyDmgPerLevel);

            // Apply health scaling
            s.IncreaseMaxStat(0, hpAdd);
            s.IncreaseCurStat(0, hpAdd);

            // Apply damage scaling
            s.IncreaseMaxStat(1, dmgAdd);
            s.IncreaseCurStat(1, dmgAdd);
        }
    }
}