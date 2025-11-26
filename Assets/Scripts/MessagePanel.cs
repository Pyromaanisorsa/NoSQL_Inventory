using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessagePanel : MonoBehaviour
{
    public static MessagePanel Instance { get; private set; }
    [SerializeField] private GameObject inspectBox;
    private TextMeshProUGUI text;
    private Coroutine currentCoroutine;

    // Setup Item Dictionary and NullItem Dictionary before any other script can run start functions
    private void Awake()
    {
        // Create instance and ensure that THERE CAN ONLY BE ONE!
        if (Instance == null)
        {
            Instance = this;
            text = inspectBox.GetComponentInChildren<TextMeshProUGUI>();
        }
        // Instance already exists, you fool!
        else
        {
            Debug.Log("There can only be one!");
            Destroy(gameObject);
        }
    }

    // Send a message to inspectPanel to display (default duration 5 seconds)
    public void DisplayMessage(string message, int duration = 5)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(ShowMessage(message, duration));
    }

    // Send a message to show all items obtained in AddItem
    public void DisplayMessageLoot(List<string> addedItems, int duration = 5) 
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(ShowMessageAddItem(addedItems));
    }

    #region Coroutines
    //Display a message in inspectPanel
    private IEnumerator ShowMessage(string message, int duration)
    {
        text.text = message;

        inspectBox.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        inspectBox.gameObject.SetActive(false);
    }

    // Display all items obtained from AddItem
    private IEnumerator ShowMessageAddItem(List<string> addedItems)
    {
        text.text = $"Items obtained";

        foreach (string add in addedItems)
        {
            text.text += $"\n{add}";
        }

        inspectBox.gameObject.SetActive(true);
        yield return new WaitForSeconds(3 + addedItems.Count * 2);
        inspectBox.gameObject.SetActive(false);
    }

    /*// Show item data+location on clicked slot
    private IEnumerator InspectSlot(int slot)
    {
        // Makes code less bloated
        int bagNum = slotList[slot].bagNum, slotNum = slotList[slot].slotNum;
        InventorySlot temp = inventory.bags[slotList[slot].bagNum].bagInventory[slotList[slot].slotNum];

        TextMeshProUGUI text = inspectBox.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"Location: Bag: {bagNum + 1} | Slot: {slotNum + 1}\n{temp.item.ItemName}\n{temp.item.ItemDescription}\n{temp.item.ItemType}\nWeight: {temp.item.Weight}";
        if (temp.item.Stackable)
        {
            text.text += $"\nStack: {inventory.bags[bagNum].bagInventory[slotNum].currentStack} / {temp.item.MaxStack}";
        }

        inspectBox.gameObject.SetActive(true);
        yield return new WaitForSeconds(3);
        inspectBox.gameObject.SetActive(false);
    }

    // Show contents of (not-NULL) clicked bagSlot
    private IEnumerator InspectBagSlot(int slot)
    {
        // Makes code more bearable
        BagSlot temp = inventory.bags[slot];

        TextMeshProUGUI text = inspectBox.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"Bag #{slot + 1}: {temp.bagItem.ItemName}";
        int i = 1;

        // Print each item and slot # in bag; show stacks if item is stackable as well
        foreach (InventorySlot item in temp.bagInventory)
        {
            if (item.IsSlotNull())
            {
                text.text += $"\nSlot #{i} | NULL";
            }
            else
            {
                text.text += $"\nSlot #{i} | {item.item.ItemName}";
                if (item.item.Stackable)
                    text.text += $" | Stack: {item.currentStack} / {item.item.MaxStack}";
            }
            i++;
        }

        // Show the message in inspector and hide panel after waitTimer
        inspectBox.gameObject.SetActive(true);
        yield return new WaitForSeconds(3 + i);
        inspectBox.gameObject.SetActive(false);
    }*/
    #endregion
}
