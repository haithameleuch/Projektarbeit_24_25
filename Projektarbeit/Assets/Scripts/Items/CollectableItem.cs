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
        Inventory inv = interactor.GetComponent<Inventory>();
        if (inv != null)
        {
            if (inv.addItem(new ItemInstance(item,amount)))
            {
                gameObject.SetActive(false);
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