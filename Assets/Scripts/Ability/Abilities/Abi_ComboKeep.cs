using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_ComboKeep : Ability
{
    // public override string Name { get; protected set; } = "Combo Keep";
    // public override int Weight { get; protected set; } = 5;
    // public override string Explanation { get; protected set; } = "Extend the time until combos run out.";
    public override void Introducer(FighterCondition condition)
    {
        condition.default_combo_timer += 1.5f;
    }
}
