using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class AiAttack : Attack
{
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
        // Set homing targets
        base.FixedUpdate();

        // Set isBlasting true if there are targets.
        // When multiplayer, only the owner set its own isBlasting true, and send a RPC to set isBlasting true at every other clones.
        if(homingTargets.Count > 0)
        {
            if(BattleInfo.isMulti)
            {
                if(IsOwner) SetIsBlastingServerRpc(true);
            }
            else
            {
                isBlasting.Value = true;
            }
        }
        // Set isBlasting false if there are no targets.
        // When multiplayer, only the owner set its own isBlasting false, and send a RPC to set isBlasting false at every other clones.
        else
        {
            if(BattleInfo.isMulti)
            {
                if(IsOwner) SetIsBlastingServerRpc(false);
            }
            else
            {
                isBlasting.Value = false;
            }
        }

        // Blast normal bullets if isBlasting is true.
        if(isBlasting.Value) NormalBlast();
        
        // Activate Skills
        // Only the host activates skills
        if(BattleInfo.isMulti && !IsHost) return;

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
                switch(skillData.GetSkillType())
                {
                    case SkillType.attack :
                    // 標的がtarget_thresh機以上だったらスキル発動
                    if(homingTargets.Count >= target_thresh)
                    {
                        skillToActivate = skill;
                        elapsed_times[skill_num] = 0;
                        wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                    }
                    break;

                    case SkillType.heal :
                    // 自分の状態に応じて分岐
                    if(skillData.GetName() == "RepairDevice")
                    {
                        if(fighterCondition.HP.Value < fighterCondition.default_HP/2)
                        {
                            skillToActivate = skill;
                        }
                    }
                    break;

                    case SkillType.assist :
                    // 無条件に起動 
                    skillToActivate = skill;
                    elapsed_times[skill_num] = 0;
                    wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                    break;

                    case SkillType.disturb :
                    // 標的がtarget_thresh機以上だったらスキル発動
                    if(homingTargets.Count >= target_thresh)
                    {
                        skillToActivate = skill;
                        elapsed_times[skill_num] = 0;
                        wait_times[skill_num] = Random.Range(min_wait_time, max_wait_time);
                    }
                    break;
                }
                if(skillToActivate != null) skillToActivate.Activator();
            }
        }
    }



    public override float homingAngle {get; set;} = 30;
    public override float homingDist {get; set;} = 30;
    protected override float normalInterval {get; set;} = 0.16f;

    // Must be called on every clients.
    public override void OnDeath()
    {
        foreach(Skill skill in skills) if(skill != null) skill.ForceTermination();
    }
}
