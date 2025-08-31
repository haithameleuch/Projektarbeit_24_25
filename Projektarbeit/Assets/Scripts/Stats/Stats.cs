using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    [SerializeField]
    private List<float> maxStats = new List<float>();
    
    [SerializeField]
    private List<float> curStats = new List<float>();

    public List<float> GetMaxStatsList() => maxStats;
    public List<float> GetCurStatsList() => curStats;

    public void SetStats(List<float> cur, List<float> max)
    {
        maxStats = max;
        curStats = cur;
    }

    public float GetMaxStats(int stat)
    {
        return maxStats[stat];
    }

    public float GetCurStats(int stat)
    {
        return curStats[stat];
    }

    public void IncreaseMaxStat(int stat, float amount)
    {
        maxStats[stat] += amount;
    }

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

    public void IncreaseCurStat(int stat, float amount)
    {
        curStats[stat] += amount;
        if (curStats[stat] > maxStats[stat])
        {
            curStats[stat] = maxStats[stat];
        }
    }

    public void DecreaseCurStat(int stat, float amount)
    {
        curStats[stat] -= amount;
        if (curStats[stat] < 0)
        {
            curStats[stat] = 0;
        }
    }
    
    public void AddToMaxPreserveRatio(int stat, float delta)
    {
        if (stat < 0) return;
        while (maxStats.Count <= stat) maxStats.Add(0f);
        while (curStats.Count <= stat) curStats.Add(0f);

        var oldMax = maxStats[stat];
        var ratio  = oldMax > 0f ? (curStats[stat] / oldMax) : 0f;
        var newMax = Mathf.Max(0f, oldMax + delta);

        maxStats[stat] = newMax;
        curStats[stat] = Mathf.Clamp(newMax * ratio, 0f, newMax);
    }


}
