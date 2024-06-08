using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_HpBoost1 : Ability
{
    public override string Name { get; protected set; } = "HP Boost";
    public override int Weight { get; protected set; } = 10;
    public override string Explanation { get; protected set; } = "HP + 20";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultHP += 20;
    }
}
