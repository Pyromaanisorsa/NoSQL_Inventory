using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Item : MonoBehaviour
{
    [Tooltip("Item's datafile.")]
    public TextMeshPro text;
    public bool trueRandom = false;
    public ItemLoot[] lootTable;
    public List<InventorySlot> lootList = new List<InventorySlot>();
    private bool active = true;
    private GameObject sphere;

    // Generate lootList from lootTable OnEnable
    private void Start()
    {
        sphere = transform.Find("Sphere").gameObject;
        GenerateLootList();
    }

    // Generate lootList from lootTable items
    private void GenerateLootList()
    {
        if(trueRandom)
        {
            int id = ItemDataManager.Instance.GetItemCount();
            ItemData item;
            int quantity = Random.Range(1, 6);
            for(int i = 0; i < quantity; i++) 
            {
                item = ItemDataManager.Instance.GetRandomItem();
                if (item.Stackable)
                    lootList.Add(new InventorySlot(Random.Range(1, item.MaxStack), item));
                else
                    lootList.Add(new InventorySlot(item));
            }
        }
        else 
        {
            foreach (ItemLoot loot in lootTable)
                lootList.Add(new InventorySlot(SetStack(loot), loot.item));
        }
        PrintLootList();


        // Set Item Stack count, based off if Item is stackable
        int SetStack(ItemLoot loot)
        {
            if (loot.isRandom)
            {
                if (loot.item.Stackable)
                {
                    return Random.Range(1, loot.item.MaxStack);
                }
                else
                    return 1;
            }
            else
                return loot.stack;
        }
    }

    // Print item status and loot to the designated 3D text
    private void PrintLootList()
    {
        if (text == null)
            return;

        if (trueRandom)
            text.text = "LootType: Random\n";
        else
            text.text = "LootType: Static\n";

        text.text += $"Status: ";

        if (lootList.Count <= 0)
        {
            text.text += $"Recharging";
            return;
        }
        else 
        {
            text.text += $"Active";

            foreach (InventorySlot slot in lootList)
            {
                if (slot.item.Stackable)
                    text.text += $"\n- {slot.currentStack}x {slot.item.ItemName}";
                else
                    text.text += $"\n- {slot.item.ItemName}";
            }
        }
    }

    // Clean empty items from the lootList and check if the lootList is empty
    public void UpdateItem()
    {
        lootList.RemoveAll(item => item.currentStack == 0);
        if (lootList.Count <= 0)
        {
            sphere.GetComponent<Renderer>().material.color = new Color(1, 0, 0);
            PrintLootList();
            StartCoroutine(RespawnLoot());
        }
        else
        {
            PrintLootList();
            active = true;
        }
    }

    // Check if item is active; lock it, if it is available
    public bool IsActive() 
    {
        if (!active)
            return false;

        active = false;
        sphere.GetComponent<Renderer>().material.color = new Color(0, 0, 1);
        return true;
    }

    // Respawn loot after 3-5 seconds after the lootList becomes empty
    IEnumerator RespawnLoot() 
    {
        yield return new WaitForSeconds(Random.Range(3,6));
        sphere.GetComponent<Renderer>().material.color = new Color(0, 1, 0);
        GenerateLootList();
        active = true;
    }
}

[System.Serializable]
public struct ItemLoot
{
    [SerializeField] public ItemData item;
    [SerializeField] public bool isRandom;
    [SerializeField] public int stack;
}
