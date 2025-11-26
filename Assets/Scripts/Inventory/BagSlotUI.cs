using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class BagSlotUI : UISlotBase
{
    public Image stackBG;
    public int bagNum;
    public int slot;

    public void SetSlotLocation(int bagNum, int slot, InventoryUI ui)
    {
        this.bagNum = bagNum;
        this.slot = slot;

        if (referenceUI == null)
            referenceUI = ui;
    }

    public async Task OnDragSlotDrop(UISlotBase originSlot)
    {
        referenceUI.DraggingDropBagSlot(slot, originSlot);
    }
}