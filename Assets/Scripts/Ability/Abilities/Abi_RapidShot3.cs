using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_RapidShot3 : Ability
{
    // public override int Weight { get; protected set; } = 20;
    // public override string Explanation { get; protected set; } = "Considerably increases the fire rate of normal bullet.";
    public override void Introducer(FighterCondition condition)
    {
        condition.attack.blastInterval /= 1.6f;
    }
}
