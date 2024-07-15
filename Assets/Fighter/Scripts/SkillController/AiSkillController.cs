using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiSkillController : SkillController
{
    [SerializeField, Tooltip("Activate attack and disturb skills when lockon target exceed this value.")]
    int activateThresh;
    AiMovement aiMovement;
    Attack attack;

    // To prevent multiple skills from being activated simultaneously, add a freeze period after a skill is activated.
    const float FREEZE_TIME = 1;
    float freezeTimer = FREEZE_TIME;

    protected override void Awake()
    {
        base.Awake();
        aiMovement = fighterCondition.GetComponent<AiMovement>();
        attack = fighterCondition.GetComponentInChildren<Attack>();
    }

    void FixedUpdate()
    {
        if (!fighterCondition.IsOwner) return;
        if (fighterCondition.isDead) return;

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

    // This method trys to activate only one skill. Returns true if activated skill.
    bool TryActivateSkill()
    {
        int lockon_count = attack.lockonCount;
        for (int skill_no = 0; skill_no < skills.Length; skill_no++)
        {
            Skill skill = skills[skill_no];
            if (skill.isCharged)
            {
                switch (skill.skillType)
                {
                    case SkillType.attack:
                        if (lockon_count >= activateThresh)
                        {
                            SkillActivator(skill_no);
                        }
                        break;

                    case SkillType.heal:
                        // For RepairDevice, activate when your HP is less than 50%.
                        if (skill.skillName == "RepairDevice")
                        {
                            if (fighterCondition.Hp < fighterCondition.defaultHp / 2)
                            {
                                SkillActivator(skill_no);
                            }
                        }
                        break;

                    case SkillType.assist:
                        // For NitroBoost, activate only when destination is far enough.
                        if (skill.skillName == "NitroBoost")
                        {
                            const float DISTANCE_THRESH = 800;
                            Vector3 relative_to_next = aiMovement.nextDestination - transform.position;
                            float distance_to_destination = Vector3.Magnitude(relative_to_next);
                            if (distance_to_destination > DISTANCE_THRESH) skill.Activator();
                        }
                        // For other assist skills, activate as soon as it's charged.
                        else
                        {
                            SkillActivator(skill_no);
                        }
                        break;

                    case SkillType.disturb:
                        if (lockon_count >= activateThresh)
                        {
                            SkillActivator(skill_no);
                        }
                        break;
                }
                return true;
            }
        }
        return false;
    }

}
