using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Berserker1 : Ability
{
    // public override int Weight { get; protected set; } = 10;
    // public override string Explanation { get; protected set; } = "Attack +20";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultPower += 0.2f;
    }
}
