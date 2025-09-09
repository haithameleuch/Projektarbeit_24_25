using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Stats-Class is used to keep track of an objects Health, Damage and Speed (and possibly more in the future).
/// </summary>
public class Stats : MonoBehaviour
{
    /*  For the stats it holds that the index in the list identifies the stat:
     *  0 = Health
     *  1 = Damage
     *  2 = Speed
     */
    
    // The maximum a stat can reach
    [SerializeField]
    private List<float> maxStats = new List<float>();

    // The value the stat has at the moment (at the start the values are equal to the max stats)
    [SerializeField]
    private List<float> curStats = new List<float>();

    /// <summary>
    /// Getter method for the list of maximum stats
    /// </summary>
    /// <returns>The list "maxStats"</returns>
    public List<float> GetMaxStatsList() => maxStats;
    
    /// <summary>
    /// Getter method for the list of current stats
    /// </summary>
    /// <returns>The list "curStats"</returns>
    public List<float> GetCurStatsList() => curStats;

    /// <summary>
    /// With this method it is possible to set the values of both lists through the script instead of the serialize field.
    /// </summary>
    /// <param name="cur">A float list of the new values for the current stats.</param>
    /// <param name="max">A float list of the new values for the maximum stats.</param>
    public void SetStats(List<float> cur, List<float> max)
    {
        maxStats = max;
        curStats = cur;
    }

    /// <summary>
    /// Getter method for a specific maximum stat.
    /// </summary>
    /// <param name="stat">The index of the stat you want to get.</param>
    /// <returns>The float value of the wanted stat.</returns>
    public float GetMaxStats(int stat)
    {
        return maxStats[stat];
    }

    /// <summary>
    /// Getter method for a specific current stat.
    /// </summary>
    /// <param name="stat">The index of the stat you want to get.</param>
    /// <returns>The float value of the wanted stat.</returns>
    public float GetCurStats(int stat)
    {
        return curStats[stat];
    }

    /// <summary>
    /// Increases a maximum stat by a specific value.
    /// </summary>
    /// <param name="stat">The index of the stat you want to increase.</param>
    /// <param name="amount">The float value by which the stat shall be increased.</param>
    public void IncreaseMaxStat(int stat, float amount)
    {
        maxStats[stat] += amount;
    }

    /// <summary>
    /// Decreases a maximum stat by a specific value or sets it to 0 if the value goes into the negative.
    /// It also sets the current stat if the current stat is higher than the new maximum stat value.
    /// </summary>
    /// <param name="stat">The index of the stat you want to decrease.</param>
    /// <param name="amount">The float value by which the stat shall be decreased.</param>
    public void DecreaseMaxStat(int stat, float amount)
    {
        maxStats[stat] -= amount;
        
        if (maxStats[stat] < 0)
        {
            maxStats[stat] = 0;
        }
    
        if (stat >= 0 && stat < curStats.Count && stat < maxStats.Count)
        {
            if (curStats[stat] > maxStats[stat])
                curStats[stat] = maxStats[stat];
        }
    }

    /// <summary>
    /// Increases a current stat by a specific value.
    /// </summary>
    /// <param name="stat">The index of the stat you want to increase.</param>
    /// <param name="amount">The float value by which the stat shall be increased.</param>
    public void IncreaseCurStat(int stat, float amount)
    {
        curStats[stat] += amount;
        if (curStats[stat] > maxStats[stat])
        {
            curStats[stat] = maxStats[stat];
        }
    }

    /// <summary>
    /// Decreases a current stat by a specific value or sets it to 0 if the value goes into the negative.
    /// </summary>
    /// <param name="stat">The index of the stat you want to decrease.</param>
    /// <param name="amount">The float value by which the stat shall be decreased.</param>
    public void DecreaseCurStat(int stat, float amount)
    {
        curStats[stat] -= amount;
        
        if (curStats[stat] < 0)
        {
            curStats[stat] = 0;
        }
    }
    
    /// <summary>
    /// The Method is used to change a stat while keeping the percentage between maximum and current stat the same.
    /// </summary>
    /// <param name="stat">Index of the stat you want to change.</param>
    /// <param name="delta">The value by which the stats should be changed.</param>
    public void AddToMaxPreserveRatio(int stat, float delta)
    {
        // Stats below 0 are invalid
        if (stat < 0) return;
        
        // If the wanted stat is not present yet add it with a 0
        while (maxStats.Count <= stat) maxStats.Add(0f);
        while (curStats.Count <= stat) curStats.Add(0f);

        // Calculate the ratio of maximum and current stats
        var oldMax = maxStats[stat];
        var ratio  = oldMax > 0f ? (curStats[stat] / oldMax) : 0f;
        
        // Calculate the new maximum
        var newMax = Mathf.Max(0f, oldMax + delta);

        // Set new maximum
        maxStats[stat] = newMax;
        
        // Set the new current value staying true to the ratio calculated
        curStats[stat] = Mathf.Clamp(newMax * ratio, 0f, newMax);
    }
}
