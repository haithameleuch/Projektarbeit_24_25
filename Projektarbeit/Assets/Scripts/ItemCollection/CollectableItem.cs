using UnityEngine;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    public string itemName;
    public Sprite itemIcon;
    public int itemQuantity;

    public void Interact(GameObject interactor)
    {
        //füge hier das item zum inventory hinzu
        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv != null)
        {
            if (inv.AddItem(new Item(itemName, itemIcon, itemQuantity)))
            {
                Destroy(gameObject);
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