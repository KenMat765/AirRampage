using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_SkillBoost : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        SkillExecuter skillExecuter = condition.GetComponentInChildren<SkillExecuter>();
        Skill[] skills = skillExecuter.skills;
        foreach (Skill skill in skills)
        {
            if (skill == null)
            {
                continue;
            }
            skill.elapsed_time = skill.charge_time;
        }
    }
}
