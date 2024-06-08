using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_SkillKeep : Ability
{
    public override string Name { get; protected set; } = "Skill Keep";
    public override int Weight { get; protected set; } = 15;
    public override string Explanation { get; protected set; } = "Maintains skill charge even if destroyed.";
    public override void Introducer(FighterCondition condition)
    {
        condition.has_skillKeep = true;
    }
}
