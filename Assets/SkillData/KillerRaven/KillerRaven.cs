using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class KillerRaven : SkillAttack
{
    // スキルレベルによって変更の可能性があるパラメータ
    int raven_count;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        raven_count = levelData.WeaponCount;
    }

    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        InitTransformsLists(raven_count);
        const float r = 0.15f;
        for (int k = 0; k < raven_count; k++)
        {
            var angle = k * 2 * Mathf.PI / raven_count - Mathf.PI / 2.1f;
            var euler_angle = angle * 180 / Mathf.PI;
            var raven_localPos = new Vector3(r * Mathf.Cos(angle), r * Mathf.Sin(angle), 0);
            SetPrefabLocalTransforms(k, raven_localPos, new Vector3(-euler_angle, 90, -90), new Vector3(0.4f, 0.4f, 0.4f));
        }
        GeneratePrefabs(raven_count);
    }

    public override int[] Activator(int[] received_targetNos = null)
    {
        base.Activator();
        MeterDecreaser();

        // 今回の発射で使用するweapons
        Weapon[] weapons_this_time = new Weapon[raven_count];
        int[] ready_indexes = GetPrefabIndexes(raven_count);
        for (int k = 0; k < raven_count; k++) weapons_this_time[k] = weapons[ready_indexes[k]];

        GameObject[] targets = new GameObject[raven_count];
        if (skillController.IsOwner)
        {
            int[] target_nos = new int[raven_count];
            for (int k = 0; k < target_nos.Length; k++) target_nos[k] = -1;

            // Activate your own skill.
            if (attack.lockonCount > 0)
            {
                for (int k = 0; k < raven_count; k++)
                {
                    int target_no = attack.lockonTargetNos[k % attack.lockonCount];
                    targets[k] = ParticipantManager.I.fighterInfos[target_no].body;
                    target_nos[k] = target_no;
                }
            }
            StartCoroutine(activator(weapons_this_time, targets));
            return target_nos;
        }
        else
        {
            // Receive Rpc from the owner.
            for (int k = 0; k < raven_count; k++)
            {
                // Convert received target numbers to fighter-body.
                if (received_targetNos[k] != -1)
                {
                    targets[k] = ParticipantManager.I.fighterInfos[received_targetNos[k]].body;
                }
            }
            StartCoroutine(activator(weapons_this_time, targets));
            return null;
        }
    }

    IEnumerator activator(Weapon[] weapons, GameObject[] targets)
    {
        float fighter_power = skillController.fighterCondition.power.value;
        const float interval = 0.05f;
        for (int k = 0; k < raven_count; k++)
        {
            weapons[k].Activate(targets[k], fighter_power);
            yield return new WaitForSeconds(interval);
        }
    }
}