using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_ComboBoostA : Ability
{
    public override string Name { get; protected set; } = "Combo Boost - A";
    public override int Weight { get; protected set; } = 15;
    public override string Explanation { get; protected set; } = "Attack increases at each 5 combos.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_comboBoostA = true;
    }
}
