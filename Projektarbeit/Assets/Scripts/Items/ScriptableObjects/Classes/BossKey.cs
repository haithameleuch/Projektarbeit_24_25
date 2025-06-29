using Manager;
using UnityEngine;

[CreateAssetMenu(fileName = "BossKey", menuName = "Scriptable Objects/BossKey")]
public class BossKey : Item
{
    public override void use(Inventory inv)
    {
        // Only when the player is standing in front of the boss room can the key work
        bool valid = GameManagerVoronoi.Instance.OnBossKeyUsed();

        if (valid)
        {
            inv.removeItem(this);
            Debug.Log("[BossKey] Boss doors opened, key removed.");
        }
        else
        {
            Debug.Log("[BossKey] You are not in front of the boss room – key remains.");
        }
    }
}