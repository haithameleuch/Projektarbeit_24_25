using Manager;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// The class is used to create the Boss-Room-Key and to be able to open the doors of the boss room on the keys usage.
    /// </summary>
    [CreateAssetMenu(fileName = "BossKey", menuName = "Scriptable Objects/BossKey")]
    public class BossKey : Item
    {
        /// <summary>
        /// Inherited method from the superclass.
        /// Makes it possible that the boss room doors open through the use of the key.
        /// </summary>
        /// <param name="inv">The inventory that called the use action.</param>
        public override void use(Inventory inv)
        {
            // Only when the player is standing in front of the boss room can the key work
            bool valid = GameManagerVoronoi.Instance.OnBossKeyUsed();

            // If the player used the key in front of the boss room it will be removed
            if (valid)
            {
                inv.removeItem(this);
            }
        }
    }
}