using UnityEngine;
using UnityEngine.Serialization;

namespace Items
{
    /// <summary>
    /// Consumable-Class is used to handle all items that increase a players current stats.
    /// Example: Health-Potion
    /// </summary>
    [CreateAssetMenu(fileName = "Consumable", menuName = "Scriptable Objects/Consumable")]
    public class Consumable : Item
    {
        /// <summary>
        /// Which stat this consumable should give (a.t.m. 0 = Health, 1 = Damage, 2 = Speed)
        /// </summary>
        [FormerlySerializedAs("stat_to_restore")] [SerializeField]
        public int statToRestore = 0;

        /// <summary>
        /// The amount by which the specific current stat should be increased
        /// </summary>
        [FormerlySerializedAs("amount_to_restore")] [SerializeField]
        public float amountToRestore = 0;

        /// <summary>
        /// This method increases the specified stat by the set amount for the object that is calling this method.
        /// Only objects with an inventory can call this method and the increase will be given to the object that the inventory is attached to.
        /// </summary>
        /// <param name="inv">The inventory calling this method to ease the removal of the consumable.</param>
        public override void use(Inventory inv)
        {
            // Increase the current stat by the amount
            inv.gameObject.GetComponent<Stats>().IncreaseCurStat(statToRestore,amountToRestore);
            
            // Remove the item after usage
            inv.removeItem(this);
        }
    }
}
