using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class AiAttack : Attack
{
    // Blasts {rapidCount} bullets in {setInterval} seconds.
    public override float setInterval { get; set; } = 0.6f;
    protected override int rapidCount { get; set; } = 3;

    // Must be called on every clients.
    public override void OnDeath()
    {
        TerminateAllSkills();
    }


    // For Skills. ////////////////////////////////////////////////////////////////////////////////////////////////
    SkillData[] skillDatas;
    float[] elapsed_times, wait_times;
    const float min_wait_time = 0, max_wait_time = 15;
    const int target_thresh = 3;

    void Start()
    {
        int skill_length = skills.Length;
        elapsed_times = new float[skill_length];
        wait_times = new float[skill_length];
        skillDatas = new SkillData[skill_length];
        for (int k = 0; k < skill_length; k++)
        {
            wait_times[k] = Random.Range(min_wait_time, max_wait_time);
            skillDatas[k] = SkillDatabase.I.SearchSkillByName(skills[k].GetType().Name);
        }
    }

    void FixedUpdate()
    {
        if (!attackable) return;

        // Only the owner (= host) executes the following processes.
        if (!IsHost) return;

        // Normal Blast. ////////////////////////////////////////////////////////////////////////////////////////
        if (blastTimer > 0) blastTimer -= Time.deltaTime;

        else
        {
            // Search targets. (Fighters or Terminals)
            SetHomingTargetNos();
            if (BattleInfo.rule == Rule.TERMINAL_CONQUEST) SearchAttackableTerminals();

            if (homingCount > 0 || (BattleInfo.rule == Rule.TERMINAL_CONQUEST && attackableTerminals.Count > 0))
            {
                // Reset timer.
                blastTimer = setInterval;

                // Determine target.
                int targetNo = -1;
                GameObject target = null;

                // Do not home to terminals.
                if (homingCount > 0)
                {
                    targetNo = homingTargetNos[0];
                    target = ParticipantManager.I.fighterInfos[targetNo].body;
                }

                // Blast normal bullets for yourself.
                NormalRapid(rapidCount, target);
                // Send to all clones to blast bullets.
                NormalRapidClientRpc(OwnerClientId, rapidCount, targetNo);
            }
        }

        // Activate Skills. /////////////////////////////////////////////////////////////////////////////////////
        for (int skill_num = 0; skill_num < skills.Length; skill_num++)
        {
            Skill skill = skills[skill_num];
            SkillData skillData = skillDatas[skill_num];
            if (skill.isCharged)
            {
                // チャージが完了したら経過時間を計測
                elapsed_times[skill_num] += Time.deltaTime;

                // 待機時間を過ぎていなかったら何もしない
                if (elapsed_times[skill_num] < wait_times[skill_num]) return;

                // 待機時間を過ぎていて、かつType毎の条件を満たしていればスキルを発動
                string skill_name = skillData.GetName();
                switch (skillData.GetSkillType())
                {
                    case SkillType.attack:
                        // Activate when fighters in front is more than thresh, or attackable terminals are in front.
                        if (homingCount >= target_thresh || (BattleInfo.rule == Rule.TERMINAL_CONQUEST && attackableTerminals.Count > 0))
                        {
                            skill.Activator();
                            elapsed_times[skill_num] = 0;
                            wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                        }
                        break;

                    case SkillType.heal:
                        // For RepairDevice, activate when your HP is less than 50%.
                        if (skill_name == "RepairDevice")
                        {
                            if (fighterCondition.HP < fighterCondition.defaultHP / 2)
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
                            float distance_to_destination = Vector3.Magnitude(fighterCondition.movement.relative_to_next);
                            if (distance_to_destination > DISTANCE_THRESH) skill.Activator();
                        }
                        // For other assist skills, activate as soon as it's charged.
                        else
                        {
                            skill.Activator();
                            elapsed_times[skill_num] = 0;
                            wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                        }
                        break;

                    case SkillType.disturb:
                        // Activate when opponents in front is more than thresh.
                        if (homingCount >= target_thresh)
                        {
                            skill.Activator();
                            elapsed_times[skill_num] = 0;
                            wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                        }
                        break;
                }
            }
        }
    }
}
