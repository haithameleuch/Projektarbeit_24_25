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
                    if (toAdd.itemData.name == inventory[i, j].itemData.name)
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

    public bool removeItem(ItemInstance toRemove)
    {
        for (int i = 0; i < inventory.GetLength(0); i++)
        {
            for (int j = 0; j < inventory.GetLength(1); j++)
            {
                if (inventory[i, j] == toRemove)
                {
                    inventory[i, j].itemQuantity -= 1;
                    if (inventory[i, j].itemQuantity<1)
                    {
                        inventory[i, j] = null;     
                    }
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
            Debug.Log($"{item.itemData.spawnName} - Menge: {item.itemQuantity}");
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
                if (inventory[i, j].itemData.name == name)
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
                    equipment[row, col] = null;
                }
                return true;
            }
        }
        return false;
    }
}
