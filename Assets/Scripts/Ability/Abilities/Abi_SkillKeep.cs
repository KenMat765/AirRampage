using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abi_SkillKeep : Ability
{
    public override void Introducer(FighterCondition condition)
    {
        SkillExecuter skill_executer = condition.GetComponentInChildren<SkillExecuter>();
        if (skill_executer)
        {
            skill_executer.has_skillKeep = true;
        }
        else
        {
            Debug.LogWarning("Could not get SkillExecuter", condition.gameObject);
        }
    }
}
