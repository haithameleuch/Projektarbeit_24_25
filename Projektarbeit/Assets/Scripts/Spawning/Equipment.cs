using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Equipment", menuName = "Scriptable Objects/Equipment")]
public class Equipment : Item
{
    [SerializeField]
    [Range(0, 5)]
    public int equip_slot = 0;

    [SerializeField]
    public List<int> stat_increases = new List<int>();

    public override void use(Inventory_V3 inv)
    {
        ItemStack[,] player_equip = inv.getEquipment();
        int col = equip_slot % 2;
        int row = equip_slot / 2;
        if (player_equip[row,col] != null)
        {
            if (player_equip[row,col].item == this)
            {
                inv.addItem(player_equip[row, col]);
                player_equip[row, col] = null;
            }
            else
            {
                inv.addItem(player_equip[row, col]);
                inv.removeItem(this);
                player_equip[row, col] = new ItemStack(this, 1);
            }
        }
        else
        {
            player_equip[row, col] = new ItemStack(this,1);
            inv.removeItem(this);
        }
    }
}
