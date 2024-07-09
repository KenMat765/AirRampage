using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_HpBoost2 : Ability
{
    // public override string Name { get; protected set; } = "HP Boost II";
    // public override int Weight { get; protected set; } = 15;
    // public override string Explanation { get; protected set; } = "HP + 30";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultHp += 30;
    }
}
