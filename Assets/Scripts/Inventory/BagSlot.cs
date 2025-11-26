using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BagSlot
{
    public BagData bagItem;
    public InventorySlot[] bagInventory;

    public BagSlot()
    {
        bagItem = null;
        bagInventory = new InventorySlot[0];
    }

    public BagSlot(BagData bagItem)
    {
        this.bagItem = bagItem;
        if (bagItem.BagSize > 0)
        {
            bagInventory = new InventorySlot[bagItem.BagSize];
            ItemData nullData = ItemDataManager.Instance.GetNullData<ItemData>();
            
            for (int i = 0; i < bagInventory.Length; i++)
                bagInventory[i] = new InventorySlot(nullData);
        }
        else
            bagInventory = new InventorySlot[0];
    }

    // Create new BagSlot and "move" items from old bag
    public BagSlot(InventorySlot[] equippedBag, BagData bagItem) 
    {
        this.bagItem = bagItem;
        bagInventory = new InventorySlot[bagItem.BagSize];
        ItemData nullData = ItemDataManager.Instance.GetNullData<ItemData>();

        for (int i = 0; i < bagInventory.Length; i++)
            bagInventory[i] = new InventorySlot(nullData);

        // "Move" all items from equippedBag to new Bag
        int count = 0;
        for (int i = 0; i < equippedBag.Length; i++)
        {
            if(!equippedBag[i].IsSlotNull())
            {
                bagInventory[count] = new InventorySlot(equippedBag[i]);
                count++;
            }
        }
    }

    public bool IsBagNull()
    {
        if (bagItem.ItemID == 0)
            return true;
        return false;
    }

    public bool IsBagEmpty() 
    {
        foreach (InventorySlot slot in bagInventory)
            if (!slot.IsSlotNull())
                return false;
        return true;
    }

    // Return first empty inventory slot in bag (if any)
    public bool GetEmptySlot(out int slotNum)
    {
        slotNum = 0;

        for(int i = 0; i < bagInventory.Length; i++) 
        {
            if (bagInventory[i].IsSlotNull()) 
            {
                slotNum = i;
                return true;
            }
        }
        return false;
    }

    // Return count of emptySlots in the bag
    public int GetBagSpaceCount()
    {
        int count = 0;
        for(int i = 0; i < bagInventory.Length; i++) 
        {
            if (!bagInventory[i].IsSlotNull())
                count++;
        }
        return bagItem.BagSize - count;
    }
}
