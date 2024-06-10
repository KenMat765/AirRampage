using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Idaten1 : Ability
{
    // public override string Name { get; protected set; } = "Lightning";
    // public override int Weight { get; protected set; } = 10;
    // public override string Explanation { get; protected set; } = "Speed +20";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultSpeed += 7f;
    }
}
