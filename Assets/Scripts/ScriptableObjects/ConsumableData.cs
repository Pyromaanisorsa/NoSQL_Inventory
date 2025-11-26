using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Inventory/Consumable")]
public class ConsumableData : ItemData
{
    [SerializeField] private string effect = "";

    //Public getters
    public string Effect => effect;

}
