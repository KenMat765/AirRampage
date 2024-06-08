using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Idaten3 : Ability
{
    public override string Name { get; protected set; } = "Lightning - III";
    public override int Weight { get; protected set; } = 25;
    public override string Explanation { get; protected set; } = "Speed +50";
    public override void Introducer(FighterCondition condition)
    {
        condition.defaultSpeed += 17.5f;
    }
}