using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_ComboBoostS : Ability
{
    public override string Name { get; protected set; } = "Combo Boost - S";
    public override int Weight { get; protected set; } = 15;
    public override string Explanation { get; protected set; } = "Speed increases at each 5 combos.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_comboBoostS = true;
    }
}
