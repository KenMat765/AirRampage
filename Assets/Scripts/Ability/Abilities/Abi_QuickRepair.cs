using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_QuickRepair : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        condition.revivalTime -= 2;
    }
}
