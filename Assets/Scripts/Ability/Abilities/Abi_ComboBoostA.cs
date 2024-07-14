using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_ComboBoostA : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        if (condition.TryGetComponent(out ZoneController zone_controller))
        {
            zone_controller.has_comboBoostA = true;
        }
        else
        {
            Debug.LogWarning("Could not get ZoneController", condition.gameObject);
        }
    }
}
