using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_Technician_2 : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        Skill[] skills = condition.attack.skills;
        foreach (Skill skill in skills)
        {
            if (skill == null)
            {
                continue;
            }
            skill.charge_time /= 1.5f;
        }
    }
}
