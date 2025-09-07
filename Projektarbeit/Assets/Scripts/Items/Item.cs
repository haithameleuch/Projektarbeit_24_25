using NUnit.Framework.Interfaces;
using UnityEngine;

/// <summary>
/// Abstract Data-Class, which shares all commonalities between different types of Items 
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public abstract class Item : ScriptableObject
{
    /// <summary>
    /// The name that will be displayed in the inventory (and is maybe used as an ID in some cases)
    /// </summary>
    [SerializeField]
    public string _name = "";
    /// <summary>
    /// Reference to the prefab as which this item should appear in game
    /// </summary>
    [SerializeField]
    public GameObject _model;
    /// <summary>
    /// How rare/ Which quality an item should have. With 0 beeing the rarest and 100 the least rare
    /// </summary>
    [SerializeField]
    [Range(0, 100)]
    public float rarity = 50.0f;
    /// <summary>
    /// The Sprite, which will be displayed in the inventory
    /// </summary>
    [SerializeField]
    public Sprite item_icon = null;

    /// <summary>
    /// The method decides which color the rarity of the item corresponds to
    /// </summary>
    /// <returns>A RGBA-Color with respect to the items rarity</returns>
    public Color32 getRarityColor()
    {
        if (this.rarity < 25)
        {
            // Color Yellow
            return new Color32(255, 255, 0, 100);
        }
        else if (this.rarity < 50)
        {
            // Color Violett
            return new Color32(255, 0, 255, 100);
        }
        else if (this.rarity < 75)
        {
            // Color Blue
            return new Color32(0, 0, 255, 100);
        }
        else
        {
            // Color Green
            return new Color32(0, 255, 0, 100);
        }
    }

    /// <summary>
    /// Abstract method to be implemented by every sub-class
    /// </summary>
    /// <param name="inv">The inventory which calles the method so the interaction can be handled locally</param>
    public abstract void use(Inventory inv);

}
