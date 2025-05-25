using UnityEngine;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    public ItemInstance item;

    public void Initialize(ItemInstance item)
    {
        this.item = item;
    }
    public void Interact(GameObject interactor)
    {
        //füge hier das item zum inventory hinzu
        InventoryV2 inv = interactor.GetComponent<InventoryV2>();
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