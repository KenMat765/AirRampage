using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_SkillBoost : Ability
{
    // public override int Weight { get; protected set; } = 15;
    // public override string Explanation { get; protected set; } = "Sortie with all skills fully charged.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_skillBoost = true;
    }
}
