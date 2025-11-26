using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

public class ItemDataManager : MonoBehaviour
{
    public static ItemDataManager Instance { get; private set; }
    [SerializeField] private ItemDatabase database;
    private Dictionary<int, ItemData> itemDictionary;
    private Dictionary<string, ItemData> nullDictionary;
    [SerializeField] private ItemData backpackData;
    [SerializeField] private bool generateNullsOnAwake = true;

    // Setup Item Dictionary and NullItem Dictionary before any other script can run start functions
    private void Awake()
    {
        // Create instance and ensure that THERE CAN ONLY BE ONE!
        if(Instance == null)
        {
            Instance = this;
            LoadDictionaries();
        }
        // Instance already exists, you fool!
        else 
        {
            Debug.Log("There can only be one!");
            Destroy(gameObject);
        }
    }

    // Convert ItemDataBase lists to dictionaries
    private void LoadDictionaries()
    {
        itemDictionary = database.itemList.ToDictionary(item => item.itemID, item => item.itemData);
        //backpackData = itemDictionary[-1];
        itemDictionary.Remove(-1);
        //database.itemList.Clear();

        if (generateNullsOnAwake)
            CreateNullItems();
        else
            nullDictionary = database.nullList.ToDictionary(item => item.className, item => item.nullItemData);
    }

    // Create NullInstances for each ItemData type
    private void CreateNullItems()
    {
        // Initialize nullDictionary
        nullDictionary = new Dictionary<string, ItemData>();

        // Add generic ItemData null instance
        nullDictionary["ItemData"] = ScriptableObject.CreateInstance<ItemData>();
        Debug.Log("Created and stored instance of: ItemData");

        // Create an Assembly that has different kind of information, metadata etc. Any class that is typeof ItemData
        Assembly assembly = Assembly.GetAssembly(typeof(ItemData));

        // Get all subclasses of ItemData
        Type[] types = assembly.GetTypes();
        IEnumerable<Type> itemTypes = types.Where(t => t.IsSubclassOf(typeof(ItemData)) && !t.IsAbstract);

        // Create a new Instance with default values(null values) for each subclass of ItemData 
        foreach (Type type in itemTypes)
        {
            ItemData itemInstance = ScriptableObject.CreateInstance(type) as ItemData;
            if (itemInstance != null)
            {
                nullDictionary[type.Name] = itemInstance;
                Debug.Log("Created and stored instance of: " + type.Name);
            }
            else
                Debug.LogError("Failed to create instance of: " + type.Name);
        }
    }

    // Get ItemData with ItemID from Item Dictionary
    public ItemData GetItemByID(int id)
    {
        if (id == -1)
            return backpackData;
        return itemDictionary[id];
    }

    // Get NullItem with className from NullItem Dictionary
    public T GetNullData<T>()where T : ItemData
    {
        // Check if there's nullItem with className T as key in dictionary and return value
        string className = typeof(T).Name;
        if (nullDictionary.TryGetValue(className, out ItemData itemData))
            return itemData as T;
        else
        {
            Debug.LogError("Null itemData for class: " + className + " not found!");
            return null;
        }
    }

    // Get random item from ItemDictionary
    public ItemData GetRandomItem()
    {
        return itemDictionary[UnityEngine.Random.Range(1, itemDictionary.Count+1)];
    }

    // Get ItemDictionary count
    public int GetItemCount()
    {
        return itemDictionary.Count();
    }
}