using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

/// <summary>
/// Main inventory class, this class does the administration of the item system.
/// The class saves the items in a list and is the point of interaction for Items.
/// Possible for player and enemys/chests.
/// </summary>
public class Inventory : MonoBehaviour
{
    /// <summary>
    /// The main part of the inventory, the list contains all items present in the inventory, in the order they were added.
    /// </summary>
    [SerializeField]
    private List<Item> items = new List<Item>();
    
    /// <summary>
    /// Maximum number of different items the inventory is allowed to hold.
    /// </summary>
    private int maxSlots = 16;

    /// <summary>
    /// This method adds an item to the inventory either by adding it to a stack of already present items, or to a new stack if the space is available.
    /// </summary>
    /// <param name="newItem">Is the object of the item class that should be added to the inventory.</param>
    /// <returns>The method returns a boolean wether the addition of the item was succesful. This should be used incase something should happen regarding the result of the addition.</returns>
    public bool AddItem(Item newItem)
    {
        if (newItem.itemQuantity<=0)
        {
            return false;
        }
        // Add Item to stack if the item is already in the Inventory and return true
        foreach (Item item in items)
        {
            if (item.itemName==newItem.itemName)
            {
                item.itemQuantity += newItem.itemQuantity;
                return true;
            }
        }

        // If the inventory is ful stop and return false, else add the item to the inventory and return true
        if (items.Count < maxSlots)
        {
            items.Add(newItem);
            return true;
        }
        else
        {
            Debug.Log("Inventar ist voll!");
            return false;
        }
    }

    /// <summary>
    /// Removes a certain amount of an item  from the inventory. //TODO spawn thrown away items
    /// </summary>
    /// <param name="item">The item that should be removed.</param>
    /// <param name="amount">The amount of the item that should be removed</param>
    public void RemoveItem(Item toRemove,int amount)
    {
        foreach (var item in items)
        {
            if (item.Equals(toRemove))
            {
                item.itemQuantity -= amount;
                if (item.itemQuantity <= 0)
                {
                    items.Remove(item);  
                }
                return;
            }
        }
    }

    // DEBUG: show the inventory to the console
    public void PrintInventory()
    {
        Debug.Log("Inventar:");
        foreach (var item in items)
        {
            Debug.Log($"{item.itemName} - Menge: {item.itemQuantity}");
        }
    }

    /// <summary>
    /// Getter-method for the inventory
    /// </summary>
    /// <returns>The list of items.</returns>
    public List<Item> getInventory()
    {
        return items; 
    }

    /// <summary>
    /// Method searches for an item in the inventory by the name.
    /// </summary>
    /// <param name="itemName">The name of the item you search for.</param>
    /// <returns>The item from the list.</returns>
    public Item GetItem(string itemName)
    {
        foreach(var item in items)
        {
            if (item.itemName.Equals(itemName))
            {
                return item;
            }
        }
        return new Item("",null,0);
    }
}