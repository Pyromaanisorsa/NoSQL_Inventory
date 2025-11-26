using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MongoInventorySlot
{
    public int itemID;
    public int currentStack;

    public MongoInventorySlot() 
    {
        itemID = 0;
        currentStack = 0;
    }

    public MongoInventorySlot(int id)
    {
        itemID = id;
        currentStack = 1;
    }

    public MongoInventorySlot(int id, int stack)
    {
        itemID = id;
        currentStack = stack;
    }
}
