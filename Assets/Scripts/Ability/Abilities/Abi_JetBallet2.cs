using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_JetBallet2 : Ability
{
    // public override string Name { get; protected set; } = "JetBullet II";
    // public override int Weight { get; protected set; } = 15;
    // public override string Explanation { get; protected set; } = "Somewhat increases the normal bullet speed.";
    public override void Introducer(FighterCondition condition)
    {
        condition.attack.bulletSpeed += 45;
    }
}
