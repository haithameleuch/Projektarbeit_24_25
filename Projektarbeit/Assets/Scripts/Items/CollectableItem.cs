using UnityEngine;
using Saving;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    [SerializeField]
    public Item item;
    [SerializeField]
    public int amount;
    public int saveIndex;

    public void Initialize(Item item)
    {
        this.item = item;
    }
    
    public void Interact(GameObject interactor)
    {
        Inventory inv = interactor.GetComponent<Inventory>();
        
        if (inv is not null)
        {
            if (inv.addItem(new ItemInstance(item,amount)))
            {
                SaveSystemManager.SetCollectibleActive(saveIndex, false);
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