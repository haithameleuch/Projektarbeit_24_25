using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Data Class for interaction between the different parts of the inventory-system
/// </summary>

[System.Serializable]
public class ItemInstance
{
    public Item itemData;
    [Range(0,100)]
    public int itemQuantity = 1;

    public ItemInstance(Item itemData,int amount)
    {
        this.itemData = itemData; 
        this.itemQuantity = amount;
    }
    public ItemInstance(string name, GameObject spawnedObject, float probability, Sprite icon, int quantity)
    {
        itemData._name = name;
        itemData._model = spawnedObject;
        itemData.rarity = probability;
        itemData.item_icon = icon;
        itemQuantity = quantity;
    }

    public ItemInstance(ItemInstance item)
    {
        this.itemData = item.itemData;
        this.itemQuantity = item.itemQuantity;
    }

    public ItemInstance()
    {
    }
}