using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_DeepAbsorb : Ability
{
    public override string Name { get; protected set; } = "Deep Absorb";
    public override int Weight { get; protected set; } = 10;
    public override string Explanation { get; protected set; } = "More CP is obtained when destroyed opponent.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_deepAbsorb = true;
    }
}
