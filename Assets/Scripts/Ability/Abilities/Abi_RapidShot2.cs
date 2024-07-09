using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_RapidShot2 : Ability
{
    // public override int Weight { get; protected set; } = 15;
    // public override string Explanation { get; protected set; } = "Somewhat increases the fire rate of normal bullet.";
    public override void Introducer(FighterCondition condition)
    {
        condition.attack.blastInterval /= 1.4f;
    }
}
