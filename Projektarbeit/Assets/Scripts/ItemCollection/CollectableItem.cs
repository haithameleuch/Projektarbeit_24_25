using UnityEngine;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    public Item item;

    public void Initialize(Item item)
    {
        this.item = item;
    }
    public void Interact(GameObject interactor)
    {
        //füge hier das item zum inventory hinzu
        Inventory_V3 inv = interactor.GetComponent<Inventory_V3>();
        if (inv != null)
        {
            if (inv.addItem(item))
            {
                Renderer rend = GetComponent<Renderer>();
                Collider collider = GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                if (rend != null) rend.enabled = false;
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