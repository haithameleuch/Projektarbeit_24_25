using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<Item> items = new List<Item>();
    public int maxSlots = 16;

    public bool AddItem(Item newItem)
    {
        // Add Item to stack if the item is already in the Inventory
        foreach (Item item in items)
        {
            if (item.itemName==newItem.itemName)
            {
                item.itemQuantity += newItem.itemQuantity;
                Debug.Log(item.itemQuantity);
                return true;
            }
        }


        // Check if the inventory is full
        if (items.Count < maxSlots)
        {
            // Add item to inventory and return true
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
}