using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is just used as baseClass for inventory UI slot classes; to make the dragging dropping work smoothly
public class UISlotBase : MonoBehaviour
{
    public CustomButton itemButton;
    protected InventoryUI referenceUI;
    public DragInventorySlot dragReference;
}
