using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;

public class InventoryUISlot : UISlotBase
{
    public Image stackBG;

    public int bagNum;
    public int slotNum;
    public int slot; // Slot number in UI order
    private bool running; // Prevent slot running more than one function at a time

    public void SetSlotLocation(int bagNum, int slotNum, int slot, InventoryUI ui) 
    {
        this.bagNum = bagNum;
        this.slotNum = slotNum;
        this.slot = slot;

        if (referenceUI == null)
            referenceUI = ui;
    }

    public async Task OnDragSlotDrop(UISlotBase originSlot)
    {
        referenceUI.DraggingDropInventorySlot(slot, originSlot);
    }
}