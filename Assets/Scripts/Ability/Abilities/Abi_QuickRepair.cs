using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_QuickRepair : Ability
{
    // public override int Weight { get; protected set; } = 10;
    // public override string Explanation { get; protected set; } = "Revives faster.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_quickRepair = true;
    }
}
