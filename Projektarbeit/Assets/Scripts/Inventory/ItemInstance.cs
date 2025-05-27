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
    // public SpawnableData itemData;

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

    public ItemInstance()
    {
    }

    public Color32 getRarityColor()
    {
        if (itemData.spawnProbability < 25)
        {
            return new Color32(255,255,0,100);
        }
        else if (itemData.spawnProbability < 50)
        {
            return new Color32(255,0,255,100);
        }
        else if(itemData.spawnProbability < 75)
        {
            return new Color32(0,0,255,100);
        }
        else
        {
            return new Color32(0,255,0,100);
        }
    }
}