using UnityEngine;
using System.Collections.Generic;

public enum ToolType
{
    None,
    Pickaxe,
    Sword,
    // … other tools
}

/// <summary>
/// Data-Class representing the equipment type as a sub-class of item
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
    // 5 : Leaft_Hand
    [SerializeField]
    [Range(0, 5)]
    public int equip_slot = 0;

    // List with all the stat increases the equipment should provide
    // Example: The helmet provides 10 Health, 0 Damage and -2 Speed, the list would look like this (10,0,-2)
    [SerializeField]
    public List<int> stat_increases = new List<int>();
    
    [SerializeField]
    public ToolType toolType = ToolType.None;

    // This method brings the functionality to the item
    public override void use(Inventory inv)
    {
        // Reference to the equipmentslots
        ItemInstance[,] player_equip = inv.getEquipment();

        // The column of the equipment slot
        int col = equip_slot % 2;

        // The row of the equipment slot
        int row = equip_slot / 2;

        // If no item is in the corresponding equipment slot, add this item
        if (player_equip[row,col] != null)
        {
            // Place item back in the inventory
            inv.addItem(player_equip[row, col]);
            // Remove Stats
            Stats playerStats = inv.gameObject.GetComponent<Stats>();
            for (int i=0;i< stat_increases.Count;i++)
            {
                playerStats.DecreaseMaxStat(i,stat_increases[i]);
            }
            
            // If the item in the slot is of this type, unequip it / if not add this item
            if (player_equip[row,col].itemData == this)
            {
                // Remove item from equipment
                player_equip[row, col] = null;
            }
            else
            {   
                // Remove this item from the inventory
                inv.removeItem(this);
                // Place this item in its equipment slot
                player_equip[row, col] = new ItemInstance(this, 1);
                
                // Add Stats
                for (int i=0;i< stat_increases.Count;i++)
                {
                    playerStats.IncreaseMaxStat(i,stat_increases[i]);
                    playerStats.IncreaseCurStat(i,stat_increases[i]);
                }
            }
        }
        else
        {
            // Place this item in the empty slot
            player_equip[row, col] = new ItemInstance(this,1);
            // Remove this item from the inventory
            inv.removeItem(this);
            // Add Stats
            Stats playerStats = inv.gameObject.GetComponent<Stats>();
            for (int i=0;i< stat_increases.Count;i++)
            {
                playerStats.IncreaseMaxStat(i,stat_increases[i]);
                playerStats.IncreaseCurStat(i,stat_increases[i]);
            }
        }
    }
}
