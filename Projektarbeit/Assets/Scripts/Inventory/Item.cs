using UnityEngine;

/// <summary>
/// Data Class for interaction between the different parts of the inventory-system
/// </summary>
public class Item
{
    public string itemName;
    public Sprite itemIcon;
    public int itemQuantity;

    public Item(string name, Sprite icon, int quantity)
    {
        itemName = name;
        itemIcon = icon;
        itemQuantity = quantity;
    }

}