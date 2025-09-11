using UnityEngine;

namespace Items
{
    /// <summary>
    /// The Glyphs-Class is only used to create items with the glyphs icon so the player knows how the glyphs look.
    /// </summary>
    [CreateAssetMenu(fileName = "Glyphs", menuName = "Scriptable Objects/Glyphs")]
    public class Glyphs : Item
    {
        /// <summary>
        /// Unused in this class but inherited from the superclass.
        /// </summary>
        /// <param name="inv">The inventory calling this method.</param>
        public override void Use(Inventory.Inventory inv)
        {
        }
    }
}
