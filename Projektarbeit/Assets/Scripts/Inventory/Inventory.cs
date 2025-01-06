using System.Collections.Generic;
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

    // Remove item from Inventory
    public void RemoveItem(Item item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            Debug.Log($"Item {item.itemName} entfernt.");
        }
        else
        {
            Debug.Log("Item nicht im Inventar gefunden.");
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
}