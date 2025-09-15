using Items;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// Data-Class representing an item stack by containing an reference to a scriptable object(item) and the quantity of that item.
    /// </summary>

    [System.Serializable]
    public class ItemInstance
    {
        /// <summary>
        /// The scriptable object of the item that contains all item data (e.g. the name, rarity and icon).
        /// </summary>
        public Item itemData;
    
        /// <summary>
        /// The number of items that is contained in this instance (e.g. there could be 5 potions on the ground)
        /// </summary>
        [Range(0,100)]
        public int itemQuantity = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="itemData">A scriptable object of type item containing the item data.</param>
        /// <param name="amount">An integer containing the number of items.</param>
        public ItemInstance(Item itemData,int amount)
        {
            this.itemData = itemData; 
            this.itemQuantity = amount;
        }
        /// <summary>
        /// Alternative constructor
        /// </summary>
        /// <param name="name">The name of the item, NOT the scriptable object.</param>
        /// <param name="spawnedObject">Reference to a game object that models the item in the world.</param>
        /// <param name="probability">The probability that this item should be spawned.</param>
        /// <param name="icon">The icon that shows up in the inventory.</param>
        /// <param name="quantity">The number of items represented by this instance.</param>
        public ItemInstance(string name, GameObject spawnedObject, float probability, Sprite icon, int quantity)
        {
            itemData._name = name;
            itemData._model = spawnedObject;
            itemData.rarity = probability;
            itemData.item_icon = icon;
            itemQuantity = quantity;
        }

        /// <summary>
        /// Copy-constructor
        /// </summary>
        /// <param name="item">An item instance that shall be copied.</param>
        public ItemInstance(ItemInstance item)
        {
            this.itemData = item.itemData;
            this.itemQuantity = item.itemQuantity;
        }

        /// <summary>
        /// Empty constructor (NOT USED)
        /// </summary>
        public ItemInstance()
        {
        }
    }
}