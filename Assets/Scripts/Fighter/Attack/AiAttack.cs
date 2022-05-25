using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class AiAttack : Attack
{
    public override float homingAngle {get; set;} = 30;
    public override float homingDist {get; set;} = 30;
    protected override float setInterval {get; set;} = 0.6f;
    protected override int rapidCount {get; set;} = 3;

    // Must be called on every clients.
    public override void OnDeath()
    {
        foreach(Skill skill in skills) if(skill != null) skill.ForceTermination();
    }


    // For Skills. ////////////////////////////////////////////////////////////////////////////////////////////////
    SkillData[] skillDatas;
    float[] elapsed_times, wait_times;
    const float min_wait_time = 0, max_wait_time = 15;
    const int target_thresh = 3;

    protected override void Start()
    {
        base.Start();

        int skill_length = skills.Length;
        elapsed_times = new float[skill_length];
        wait_times = new float[skill_length];
        skillDatas = new SkillData[skill_length];
        for(int k = 0; k < skill_length; k ++)
        {
            wait_times[k] = Random.Range(min_wait_time, max_wait_time);
            skillDatas[k] = SkillDatabase.I.SearchSkillByName(skills[k].GetType().Name);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();


        // Only the owner (= host) executes the following processes.
        if(BattleInfo.isMulti && !IsHost) return;


        // Normal Blast. ////////////////////////////////////////////////////////////////////////////////////////
        if(homingCount > 0)
        {
            blastTimer -= Time.deltaTime;
            if(blastTimer < 0)
            {
                // Set timer.
                blastTimer = setInterval;

                // Determine target.
                int targetNo = homingTargetNos[0];
                GameObject target = ParticipantManager.I.fighterInfos[targetNo].body;

                // Blast normal bullets for yourself.
                NormalRapid(target, rapidCount);

                // If multiplayer, send to all clones to blast bullets.
                if(BattleInfo.isMulti) NormalRapidClientRpc(OwnerClientId, targetNo, rapidCount);
            }
        }
        else
        {
            blastTimer = 0;
        }


        // Activate Skills. /////////////////////////////////////////////////////////////////////////////////////
        for(int skill_num = 0; skill_num < skills.Length; skill_num ++)
        {
            Skill skill = skills[skill_num];
            SkillData skillData = skillDatas[skill_num];
            if(skill.isCharged)
            {
                // チャージが完了したら経過時間を計測
                elapsed_times[skill_num] += Time.deltaTime;

                // 待機時間を過ぎていなかったら何もしない
                if(elapsed_times[skill_num] < wait_times[skill_num]) return;

                // 待機時間を過ぎていて、かつType毎の条件を満たしていればスキルを発動
                Skill skillToActivate = null;

                // Boolean which determines whether to activate skill immediately in clones when the owner has activated.
                // For attack & disturb skills, clones must receive target fighter from owner, so we should not activate immediately when the owner has activated.
                // For heal & assist skills, there are no messages clones need to receive from the owner, so we can enable clones to activate skills immediately.
                bool acitivateInCloneImmediate = false;

                switch(skillData.GetSkillType())
                {
                    case SkillType.attack :
                    // 標的がtarget_thresh機以上だったらスキル発動
                    if(homingCount >= target_thresh)
                    {
                        skillToActivate = skill;
                        acitivateInCloneImmediate = false;
                        elapsed_times[skill_num] = 0;
                        wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                    }
                    break;

                    case SkillType.heal :
                    // 自分の状態に応じて分岐
                    if(skillData.GetName() == "RepairDevice")
                    {
                        if(fighterCondition.HP < fighterCondition.default_HP/2)
                        {
                            skillToActivate = skill;
                            acitivateInCloneImmediate = true;
                        }
                    }
                    break;

                    case SkillType.assist :
                    // 無条件に起動 
                    skillToActivate = skill;
                    acitivateInCloneImmediate = true;
                    elapsed_times[skill_num] = 0;
                    wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                    break;

                    case SkillType.disturb :
                    // 標的がtarget_thresh機以上だったらスキル発動
                    if(homingCount >= target_thresh)
                    {
                        skillToActivate = skill;
                        acitivateInCloneImmediate = false;
                        elapsed_times[skill_num] = 0;
                        wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                    }
                    break;
                }

                if(BattleInfo.isMulti)
                {
                    if (skillToActivate != null)
                    {
                        // Owner Activate its own skill.
                        skillToActivate.Activator();
                        if (acitivateInCloneImmediate)
                        {
                            // Owner send RPC to clones to tell them to Activate their own skills.
                            SkillActivatorClientRpc(OwnerClientId, skillToActivate.skillNo);
                        }
                    }
                }
                else
                {
                    if (skillToActivate != null) skillToActivate.Activator();
                }
            }
        }
    }
}
