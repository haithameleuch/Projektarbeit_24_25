using UnityEngine;

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