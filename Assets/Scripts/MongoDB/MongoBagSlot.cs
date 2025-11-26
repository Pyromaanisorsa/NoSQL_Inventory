using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MongoBagSlot
{
    public int bagItemID { get; set; }
    public MongoInventorySlot[] bagInventory { get; set; }

    public MongoBagSlot() 
    {
        bagItemID = 0;
        bagInventory = new MongoInventorySlot[0];
    }

    // Create MongoBagSlot copy of BagSlot
    public MongoBagSlot(BagSlot origin) 
    {
        bagItemID = origin.bagItem.ItemID;
        bagInventory = new MongoInventorySlot[origin.bagInventory.Length];

        for (int i = 0; i < origin.bagInventory.Length; i++)
            bagInventory[i] = new MongoInventorySlot(origin.bagInventory[i].item.ItemID, origin.bagInventory[i].currentStack);
    }

    // Create new MongoBagSlot and "move" items from old bag
    public MongoBagSlot(InventorySlot[] equippedBag, BagData bagItem)
    {
        bagItemID = bagItem.ItemID;
        bagInventory = new MongoInventorySlot[bagItem.BagSize];

        for (int i = 0; i < bagInventory.Length; i++) 
        {
            bagInventory[i] = new MongoInventorySlot();
        }

        // "Move" all items from equippedBag to new Bag
        int count = 0;
        for (int i = 0; i < equippedBag.Length; i++) 
        {
            if(!equippedBag[i].IsSlotNull())
            {
                bagInventory[count] = new MongoInventorySlot(equippedBag[i].item.ItemID, equippedBag[i].currentStack);
                count++;
            }

        }
    }
}
