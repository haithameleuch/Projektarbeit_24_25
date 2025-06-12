using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Equipment", menuName = "Scriptable Objects/Equipment")]
public class Equipment : Item
{
    [SerializeField]
    public int equip_slot = 0;

    [SerializeField]
    public List<int> stat_increases = new List<int>();

    public bool is_equipped = false;

    public override void use(Inventory_V3 inv)
    {
        Item[,] player_equip = inv.getEquipment();
        int row = equip_slot % 2;
        int col = equip_slot / 2;
        if (is_equipped)
        {
            Debug.Log(this._name+" was unequipped.");
            is_equipped = false;

            if (inv.addItem(this))
            {
                player_equip[row,col]= null;
            }
        }
        else
        {
            Debug.Log(this._name + " was equipped.");
            is_equipped = true;
            if (player_equip[row, col] == null)
            {
                inv.removeItem(this);
                player_equip[row, col] = this;
            }
            else
            {
                if (inv.addItem(player_equip[row,col]))
                {
                    inv.removeItem(this);
                    player_equip[row, col] = this;
                }
            }
        }
    }
}
