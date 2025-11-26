using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class MongoInventory
{
    public ObjectId Id { get; set; }
    public MongoBagSlot[] bags;
    public MongoWeaponSlot mainHandWeapon;

    public MongoInventory()
    {
        bags = new MongoBagSlot[5];
        mainHandWeapon = new MongoWeaponSlot();
    }

    public MongoInventory(ObjectId newId)
    {
        Id = newId;
    }

    // Convert inventory data to mongoInventory format
    public MongoInventory(Inventory inventory)
    {
        bags = inventory.bags.Select(bagSlot => new MongoBagSlot
        {
            bagItemID = bagSlot.bagItem.ItemID,
            bagInventory = bagSlot.bagInventory.Select(slot => new MongoInventorySlot
            {
                itemID = slot.item.ItemID,
                currentStack = slot.currentStack
            }).ToArray()
        }).ToArray();
        mainHandWeapon = new MongoWeaponSlot
        {
            weaponID = inventory.mainHandWeapon.weapon.ItemID
        };
    }
}
