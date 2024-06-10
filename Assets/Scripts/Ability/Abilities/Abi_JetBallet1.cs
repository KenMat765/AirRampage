using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_JetBallet1 : Ability
{
    // public override string Name { get; protected set; } = "JetBullet";
    // public override int Weight { get; protected set; } = 10;
    // public override string Explanation { get; protected set; } = "Slightly increases the normal bullet speed.";
    public override void Introducer(FighterCondition condition)
    {
        condition.attack.speed += 30;
    }
}
