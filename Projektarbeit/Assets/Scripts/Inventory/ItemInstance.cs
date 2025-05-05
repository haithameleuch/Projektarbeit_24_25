using Spawning;
using UnityEngine;

/// <summary>
/// Data Class for interaction between the different parts of the inventory-system
/// </summary>

[CreateAssetMenu(fileName = "Item", menuName = "Item")]
public class Item : ScriptableObject, IInteractable
{
    public int itemQuantity { get; set; }
    public SpawnableData itemData;

    public Item(){}
    public Item(string name, Sprite icon, int quantity, GameObject spawnedObject, float probability)
    {
        itemData.spawnName = name;
        itemData.sprite = icon;
        itemQuantity = quantity;
        itemData.spawnObject = spawnedObject;
        itemData.spawnProbability = probability;
    }

    public Item(SpawnableData data, Sprite icon, int quantity)
    {
        itemData = data;
        itemIcon = icon;
        itemQuantity = quantity;
    }
    
    public void Interact(GameObject interactor)
    {
        //füge hier das item zum inventory hinzu
        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv != null)
        {
            if (inv.AddItem(new Item(itemData, itemIcon, itemQuantity)))
            {
                Destroy(itemData.spawnObject);
            }
        }

    }
    
    public void OnExit(GameObject interactor)
    {
        // Only if needed!
    }

    public bool ShouldRepeat()
    {
        return false;
    }
}