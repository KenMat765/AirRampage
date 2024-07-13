using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class AiAttack : Attack
{
    void Start()
    {
        int skill_length = skills.Length;
        skillDatas = new SkillData[skill_length];
        for (int k = 0; k < skill_length; k++)
        {
            skillDatas[k] = SkillDatabase.I.SearchSkillByName(skills[k].GetType().Name);
        }
        aiMovement = (AiMovement)fighterCondition.movement;
    }

    void FixedUpdate()
    {
        if (!attackable) return;
        if (!IsOwner) return;

        // === Normal Blast === //
        if (blastTimer > 0)
        {
            blastTimer -= Time.deltaTime;
        }
        else
        {
            SetLockonTargetNos();
            if (lockonCount > 0)
            {
                blastTimer = blastInterval;
                int targetNo = lockonTargetNos[0];
                GameObject target = ParticipantManager.I.fighterInfos[targetNo].body;
                int rapid_count = 3;
                NormalRapid(rapid_count, target);
            }
        }

        // === Activate Skills === //
        if (freezeTimer > 0)
        {
            freezeTimer -= Time.deltaTime;
        }
        else
        {
            bool activated_skill = TryActivateSkill();
            if (activated_skill)
            {
                freezeTimer = FREEZE_TIME;
            }
        }
    }



    // Skills //////////////////////////////////////////////////////////////////////////////////////////////////////////
    [Header("Skill")]
    [SerializeField, Tooltip("Activate attack and disturb skills when lockon target exceed this value.")]
    int activateThresh;

    SkillData[] skillDatas;

    // To prevent multiple skills from being activated simultaneously, add a freeze period after a skill is activated.
    const float FREEZE_TIME = 1;
    float freezeTimer = FREEZE_TIME;

    AiMovement aiMovement;

    // This method trys to activate only one skill. Returns true if activated skill.
    bool TryActivateSkill()
    {
        for (int skill_num = 0; skill_num < skills.Length; skill_num++)
        {
            Skill skill = skills[skill_num];
            SkillData skillData = skillDatas[skill_num];
            if (skill.isCharged)
            {
                string skill_name = skillData.GetName();
                switch (skillData.GetSkillType())
                {
                    case SkillType.attack:
                        if (lockonCount >= activateThresh)
                        {
                            skill.Activator();
                        }
                        break;

                    case SkillType.heal:
                        // For RepairDevice, activate when your HP is less than 50%.
                        if (skill_name == "RepairDevice")
                        {
                            if (fighterCondition.Hp < fighterCondition.defaultHp / 2)
                            {
                                skill.Activator();
                            }
                        }
                        break;

                    case SkillType.assist:
                        // For NitroBoost, activate only when destination is far enough.
                        if (skill_name == "NitroBoost")
                        {
                            const float DISTANCE_THRESH = 800;
                            Vector3 relative_to_next = aiMovement.nextDestination - transform.position;
                            float distance_to_destination = Vector3.Magnitude(relative_to_next);
                            if (distance_to_destination > DISTANCE_THRESH) skill.Activator();
                        }
                        // For other assist skills, activate as soon as it's charged.
                        else
                        {
                            skill.Activator();
                        }
                        break;

                    case SkillType.disturb:
                        if (lockonCount >= activateThresh)
                        {
                            skill.Activator();
                        }
                        break;
                }
                return true;
            }
        }
        return false;
    }



    // Death & Revival ////////////////////////////////////////////////////////////////////////////////////////////////

    // This if DEATH_NORMAL_BLAST for fighters, but change this to SPECIFIC_DEATH_CANNON for cannons.
    protected override string causeOfDeath { get; set; } = FighterCondition.DEATH_NORMAL_BLAST;

    // Must be called on every clients.
    protected override void OnDeath(int destroyerNo, string causeOfDeath)
    {
        base.OnDeath(destroyerNo, causeOfDeath);
        TerminateAllSkills();
    }
}
