using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Technician : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        SkillController skill_executer = condition.GetComponentInChildren<SkillController>();
        if (skill_executer)
        {
            Skill[] skills = skill_executer.skills;
            foreach (Skill skill in skills)
            {
                if (skill == null)
                {
                    continue;
                }
                skill.charge_time /= 1.2f;
            }
        }
        else
        {
            Debug.LogWarning("Could not get SkillExecuter", condition.gameObject);
        }
    }
}
