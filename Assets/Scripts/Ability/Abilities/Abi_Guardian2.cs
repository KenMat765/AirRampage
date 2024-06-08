using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Guardian2 : Ability
{
    public override string Name { get; protected set; } = "Guardian - II";
    public override int Weight { get; protected set; } = 15;
    public override string Explanation { get; protected set; } = "Defence +30";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultDefence += 0.3f;
    }
}
