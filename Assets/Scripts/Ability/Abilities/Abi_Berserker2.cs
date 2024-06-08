using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Berserker2 : Ability
{
    public override string Name { get; protected set; } = "Berserker - II";
    public override int Weight { get; protected set; } = 15;
    public override string Explanation { get; protected set; } = "Attack +30";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultPower += 0.3f;
    }
}
