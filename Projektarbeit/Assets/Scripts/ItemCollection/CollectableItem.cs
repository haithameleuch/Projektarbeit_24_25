using UnityEngine;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    [SerializeField]
    public Item item;
    [SerializeField]
    public int amount;

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
            if (inv.addItem(new ItemStack(item,amount)))
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