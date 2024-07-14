using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_RapidShot2 : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        Attack attack = condition.GetComponentInChildren<Attack>();
        attack.blastInterval /= 1.4f;
    }
}
