using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField]
    private ItemInstance[,] inventory = new ItemInstance[4,5];
    [SerializeField]
    private ItemInstance[,] equipment = new ItemInstance[3,2];

    public bool addItem(ItemInstance toAdd)
    {
        if (toAdd.itemQuantity<1)
        {
            return false;
        }

        for (int i = 0; i < inventory.GetLength(0); i++)
        {
            for (int j = 0; j < inventory.GetLength(1); j++)
            {
                if (inventory[i, j] != null) {
                    if (toAdd.itemData == inventory[i, j].itemData)
                    {
                        inventory[i, j].itemQuantity += toAdd.itemQuantity;
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
                inventory[row, col].itemQuantity -= 1;
                if (inventory[row, col].itemQuantity < 1)
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
                    if (inventory[i, j].itemData == toRemove)
                    {
                        inventory[i, j].itemQuantity--;
                        if (inventory[i, j].itemQuantity < 1)
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
            Debug.Log($"{item.itemData._name} - Menge: {item.itemQuantity}");
        }
    }

    public ItemInstance[,] getInventory() { return inventory; }
    public ItemInstance[,] getEquipment() { return equipment; }

    public (int,int) getItemByName(string name)
    {
        for (int i = 0; i < inventory.GetLength(0); i++)
        {
            for (int j = 0; j < inventory.GetLength(1); j++)
            {
                if (inventory[i, j].itemData._name == name)
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
                equipment[row, col].itemQuantity -= 1;
                if (equipment[row, col].itemQuantity < 1)
                {
                    addItem(equipment[row, col]);
                    equipment[row, col] = null;
                }
                return true;
            }
        }
        return false;
    }

    public ItemInstance getItemByIndex(int row, int col)
    {
        return inventory[row, col];
    }

    public void useItem(int row, int col)
    {
        ItemInstance item;
        if (row<4)
        {
            item = inventory[row, col];
        }
        else
        {
            item = equipment[row-4,col];
        }
        item.itemData.use(this);
    }
}
