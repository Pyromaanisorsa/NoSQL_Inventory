using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

public class ItemDatabaseEditor: EditorWindow
{
    [MenuItem("Tools/Update Item Database")]
    public static void UpdateItemDatabase()
    {
        // Path to ItemDatabase ScriptableObject
        string path = "Assets/Databases/ItemDatabase.asset";
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);

        if (database == null)
        {
            Debug.LogError("ItemDatabase not found at " + path);
            return;
        }

        // Clear the current nullList (not in use atm)
        database.nullList.Clear();

        LoadItems();

        // Save the updated database
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Item Database updated successfully.");

        void LoadItems()
        {
            // Find all ItemData objects in the Resources/Items folder
            ItemData[] items = Resources.LoadAll<ItemData>("Items");

            // Delete all invalid / null entries in ItemDatabase
            database.itemList.RemoveAll(entry => entry.itemData == null || entry.itemData is UnityEngine.Object obj && obj == null);

            // Populate item list
            foreach (ItemData item in items)
            {
                if (!database.itemList.Exists(data => data.itemID == item.ItemID))
                {
                    database.itemList.Add(new ItemDataEntry {itemID = item.ItemID, itemData = item });
                }
                else
                {
                    Debug.LogWarning($"Duplicate ItemID detected: {item.ItemID}");
                }
            }

            // Sort ItemList by ItemID (looks pretty)
            database.itemList.Sort((a,b) => a.itemID.CompareTo(b.itemID));
            //database.itemList.RemoveAt(0);

            // Populate null list
            ItemData[] nulls = Resources.LoadAll<ItemData>("Null_Items");
            foreach (ItemData item in nulls)
            {
                string className = item.GetType().Name;
                if (!database.nullList.Exists(data => data.className == className))
                {
                    database.nullList.Add(new NullDataEntry {className = className, nullItemData = item });
                }
                else
                    Debug.LogError($"Failed to create null instance for {item.name}");
            }
        }
    }

    [MenuItem("Tools/Update Icon Database")]
    public static void UpdateIconDatabase() 
    {
        // Store strings to show what got added
        List<string> messages = new List<string>();

        // Path to IconDatabase ScriptableObject
        string path = "Assets/Databases/IconDatabase.asset";
        IconDatabase database = AssetDatabase.LoadAssetAtPath<IconDatabase>(path);
        List<IconDataEntry> databaseList = database.iconList;

        if (database == null)
        {
            Debug.LogError("IconDatabase not found at " + path);
            return;
        }

        // Load all the icons in Resources/Icons to array
        Sprite[] icons = Resources.LoadAll<Sprite>("Icons");

        // Update icon database list; add any icons that don't exist in database
        foreach(Sprite icon in icons)
        {
            if(!databaseList.Exists(iconEntry => iconEntry.icon == icon))
            {
                databaseList.Add(new IconDataEntry(databaseList.Count, icon));
                messages.Add($"Added icon {icon.name} to index #{databaseList.Count - 1}");
            }
        }

        foreach (string message in messages)
            Debug.Log(message);

        // Save the updated database
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        void LoadIcons()
        {
            // Find all Sprite icons in the Resources/Icons folder
            Sprite[] icons = Resources.LoadAll<Sprite>("Icons");

            // Populate item list
            for (int i = 0; i < icons.Length; i++) 
            {
                database.iconList.Add(new IconDataEntry(i, icons[i]));
            }
        }
    }

    [MenuItem("Tools/Export Item Database as JSON")]
    public static void ExportItemDatabase() 
    {
        // Exports few fields of itemDatabase in this format as JSON
        /* "items": [
        {
            "itemID": -1,
            "itemName": "Backpack",
            "iconID": 3
        },
        {
            "itemID": 1,
            "itemName": "Gold Coin",
            "iconID": 9
        },*/

        // Savepath, will popup save file location prompt
        string savePath = EditorUtility.SaveFilePanel("Save ItemDatabase export", "", "ItemDatabase.json", "json");

        // Path to ItemDatabase ScriptableObject
        string path = "Assets/Databases/ItemDatabase.asset";
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
        List<ItemExportEntry> entries = new List<ItemExportEntry>();

        if (database == null)
        {
            Debug.LogError("ItemDatabase not found at " + path);
            return;
        }

        foreach (ItemDataEntry item in database.itemList)
        {
            entries.Add(new ItemExportEntry(item.itemID, item.itemData.ItemName, item.itemData.IconID));
        }

        // Convert entries list to JSON format
        string json = JsonUtility.ToJson(new ItemDatabaseExport(entries), true);

        // Write JSON to a file
        File.WriteAllText(savePath, json);
    }

    [MenuItem("Tools/Export Icon Database as JSON")]
    public static void ExportIconDatabase() 
    {
        // Exports iconDatabase in this format as JSON
        /*"icons": [
        {
            "iconID": 0,
            "iconPath": "./icons/ability_hunter_explosiveshot.jpg"
        },
        {
            "iconID": 1,
            "iconPath": "./icons/inv_chest_plate03.jpg"
        },*/

        // Savepath, will popup save file location prompt
        string savePath = EditorUtility.SaveFilePanel("Save IconDatabase export", "", "IconDatabase.json", "json");

        // Path to IconDatabase ScriptableObject
        string path = "Assets/Databases/IconDatabase.asset";
        IconDatabase database = AssetDatabase.LoadAssetAtPath<IconDatabase>(path);
        List<IconDataEntry> databaseList = database.iconList;

        List<IconExportEntry> entries = new List<IconExportEntry>();

        if (database == null)
        {
            Debug.LogError("IconDatabase not found at " + path);
            return;
        }

        foreach(IconDataEntry icon in databaseList) 
        {
            // Format that path as "./icons/filename.fileformat"
            string iconPath = AssetDatabase.GetAssetPath(icon.icon);
            string filenameWithExtension = Path.GetFileName(iconPath);
            string formattedPath = $"./icons/{filenameWithExtension}";

            entries.Add(new IconExportEntry(icon.iconID, formattedPath));
        }

        // Convert entries list to JSON format
        string json = JsonUtility.ToJson(new IconDatabaseExport(entries), true);

        // Write JSON to a file
        File.WriteAllText(savePath, json);
    }

    [MenuItem("Tools/Create Null Items for Item Database")]
    public static void CreateNulls()
    {
        // Delete all nullFiles before iniation
        DeleteNulls();

        // Create path where to create the files
        string path = "Assets/Resources/Null_Items/";

        // Create generic ItemData null instance
        string assetPath = path + "ItemData_Null.asset";
        ScriptableObject nullItem = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(nullItem, assetPath);

        // Create an Assembly that has different kind of information, metadata etc. Any class that is typeof ItemData
        Assembly assembly = Assembly.GetAssembly(typeof(ItemData));

        // Get all subclasses of ItemData
        Type[] types = assembly.GetTypes();
        var itemTypes = types.Where(t => t.IsSubclassOf(typeof(ItemData)) && !t.IsAbstract);

        // Create null instances for each ItemData subclass
        foreach (Type type in itemTypes)
        {
            nullItem = ScriptableObject.CreateInstance(type) as ItemData;
            if (nullItem != null)
            {
                assetPath = path + type.Name + "_Null.asset";
                AssetDatabase.CreateAsset(nullItem, assetPath);
            }
            else
                Debug.LogError($"Failed to create null instance for {type.Name}");
        }

        // Deletes all files from null_Items folder
        void DeleteNulls() 
        {
            string path = "Assets/Resources/Null_Items/";
            if (Directory.Exists(path)) 
            {
                string[] files = Directory.GetFiles(path);
                foreach(string file in files) 
                {
                    try 
                    {
                        File.Delete(file);
                    }
                    catch(System.Exception error)
                    {
                        Debug.Log("Failed to delete file, error: " + error);
                    }
                }
            }
        }
    }

    // Use first time. If you delete items outside of ItemManager editorWindow; updateItemDatabase and then run this again
    [MenuItem("Tools/Get Next+Dropped ItemIDs")]
    public static void SetupItemIDSaveData() 
    {
        // Get ItemDatabase file
        string path = "Assets/Databases/ItemDatabase.asset";
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);

        // Get ItemManagerSaveData file
        path = "Assets/Databases/ItemManagerSave.asset";
        ItemManagerSaveData savedata = AssetDatabase.LoadAssetAtPath<ItemManagerSaveData>(path);

        // Clear droppedID list before operation (we don't need duplicates)
        savedata.droppedID.Clear();

        // Used to track ID in search
        int nextID = 1;

        // Check every itemEntry in ItemDatabase to find last+dropped IDs
        foreach (ItemDataEntry item in database.itemList)
        {
            Debug.Log("Currently looking for ID: " + nextID);
            // No need to check BackPack or NullItems; continue
            if (item.itemID <= 0)
                continue;

            // Current Item has skipped IDs; store all ID's between last ID and current itemID to droppedID list
            if (nextID != item.itemID)
            {
                Debug.Log("ID HUKATTU");
                for (int i = nextID; i < item.itemID; i++)
                    savedata.droppedID.Add(i);
            }
            else
                Debug.Log("ID OLEMASSA, NEXT");
            nextID = item.itemID + 1;
        }

        // Save currently nextID to saveData
        savedata.nextID = nextID;

        // Save the updated itemID saveData
        EditorUtility.SetDirty(savedata);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("ID OPERATION SUCCESFUL");
    }
}