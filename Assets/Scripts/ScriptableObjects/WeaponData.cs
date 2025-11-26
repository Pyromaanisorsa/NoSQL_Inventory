using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponData : ItemData
{
    [SerializeField] private GameObject weaponModel;
    [SerializeField] private WeaponType weaponType = WeaponType.Unarmed;

    //Public getters
    public GameObject WeaponModel => weaponModel;
    public WeaponType WeaponType => weaponType;
}

public enum WeaponType
{
    Unarmed,
    Sword,
    Axe,
    Polearm,
    Bow,
    Firearm
};
