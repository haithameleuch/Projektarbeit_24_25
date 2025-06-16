using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Data Class for interaction between the different parts of the inventory-system
/// </summary>

[System.Serializable]
public class ItemInstance
{
    public SpawnableData itemData;
    [Range(0,100)]
    public int itemQuantity = 1;

    public ItemInstance(SpawnableData itemData)
    {
        this.itemData = itemData;    
    }
    public ItemInstance(string name, GameObject spawnedObject, float probability, Sprite icon, int quantity)
    {
        itemData.spawnName = name;
        itemData.spawnObject = spawnedObject;
        itemData.spawnProbability = probability;
        itemData.spawnSprite = icon;
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