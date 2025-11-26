using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Database/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDataEntry> itemList = new List<ItemDataEntry>();
    public List<NullDataEntry> nullList = new List<NullDataEntry>();
}

[System.Serializable]
public struct ItemDataEntry
{
    public int itemID;
    public ItemData itemData;

    public ItemDataEntry(int id, ItemData item) 
    {
        itemID = id;
        itemData = item;
    }
}

[System.Serializable]
public struct NullDataEntry
{
    public string className;
    public ItemData nullItemData;
}

[System.Serializable]
public class ItemExportEntry
{
    public int itemID;
    public string itemName;
    public int iconID;

    public ItemExportEntry(int itemID, string itemName, int iconID)
    {
        this.itemID = itemID;
        this.itemName = itemName;
        this.iconID = iconID;
    }
}

[System.Serializable]
public class ItemDatabaseExport
{
    public List<ItemExportEntry> items;

    public ItemDatabaseExport(List<ItemExportEntry> items)
    {
        this.items = items;
    }
}
