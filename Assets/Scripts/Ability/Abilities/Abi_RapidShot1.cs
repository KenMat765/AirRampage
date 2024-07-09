using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_RapidShot1 : Ability
{
    // public override int Weight { get; protected set; } = 10;
    // public override string Explanation { get; protected set; } = "Slightly increases the fire rate of normal bullet.";
    public override void Introducer(FighterCondition condition)
    {
        condition.attack.blastInterval /= 1.2f;
    }
}
