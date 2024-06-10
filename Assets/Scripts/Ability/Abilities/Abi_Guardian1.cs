using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Guardian1 : Ability
{
    // public override string Name { get; protected set; } = "Guardian";
    // public override int Weight { get; protected set; } = 10;
    // public override string Explanation { get; protected set; } = "Defence +20";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultDefence += 0.2f;
    }
}
