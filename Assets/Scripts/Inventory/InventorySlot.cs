using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public int currentStack;
    public ItemData item;

    public InventorySlot()
    {
        currentStack = 0;
        item = ItemDataManager.Instance.GetNullData<ItemData>();
    }

    // Used for NULL items or non-stackable items
    public InventorySlot(ItemData item)
    {
        if (item.ItemID == 0)
            currentStack = 0;
        else
            currentStack = 1;
        this.item = item;
    }

    // Used for stackable items
    public InventorySlot(int stack, ItemData item)
    {
        currentStack = stack;
        this.item = item;
    }

    // Used for duplicating slots (DEEP COPY)
    public InventorySlot(InventorySlot slot)
    {
        currentStack = slot.currentStack;
        item = slot.item;
    }

    // Check if item's currentStack is full
    public bool IsSlotStackFull() 
    {
        if (currentStack >= item.MaxStack)
            return true;
        return false;
    }

    // Check if item is nullItem / itemId is 0
    public bool IsSlotNull() 
    {
        if (item.ItemID == 0)
            return true;
        return false;
    }

    // Check if item is Stackable
    public bool IsSlotStackable()
    {
        if (item.ItemID == 0)
            return false;
        if (item.Stackable)
            return true;
        return false;
    }
}
