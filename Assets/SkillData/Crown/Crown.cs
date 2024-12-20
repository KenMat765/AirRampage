using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Netcode;

class Crown : SkillAttack
{
    // スキルレベルによって変更の可能性があるパラメータ
    int crown_count;
    float interval;
    float grow_duration;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        crown_count = levelData.WeaponCount;
        interval = levelData.FreeFloat1;
        grow_duration = levelData.FreeFloat2;
    }

    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        InitTransformsLists(crown_count);
        const float r = 0.18f;
        for (int k = 0; k < crown_count; k++)
        {
            var angle = Mathf.PI / 2 + k * 2 * Mathf.PI / crown_count;
            var crown_localPos = new Vector3(r * Mathf.Cos(angle), r * Mathf.Sin(angle), 0);
            SetPrefabLocalTransforms(k, crown_localPos, Vector3.zero, Vector3.zero);
        }
        GeneratePrefabs(crown_count);
    }

    public override int[] Activator(int[] received_data = null)
    {
        base.Activator();
        MeterDecreaser(interval * crown_count * 2);

        GameObject[] targets = new GameObject[crown_count];
        if (skillController.IsOwner)
        {
            int[] target_nos = new int[crown_count];
            for (int k = 0; k < target_nos.Length; k++) target_nos[k] = -1;

            // Activate your own skill.
            if (attack.lockonCount > 0)
            {
                for (int k = 0; k < crown_count; k++)
                {
                    int target_no = attack.lockonTargetNos[k % attack.lockonCount];
                    targets[k] = ParticipantManager.I.fighterInfos[target_no].body;
                    target_nos[k] = target_no;
                }
            }
            StartCoroutine(activator(targets));
            return target_nos;
        }
        else
        {
            // Receive Rpc from the owner.
            for (int k = 0; k < crown_count; k++)
            {
                // Convert received target numbers to fighter-body.
                if (received_data[k] != -1)
                {
                    targets[k] = ParticipantManager.I.fighterInfos[(int)received_data[k]].body;
                }
            }
            StartCoroutine(activator(targets));
            return null;
        }
    }

    IEnumerator activator(GameObject[] targets)
    {
        // 今回の発射で使用するweapons
        Weapon[] weapons_this_time = new Weapon[crown_count];
        int[] ready_indexes = GetPrefabIndexes(crown_count);
        for (int k = 0; k < crown_count; k++) weapons_this_time[k] = weapons[ready_indexes[k]];

        // Blast bullets.
        float fighter_power = skillController.fighterCondition.power.value;
        for (int k = 0; k < crown_count; k++)
        {
            weapons_this_time[k].Activate(targets[k], fighter_power);
            yield return new WaitForSeconds(interval);
        }
    }

    protected override System.Func<float> StayMotionGenerator(GameObject prefab)
    {
        System.Func<float> motion;
        motion = () =>
        {
            prefab.transform.localScale = Vector3.zero;
            prefab.transform.DOScale(new Vector3(0.07f, 0.07f, 0.07f), grow_duration).SetEase(Ease.OutQuint);
            return interval * crown_count;
        };
        return motion;
    }
}