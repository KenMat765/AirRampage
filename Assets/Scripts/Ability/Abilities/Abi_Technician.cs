using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Technician : Ability
{
    public override string Name { get; protected set; } = "Technician";
    public override int Weight { get; protected set; } = 10;
    public override string Explanation { get; protected set; } = "Charges skills quickly.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_technician_1 = true;
    }
}
