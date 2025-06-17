using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class Inventory_V3 : MonoBehaviour
{
    [SerializeField]
    private ItemStack[,] inventory = new ItemStack[4,5];
    [SerializeField]
    private ItemStack[,] equipment = new ItemStack[3,2];

    public bool addItem(ItemStack toAdd)
    {
        if (toAdd.amount<1)
        {
            return false;
        }

        for (int i = 0; i < inventory.GetLength(0); i++)
        {
            for (int j = 0; j < inventory.GetLength(1); j++)
            {
                if (inventory[i, j] != null) {
                    if (toAdd.item == inventory[i, j].item)
                    {
                        inventory[i, j].amount += toAdd.amount;
                        return true;
                    }
                }
            }
        }

        for(int i = 0;i < inventory.GetLength(0); i++)
        {
            for(int j = 0;j < inventory.GetLength(1); j++)
            {
                if (inventory[i, j] == null)
                {
                    inventory[i, j] = toAdd;
                    return true;
                }
            }
        }

        return false;
    }

    public bool removeItem(int row, int col)
    {
        if (row != -1)
        {
            if (inventory[row,col] != null)
            {
                inventory[row, col].amount -= 1;
                if (inventory[row, col].amount < 1)
                {
                    inventory[row, col] = null;              
                }
                return true;
            }
        }
        return false;
    }

    public bool removeItem(Item toRemove)
    {
        for (int i = 0; i < inventory.GetLength(0); i++)
        {
            for (int j = 0; j < inventory.GetLength(1); j++)
            {
                if (inventory[i,j] != null) {
                    if (inventory[i, j].item == toRemove)
                    {
                        inventory[i, j].amount--;
                        if (inventory[i, j].amount < 1)
                        {
                            inventory[i, j] = null;
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // DEBUG: show the inventory to the console
    public void PrintInventory()
    {
        Debug.Log("Inventar:");
        foreach (var item in inventory)
        {
            Debug.Log($"{item.item._name} - Menge: {item.amount}");
        }
    }

    public ItemStack[,] getInventory() { return inventory; }
    public ItemStack[,] getEquipment() { return equipment; }

    public (int,int) getItemByName(string name)
    {
        for (int i = 0; i < inventory.GetLength(0); i++)
        {
            for (int j = 0; j < inventory.GetLength(1); j++)
            {
                if (inventory[i, j].item._name == name)
                {
                    return (i,j);
                }
            }
        }
        return (-1,-1);
    }

    public bool removeEquip(int row, int col)
    {
        if (row != -1)
        {
            if (equipment[row, col] != null)
            {
                equipment[row, col].amount -= 1;
                if (equipment[row, col].amount < 1)
                {
                    addItem(equipment[row, col]);
                    equipment[row, col] = null;
                }
                return true;
            }
        }
        return false;
    }

    public ItemStack getItemByIndex(int row, int col)
    {
        return inventory[row, col];
    }

    public void useItem(int row, int col)
    {
        ItemStack item;
        if (row<4)
        {
            item = inventory[row, col];
        }
        else
        {
            item = equipment[row-4,col];
        }
        item.item.use(this);
    }
}
