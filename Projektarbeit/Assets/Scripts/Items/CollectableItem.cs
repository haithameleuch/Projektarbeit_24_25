using Helper;
using Inventory;
using UnityEngine;
using Saving;

namespace Items
{
    /// <summary>
    /// This class implements the IInteractable interface and is used for the ability to collect items of the ground.
    /// </summary>
    public class CollectibleItem : MonoBehaviour, IInteractable
    {
        /// <summary>
        /// The scriptable object representing the type of item, which shall be collected.
        /// </summary>
        [SerializeField]
        public Item item;
        
        /// <summary>
        /// The number of items that lay on the ground.
        /// </summary>
        [SerializeField]
        public int amount;
        
        /// <summary>
        /// The index used to track, whether this item was collected or not, to not spawn it again on game load.
        /// </summary>
        public int saveIndex;

        /// <summary>
        /// The method is used to set the collectible item to a given item.
        /// </summary>
        /// <param name="itemToSet">Scriptable object of the item, which shall be collected.</param>
        public void Initialize(Item itemToSet)
        {
            this.item = itemToSet;
        }
    
        /// <summary>
        /// Interact method from the IInteractable interface.
        /// Uses the interaction event to add the item to the inventory and deactivate the game object.
        /// </summary>
        /// <param name="interactor">The object triggering the interaction with the game object that this script is attached to. Needs an inventory script to function.</param>
        public void Interact(GameObject interactor)
        {
            // Get the inventory component of the interactor (becomes null if none is present)
            Inventory.Inventory inv = interactor.GetComponent<Inventory.Inventory>();
        
            // If an inventory is found
            if (inv is not null)
            {
                // If the item could be added to the inventory
                if (inv.AddItem(new ItemInstance(item,amount)))
                {
                    // Remove this item from the list of items that can be collected
                    SaveSystemManager.SetCollectibleActive(saveIndex, false);
                    // Set the game object to inactive
                    gameObject.SetActive(false);
                }
            }
        }
    
        /// <summary>
        /// Inherited method of the IInteractable interface.
        /// (Not used Here)
        /// </summary>
        /// <param name="interactor">Game object that triggered the interaction.</param>
        public void OnExit(GameObject interactor)
        {
            // Only if needed!
        }
    
        /// <summary>
        /// This method is used for setting a flag whether the interaction should be repeated multiple times.
        /// </summary>
        /// <returns>Always returns false, since the collection of an item should only be done once.</returns>
        public bool ShouldRepeat()
        {
            return false;
        }
    }
}