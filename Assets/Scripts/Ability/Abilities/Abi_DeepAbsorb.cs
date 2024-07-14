using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_DeepAbsorb : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        if (condition.TryGetComponent(out ZoneController zone_controller))
        {
            zone_controller.MultiplyCpBonus(1.5f);
        }
        else
        {
            Debug.LogWarning("Could not get ZoneController", condition.gameObject);
        }
    }
}
