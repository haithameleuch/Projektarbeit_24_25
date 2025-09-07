using UnityEngine;

/// <summary>
/// Data-Class representing the consumable type as a sub-class of item 
/// </summary>
[CreateAssetMenu(fileName = "Consumable", menuName = "Scriptable Objects/Consumable")]
public class Consumable : Item
{
    /// <summary>
    /// Which stat this consumable should give (a.t.m. 0 = Health, 1 = Damage, 2 = Speed)
    /// </summary>
    [SerializeField]
    public int stat_to_restore = 0;

    /// <summary>
    /// The amount by which the specific stat should be increased/decreased
    /// </summary>
    [SerializeField]
    public float amount_to_restore = 0;

    /// <summary>
    /// //Todo implement
    /// This method will apply the stat increase to the player
    /// </summary>
    /// <param name="inv">The inventory calling this method to handle interactions locally</param>
    public override void use(Inventory inv)
    {
        inv.gameObject.GetComponent<Stats>().IncreaseCurStat(stat_to_restore,amount_to_restore);
        inv.removeItem(this);
        Debug.Log(this._name + " was used.");
    }
}
