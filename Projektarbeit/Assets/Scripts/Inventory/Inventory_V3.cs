using UnityEngine;

public class Inventory_V3 : MonoBehaviour
{
    [SerializeField]
    private Item[,] inventory = new Item[4,5];
    [SerializeField]
    private Item[,] equipment = new Item[3,2];

    public bool addItem(Item toAdd)
    {
        if (toAdd.item_quantity<1)
        {
            return false;
        }

        for (int i = 0; i < inventory.GetLength(0); i++)
        {
            for (int j = 0; j < inventory.GetLength(1); j++)
            {
                if (inventory[i, j] != null) {
                    if (toAdd._name == inventory[i, j]._name)
                    {
                        inventory[i, j].item_quantity += toAdd.item_quantity;
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
                inventory[row, col].item_quantity -= 1;
                if (inventory[row, col].item_quantity < 1)
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
                if (inventory[i, j] == toRemove)
                {
                    inventory[i, j] = null;     
                    return true;
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
            Debug.Log($"{item._name} - Menge: {item.item_quantity}");
        }
    }

    public Item[,] getInventory() { return inventory; }
    public Item[,] getEquipment() { return equipment; }

    public (int,int) getItemByName(string name)
    {
        for (int i = 0; i < inventory.GetLength(0); i++)
        {
            for (int j = 0; j < inventory.GetLength(1); j++)
            {
                if (inventory[i, j]._name == name)
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
                equipment[row, col].item_quantity -= 1;
                if (equipment[row, col].item_quantity < 1)
                {
                    addItem(equipment[row, col]);
                    equipment[row, col] = null;
                }
                return true;
            }
        }
        return false;
    }

    public Item getItemByIndex(int row, int col)
    {
        return inventory[row, col];
    }

    public void useItem(int row, int col)
    {
        Item item;
        if (row<4)
        {
            item = inventory[row, col];
        }
        else
        {
            item = equipment[row-4,col];
        }
        item.use(this);
    }
}
