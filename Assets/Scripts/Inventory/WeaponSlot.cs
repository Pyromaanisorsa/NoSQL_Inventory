using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSlot
{
    public WeaponData weapon;

    public WeaponSlot() 
    {
        weapon = null;
    }

    public WeaponSlot(WeaponData weapon) 
    {
        this.weapon = weapon;
    }

    public bool IsWeaponNull()
    {
        if (weapon.ItemID == 0)
            return true;
        return false;
    }
}
