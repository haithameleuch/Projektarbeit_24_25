using UnityEngine;

// Data-Class for keeping track of the item quantities
public class ItemStack 
{
    public Item item;
    public int amount;

    public ItemStack(Item pItem, int pAmount)
    {
        this.item = pItem;
        this.amount = pAmount;
    }
}
