using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_JetBallet2 : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        Attack attack = condition.GetComponentInChildren<Attack>();
        attack.bulletSpeed += 45;
    }
}
