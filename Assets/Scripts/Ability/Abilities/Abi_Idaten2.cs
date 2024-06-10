using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Idaten2 : Ability
{
    // public override string Name { get; protected set; } = "Lightning II";
    // public override int Weight { get; protected set; } = 15;
    // public override string Explanation { get; protected set; } = "Speed +30";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultSpeed += 10.5f;
    }
}
