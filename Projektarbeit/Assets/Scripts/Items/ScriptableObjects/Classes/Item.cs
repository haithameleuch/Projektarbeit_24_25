using NUnit.Framework.Interfaces;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public abstract class Item : ScriptableObject
{
    [SerializeField]
    public string _name = "";
    [SerializeField]
    public GameObject _model;
    [SerializeField]
    [Range(0, 100)]
    public float rarity = 50.0f;
    [SerializeField]
    public Sprite item_icon = null;

    public Color32 getRarityColor()
    {
        if (this.rarity < 25)
        {
            return new Color32(255, 255, 0, 100);
        }
        else if (this.rarity < 50)
        {
            return new Color32(255, 0, 255, 100);
        }
        else if (this.rarity < 75)
        {
            return new Color32(0, 0, 255, 100);
        }
        else
        {
            return new Color32(0, 255, 0, 100);
        }
    }

    public abstract void use(Inventory inv);

}
