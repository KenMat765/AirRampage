using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_ComboBoostD : Ability
{
    public override string Name { get; protected set; } = "Combo Boost - D";
    public override int Weight { get; protected set; } = 15;
    public override string Explanation { get; protected set; } = "Defence increases at each 5 combos.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_comboBoostD = true;
    }
}
