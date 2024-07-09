using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_HpBoost3 : Ability
{
    // public override string Name { get; protected set; } = "HP Boost III";
    // public override int Weight { get; protected set; } = 25;
    // public override string Explanation { get; protected set; } = "HP + 50";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultHp += 50;
    }
}
