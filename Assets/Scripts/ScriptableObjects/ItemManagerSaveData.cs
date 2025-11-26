using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemManagerSaveData")]
public class ItemManagerSaveData : ScriptableObject
{
    public int nextID;
    public List<int> droppedID;

    public int GetNextItemID() 
    {
        if (droppedID.Count > 0)
            return droppedID[0];
        return nextID;
    }
}