using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_JetBallet3 : Ability
{
    // public override int Weight { get; protected set; } = 25;
    // public override string Explanation { get; protected set; } = "Considerably increases the normal bullet speed.";

    public override void Introducer(FighterCondition condition)
    {
        condition.attack.speed += 75;
    }
}
