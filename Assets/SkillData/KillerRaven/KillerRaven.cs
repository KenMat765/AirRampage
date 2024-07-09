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

    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
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

    public override void Activator(int[] received_targetNos = null)
    {
        base.Activator();
        MeterDecreaser();

        // 今回の発射で使用するweapons
        Weapon[] weapons_this_time = new Weapon[raven_count];
        int[] ready_indexes = GetPrefabIndexes(raven_count);
        for (int k = 0; k < raven_count; k++) weapons_this_time[k] = weapons[ready_indexes[k]];

        // Target objects necessary to activate this skill.
        GameObject[] targets = new GameObject[raven_count];

        // Multi Players.
        if (BattleInfo.isMulti)
        {
            if (attack.IsOwner)
            {
                // Owner of this skill pack target fighter's number to this array.
                int[] target_nos = new int[raven_count];
                for (int k = 0; k < target_nos.Length; k++) target_nos[k] = -1;

                // Activate your own skill.
                if (attack.lockonCount > 0)
                {
                    for (int k = 0; k < raven_count; k++)
                    {
                        // int target_no = attack.homingTargetNos.RandomChoice();
                        int target_no = attack.lockonTargetNos[k % attack.lockonCount];

                        // Pack target fighters to array to activate your skill.
                        targets[k] = ParticipantManager.I.fighterInfos[target_no].body;

                        // Pack target fighter's number to array.
                        target_nos[k] = target_no;
                    }
                }
                // for (int k = 0; k < raven_count; k++)
                // {
                //     weapons_this_time[k].Activate(targets[k]);
                // }
                StartCoroutine(activator(weapons_this_time, targets));

                // Send Rpc to your clones.
                if (IsHost)
                {
                    attack.SkillActivatorClientRpc(NetworkManager.Singleton.LocalClientId, skillNo, target_nos);
                }
                else
                {
                    attack.SkillActivatorServerRpc(NetworkManager.Singleton.LocalClientId, skillNo, target_nos);
                }
            }
            else
            {
                // Receive Rpc from the owner.
                for (int k = 0; k < raven_count; k++)
                {
                    // Convert received target numbers to fighter-body.
                    if (received_targetNos[k] != -1)
                    {
                        targets[k] = ParticipantManager.I.fighterInfos[(int)received_targetNos[k]].body;
                    }
                }
                // for (int k = 0; k < raven_count; k++)
                // {
                //     weapons_this_time[k].Activate(targets[k]);
                // }
                StartCoroutine(activator(weapons_this_time, targets));
            }
        }

        // Solo Player.
        else
        {
            // Activate your own skill.
            if (attack.lockonCount > 0)
            {
                for (int k = 0; k < raven_count; k++)
                {
                    // int target_no = attack.homingTargetNos.RandomChoice();
                    int target_no = attack.lockonTargetNos[k % attack.lockonCount];
                    targets[k] = ParticipantManager.I.fighterInfos[target_no].body;
                }
            }
            // for (int k = 0; k < raven_count; k++)
            // {
            //     weapons_this_time[k].Activate(targets[k]);
            // }
            StartCoroutine(activator(weapons_this_time, targets));
        }
    }

    IEnumerator activator(Weapon[] weapons, GameObject[] targets)
    {
        const float interval = 0.05f;
        for (int k = 0; k < raven_count; k++)
        {
            weapons[k].Activate(targets[k]);
            yield return new WaitForSeconds(interval);
        }
    }
}