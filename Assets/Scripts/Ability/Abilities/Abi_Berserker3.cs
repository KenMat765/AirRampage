using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Berserker3 : Ability
{
    // public override int Weight { get; protected set; } = 25;
    // public override string Explanation { get; protected set; } = "Attack +50";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultPower += 0.5f;
    }
}
