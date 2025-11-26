using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class WeaponSlotUI : UISlotBase
{
    public void SetSlotLocation(InventoryUI ui)
    {
        if (referenceUI == null)
            referenceUI = ui;
    }

    public async Task OnDragSlotDrop(UISlotBase originSlot)
    {
        referenceUI.DraggingDropWeaponSlot(originSlot);
    }
}