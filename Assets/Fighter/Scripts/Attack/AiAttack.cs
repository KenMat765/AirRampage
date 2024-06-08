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
        if (BattleInfo.isMulti && !IsHost) return;

        SetHomingTargetNos();

        // Search for Attackable Terminals. /////////////////////////////////////////////////////////////////////
        if (BattleInfo.rule == Rule.TERMINALCONQUEST) SearchAttackableTerminals();


        // Normal Blast. ////////////////////////////////////////////////////////////////////////////////////////
        if (homingCount > 0 || (BattleInfo.rule == Rule.TERMINALCONQUEST && attackableTerminals.Count > 0))
        {
            blastTimer -= Time.deltaTime;
            if (blastTimer < 0)
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

                // If multiplayer, send to all clones to blast bullets.
                if (BattleInfo.isMulti) NormalRapidClientRpc(OwnerClientId, targetNo, rapidCount);
            }
        }
        else
        {
            blastTimer = setInterval;
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
                switch (skillData.GetSkillType())
                {
                    case SkillType.attack:
                        // Activate when fighters in front is more than thresh, or attackable terminals are in front.
                        if (homingCount >= target_thresh || (BattleInfo.rule == Rule.TERMINALCONQUEST && attackableTerminals.Count > 0))
                        {
                            skill.Activator();
                            elapsed_times[skill_num] = 0;
                            wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                        }
                        break;

                    case SkillType.heal:
                        // 自分の状態に応じて分岐
                        if (skillData.GetName() == "RepairDevice")
                        {
                            if (fighterCondition.HP < fighterCondition.defaultHP / 2)
                            {
                                skill.Activator();
                            }
                        }
                        break;

                    case SkillType.assist:
                        // 無条件に起動 
                        skill.Activator();
                        elapsed_times[skill_num] = 0;
                        wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                        break;

                    case SkillType.disturb:
                        // 標的がtarget_thresh機以上だったらスキル発動
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
