using System;
using UnityEngine;
using Saving;

public class Inventory : MonoBehaviour
{
    // 2D-Array of items (scriptable objects and quantities) for the inventory
    [SerializeField]
    private ItemInstance[,] _inventory = new ItemInstance[4,5];
    
    // 2D-Array of items (scriptable objects and quantities) for the equipment
    [SerializeField]
    private ItemInstance[,] _equipment = new ItemInstance[3,2];

    /// <summary>
    /// The method loads the state of the inventory and equipment from the SaveSystemManager.
    /// </summary>
    private void Awake()
    {
        _inventory = SaveSystemManager.GetInventory();
        _equipment = SaveSystemManager.GetEquipment();
    }

    /// <summary>
    /// This method adds an item to the inventory by adding it to an existing stack (if possible) or creating a new one (if necessary and possible).
    /// </summary>
    /// <param name="toAdd">The item instance that should be added.</param>
    /// <returns>"True" if the item could be added and "false" if anything went wrong.</returns>
    public bool AddItem(ItemInstance toAdd)
    {
        // If the quantity of the item that shall be added is less than 1 it will not be added (should not happen in practice)
        if (toAdd.itemQuantity<1)
        {
            return false;
        }

        // Search all slots if the item that shall be added is already present.
        // If yes add the item if not continue
        for (int i = 0; i < _inventory.GetLength(0); i++)
        {
            for (int j = 0; j < _inventory.GetLength(1); j++)
            {
                // If the item instance is null the slot is empty
                if (_inventory[i, j] != null) {
                    
                    // If the name is the same it should be the same item
                    if (toAdd.itemData == _inventory[i, j].itemData)
                    {
                        // Add the quantity of the item that shall be added to the existing item stack
                        _inventory[i, j].itemQuantity += toAdd.itemQuantity;
                        return true;
                    }
                }
            }
        }

        // If the item that shall be added is nor present, go through all slots until one is empty, than add the item to this slot
        for(int i = 0;i < _inventory.GetLength(0); i++)
        {
            for(int j = 0;j < _inventory.GetLength(1); j++)
            {
                // If the slot is empty
                if (_inventory[i, j] == null)
                {
                    _inventory[i, j] = toAdd;
                    return true;
                }
            }
        }

        // If the item is neither contained in the inventory nor any free space is found return "false"
        return false;
    }

    /// <summary>
    /// This method removes one item from a stack and if the stack drops down to 0 or belo zero it removes the stack.
    /// </summary>
    /// <param name="row">The row in which the item is located.</param>
    /// <param name="col">The column in which the item is located.</param>
    /// <returns>"True" if the item was found and removed. If not return "false".</returns>
    public bool removeItem(int row, int col)
    {
        // If no slot is selected in the inventory ui the row and column are -1.
        // Than the method should not remove anything.
        if (row != -1)
        {
            // Check wether there is an item in the selected slot
            if (_inventory[row,col] != null)
            {
                // Removes one item by decreasing the item quantity by 1
                _inventory[row, col].itemQuantity -= 1;
                // If the quantity drops to or below 0 remove the stack(set it to null)
                if (_inventory[row, col].itemQuantity < 1)
                {
                    _inventory[row, col] = null;              
                }
                return true;
            }
        }
        // If no item was selected, or it was not found return "false"
        return false;
    }

    /// <summary>
    /// Removes an item from an item stack and the stack if the quantity drops below 0.
    /// </summary>
    /// <param name="toRemove">The scriptable object of the item that shall be removed.</param>
    /// <returns>"True" if the removal was successful, if not return "false".</returns>
    public bool removeItem(Item toRemove)
    {
        // Iterate through the entire inventory
        for (int i = 0; i < _inventory.GetLength(0); i++)
        {
            for (int j = 0; j < _inventory.GetLength(1); j++)
            {
                // If an item is located in this slot
                if (_inventory[i,j] != null) {
                    
                    // If the item matches the description of the item that shall be removed
                    if (_inventory[i, j].itemData == toRemove)
                    {
                        // Decrease the item quantity by 1
                        _inventory[i, j].itemQuantity--;
                        
                        // If the quantity drops to or below 0 remove the entire stack from the inventory
                        if (_inventory[i, j].itemQuantity < 1)
                        {
                            _inventory[i, j] = null;
                        }
                        return true;
                    }
                }
            }
        }
        // If no item was found and removed return "false"
        return false;
    }

    /// <summary>
    /// This method prints the Inventory to the Unity console.
    /// ONLY FOR DEBUG PURPOSES!
    /// </summary>
    public void PrintInventory()
    {
        Debug.Log("Inventory:");
        // Loops over all item instances in the inventory and prints their name and quantity
        foreach (var item in _inventory)
        {
            Debug.Log($"{item.itemData._name} - Quantity: {item.itemQuantity}");
        }
    }

    /// <summary>
    /// This method fills the inventory with items.
    /// </summary>
    /// <param name="item">A 2D-Array of Item instances that should have the same shape as the inventory. (Meaning [4,5])</param>
    public void SetInventory(ItemInstance[,] item)
    {
        // Makes a deep copy of the parameter into the _inventory variable
        Array.Copy(item,_inventory,_inventory.Length);
    }

    /// <summary>
    /// This method fills the equipment slots with items.
    /// </summary>
    /// <param name="equip">A 2D-Array of Item instances that should have the same shape as the equipment. (Meaning [3,2])</param>
    public void SetEquipment(ItemInstance[,] equip)
    {
        // Makes a deep copy of the parameter into the _equipment variable
        Array.Copy(equip,_equipment,equip.Length);
    }
    
    /// <summary>
    /// Getter-method for the inventory.
    /// </summary>
    /// <returns>A 2D-Array of item instances filled with items or null if the slot is empty.</returns>
    public ItemInstance[,] getInventory() { return _inventory; }
    
    /// <summary>
    /// Getter-method for the equipment.
    /// </summary>
    /// <returns>A 2D-Array of item instances filled with equipped items or null if the slot is empty.</returns>
    public ItemInstance[,] getEquipment() { return _equipment; }

    /// <summary>
    /// This method finds an item in the inventory with the given name.
    /// </summary>
    /// <param name="name">The name of the item. (Meaning the name inside the scriptable object, not the name of the scriptable object)</param>
    /// <returns>A tuple of integers, where the first item in the tuple is the row and the second is the column.</returns>
    public (int,int) getItemByName(string name)
    {
        // Iterate over the entire inventory
        for (int i = 0; i < _inventory.GetLength(0); i++)
        {
            for (int j = 0; j < _inventory.GetLength(1); j++)
            {
                // If an item is located in this slot
                if (_inventory[i, j] != null)
                {
                    // If the item name matches the name that is searched for return the slot as a tuple of (int row, int col)
                    if (_inventory[i, j].itemData._name == name)
                    {
                        return (i,j);
                    }
                }
            }
        }
        // If the item is not found return -1,-1
        return (-1,-1);
    }

    /// <summary>
    /// This method removes an equipped item and places it back in the inventory.
    /// </summary>
    /// <param name="row">The row in the equipment slots, where we want to remove the item.</param>
    /// <param name="col">The column in the equipment slots, where we want to remove the item.</param>
    /// <returns>"True" if the removal was successful and "false" if not.</returns>
    public bool removeEquip(int row, int col)
    {
        // If a valid item slot is selected
        if (row != -1)
        {
            // If there is an item in the slot
            if (_equipment[row, col] != null)
            {
                // Decrease the quantity by 1
                _equipment[row, col].itemQuantity -= 1;
                // If the quantity drops to or below zero remove the item from the equipment and add it back to the inventory
                if (_equipment[row, col].itemQuantity < 1)
                {
                    AddItem(_equipment[row, col]);
                    _equipment[row, col] = null;
                }
                return true;
            }
        }
        // If no item was found
        return false;
    }

    /// <summary>
    /// This method gets an item instance by the indices of the row and column.
    /// </summary>
    /// <param name="row">The row, where the item is located.</param>
    /// <param name="col">The column, where the item is located.</param>
    /// <returns>The item instance, that is present in the searched slot.</returns>
    public ItemInstance GetItemByIndex(int row, int col)
    {
        return _inventory[row, col];
    }

    /// <summary>
    /// This method calls the use-method of the item in a given slot to execute its functionality.
    /// </summary>
    /// <param name="row">The row of the item that shall be used.</param>
    /// <param name="col">The column of the item that shall be used.</param>
    public void useItem(int row, int col)
    {
        // Create reference of the item
        ItemInstance item;
        // If the row is greater than 4 the slot in question is from the equipment
        if (row<4)
        {
            // Set the reference from the inventory
            item = _inventory[row, col];
        }
        else
        {
            // Set the reference from the equipment
            item = _equipment[row-4,col];
        }
        // Call the use method of the item. All functionality is handled locally in the item class.
        item.itemData.use(this);
    }
}
