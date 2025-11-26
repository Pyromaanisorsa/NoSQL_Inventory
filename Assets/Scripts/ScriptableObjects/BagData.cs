using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBag", menuName = "Inventory/Bag")]
[System.Serializable]
public class BagData : ItemData
{
    [SerializeField] private int bagSize = 0;

    // Public getter
    public int BagSize => bagSize;
}
