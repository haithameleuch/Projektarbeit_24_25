using UnityEngine;

[CreateAssetMenu(fileName = "Consumable", menuName = "Scriptable Objects/Consumable")]
public class Consumable : Item
{
    [SerializeField]
    public int stat_to_restore = 0;

    [SerializeField]
    public float amount_to_restore = 0;

    public override void use(Inventory inv)
    {
        Debug.Log(this._name + " was used.");
    }
}
