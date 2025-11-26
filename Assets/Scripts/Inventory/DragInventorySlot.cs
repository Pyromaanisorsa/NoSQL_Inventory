using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragInventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GameObject draggableIcon;
    [SerializeField] private GameObject dragPreFab;
    private CustomButton currenthoveredButton;

    // Begin dragging, create a "copy" image of slot that follows mouse cursor
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Instantiate the draggable copy
        draggableIcon = Instantiate(dragPreFab);

        // Set the copy as child of main inventory gameObject
        draggableIcon.transform.SetParent(transform.parent.parent, false);

        // Set image of copy to the inventory slot image and disable raycast target on image so it won't block the OnEndDrag pointer
        draggableIcon.GetComponent<Image>().sprite = gameObject.GetComponent<UISlotBase>().itemButton.image.sprite;
        draggableIcon.GetComponent<Image>().raycastTarget = false;

        // Set element as LastChild so it'll be top of everything in inventory
        draggableIcon.transform.SetAsLastSibling();
    }

    // Move the draggable copy during drag at center of mouse
    public void OnDrag(PointerEventData eventData)
    {
        if (draggableIcon != null)
        {
            // Move the copy and activate highlight color if there's a button under the pointer
            draggableIcon.transform.position = Input.mousePosition;
            GameObject hoveredObject = eventData.pointerEnter;

            if(hoveredObject != null) 
            {
                CustomButton hoveredButton = hoveredObject.GetComponent<CustomButton>();
                if(hoveredButton != null && hoveredButton != currenthoveredButton) 
                {
                    if(currenthoveredButton != null) 
                    {
                        currenthoveredButton.OnPointerExit(eventData);
                    }
                    hoveredButton.OnPointerEnter(eventData);
                    currenthoveredButton = hoveredButton;
                }
            }
            else 
            {
                if(currenthoveredButton != null) 
                {
                    currenthoveredButton.OnPointerExit(eventData);
                    currenthoveredButton = null;
                }
            }
        }
    }

    // End dragging and do something based off what type of slot was dropped to what type of slot
    public void OnEndDrag(PointerEventData eventData)
    {
        // Destroy the draggable copy
        if (draggableIcon != null)
        {
            Destroy(draggableIcon);
        }

        // Check if drop target parent contains InventoryUISlot script; need to check from parent because it's child button will block the raycast
        if (eventData.pointerEnter != null && eventData.pointerEnter.GetComponentInParent<UISlotBase>())
        {
            // Get origin slot script
            UISlotBase originSlotScript = gameObject.GetComponent<UISlotBase>();

            // Get the target InventorySlotUI script
            UISlotBase targetSlot = eventData.pointerEnter.GetComponentInParent<UISlotBase>();

            // Run drop function from the target slot
            if (targetSlot != null)
            {
                // If dropped at inventoryUISlot
                if(targetSlot is InventoryUISlot invSlotUI)
                    invSlotUI.OnDragSlotDrop(originSlotScript);

                // Else if dropped at BagSlotUI
                else if(targetSlot is BagSlotUI bagSlotUI)
                    bagSlotUI.OnDragSlotDrop(originSlotScript);

                // Else if dropped at WeaponSlotUI
                else if(targetSlot is WeaponSlotUI wepSlotUI)
                    wepSlotUI.OnDragSlotDrop(originSlotScript);
            }
        }
        else
            Debug.Log("FELL TO NARNIA");
    }
}
