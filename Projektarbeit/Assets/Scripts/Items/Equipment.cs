using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Items
{
    /// <summary>
    /// Enumeration used to keep track of the tool types.
    /// Currently only pickaxe is used.
    /// </summary>
    public enum ToolType
    {
        None,
        Pickaxe,
        Sword,
        // … other tools
    }

    /// <summary>
    /// The Equipment-Class is used for making items usable for wearing by the player (enemies should work too).
    /// They can increase or decrease the stats of the object on which they are equipped only while they are worn.
    /// Examples: Helmet, Sword, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment", menuName = "Scriptable Objects/Equipment")]
    public class Equipment : Item
    {
        // The slot in which an item will be equipped
        // 0 : Head
        // 1 : Body
        // 2 : Legs
        // 3 : Feet
        // 4 : Right_Hand
        // 5 : Left_Hand
        [FormerlySerializedAs("equip_slot")]
        [SerializeField]
        [Range(0, 5)]
        public int equipSlot = 0;

        // List with all the stat increases the equipment should provide
        // Example: The helmet provides 10 Health, 0 Damage and -2 Speed, the list would look like this (10,0,-2)
        [FormerlySerializedAs("stat_increases")] [SerializeField]
        public List<int> statIncreases = new List<int>();
    
        [SerializeField]
        public ToolType toolType = ToolType.None;

        /// <summary>
        /// This method equips an item into its slot or removes it from the equipment slot if it is used in the equipped state.
        /// It also handles the switches the equipment if there is another item present in the slot.
        /// </summary>
        /// <param name="inv">The inventory calling the use of the equipment item.</param>
        public override void use(Inventory inv)
        {
            // Reference to the equipment slots
            ItemInstance[,] playerEquip = inv.getEquipment();

            // The column of the equipment slot
            int col = equipSlot % 2;

            // The row of the equipment slot
            int row = equipSlot / 2;

            // If no item is in the corresponding equipment slot, add this item
            if (playerEquip[row,col] != null)
            {
                // Place item back in the inventory
                inv.addItem(playerEquip[row, col]);
                // Remove Stats
                Stats playerStats = inv.gameObject.GetComponent<Stats>();
            
                // If the player is not on max stats the percentage of the stat is preserved when equipping or unequipping
                var equippedInst = playerEquip[row, col];
                var equippedData = equippedInst.itemData as Equipment;
                if (equippedData != null)
                {
                    for (var i = 0; i < equippedData.statIncreases.Count; i++)
                    {
                        playerStats.AddToMaxPreserveRatio(i, -equippedData.statIncreases[i]);
                    }
                }
            
                // If the item in the slot is of this type, unequip it / if not add this item
                if (playerEquip[row,col].itemData == this)
                {
                    // Remove item from equipment
                    playerEquip[row, col] = null;
                }
                else
                {   
                    // Remove this item from the inventory
                    inv.removeItem(this);
                    // Place this item in its equipment slot
                    playerEquip[row, col] = new ItemInstance(this, 1);
                
                    // Add stats while preserving the percentages
                    for (int i=0;i< statIncreases.Count;i++)
                    {
                        playerStats.AddToMaxPreserveRatio(i, statIncreases[i]);
                    }
                }
            }
            else
            {
                // Place this item in the empty slot
                playerEquip[row, col] = new ItemInstance(this,1);
                // Remove this item from the inventory
                inv.removeItem(this);
                // Add stats while preserving the percentages
                Stats playerStats = inv.gameObject.GetComponent<Stats>();
                for (int i=0;i< statIncreases.Count;i++)
                {
                    playerStats.AddToMaxPreserveRatio(i, statIncreases[i]);
                }
            }
        }
    }
}