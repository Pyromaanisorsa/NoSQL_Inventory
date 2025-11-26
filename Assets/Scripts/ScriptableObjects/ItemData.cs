using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/GeneralItem")]
public class ItemData : ScriptableObject
{
    [SerializeField] private ItemType itemType = ItemType.Null;
    [SerializeField] private int itemID = 0;
    [SerializeField] private string itemName = "";
    [SerializeField] private string itemDescription = "";
    [SerializeField] private bool stackable = false;
    [SerializeField] private int maxStack = 0;
    [SerializeField] private float weight = 0;
    [SerializeField] private int iconID;

    //Public getters
    public ItemType ItemType => itemType;
    public int ItemID => itemID;
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public bool Stackable => stackable;
    public int MaxStack => maxStack;
    public float Weight => weight;
    public int IconID => iconID;

    // Used to set values of Items/Instances created inside ItemManager EditorWindow
    public virtual void Initialize(ItemType type, int id, string name, string desc, bool stackable, int maxStack, float weight) 
    {
        itemType = type;
        itemID = id;
        itemName = name;
        itemDescription = desc;
        this.stackable = stackable;
        this.maxStack = maxStack;
        this.weight = weight;
    }
}

public enum ItemType
{
    Generic,
    Null,
    Consumable,
    Bag,
    Weapon,
    Armor
};
