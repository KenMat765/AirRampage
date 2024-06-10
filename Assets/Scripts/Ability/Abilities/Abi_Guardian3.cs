using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Guardian3 : Ability
{
    // public override string Name { get; protected set; } = "Guardian III";
    // public override int Weight { get; protected set; } = 25;
    // public override string Explanation { get; protected set; } = "Defence +50";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultDefence += 0.5f;
    }
}
