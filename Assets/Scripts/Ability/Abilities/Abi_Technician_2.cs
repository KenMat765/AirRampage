using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Technician_2 : Ability
{
    // public override string Name { get; protected set; } = "Technician II";
    // public override int Weight { get; protected set; } = 15;
    // public override string Explanation { get; protected set; } = "Charges skills more quickly.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_technician_2 = true;
    }
}
