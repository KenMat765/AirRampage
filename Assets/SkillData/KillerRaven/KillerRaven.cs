using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        for(int k = 0; k < raven_count; k++)
        {
            var angle = k * 2*Mathf.PI /raven_count;
            var euler_angle = angle * 180/Mathf.PI;
            var raven_localPos = new Vector3(r * Mathf.Cos(angle), r * Mathf.Sin(angle), 0);
            SetPrefabLocalTransforms(k, raven_localPos, new Vector3(-euler_angle, 90, -90), new Vector3(0.4f, 0.4f, 0.4f));
        }
        GeneratePrefabs(raven_count);
    }

    public override void Activator(string infoCode = null)
    {
        base.Activator();
        MeterDecreaser();
        
        // 今回の発射で使用するweapons
        Weapon[] weapons_this_time = new Weapon[raven_count];
        int[] ready_indexes = GetPrefabIndexes(raven_count);
        for(int k = 0; k < raven_count; k++) weapons_this_time[k] = weapons[ready_indexes[k]];

        GameObject[] targets = new GameObject[raven_count];

        // Multi Players.
        if(BattleInfo.isMulti)
        {
            if(attack.IsOwner)
            {
                // Activate your own skill.
                if(attack.homingTargets.Count > 0)
                {
                    for(int k = 0; k < raven_count; k ++)
                    {
                        targets[k] = attack.homingTargets.RandomChoice();
                    }
                }
                for (int k = 0; k < raven_count; k++)
                {
                    weapons_this_time[k].Activate(targets[k]);
                }

                // Send Rpc to your clones.
                string targetNoCode = TargetNosEncoder(targets);
                attack.SkillActivatorServerRpc(OwnerClientId, skillNo, targetNoCode);
            }
            else
            {
                // Receive Rpc from the owner.
                if(infoCode != null)
                {
                    targets = TargetNosDecoder(infoCode);
                }
                for (int k = 0; k < raven_count; k++)
                {
                    weapons_this_time[k].Activate(targets[k]);
                }
            }
        }

        // Solo Player.
        else
        {
            // Activate your own skill.
            if (attack.homingTargets.Count > 0)
            {
                for (int k = 0; k < raven_count; k++)
                {
                    targets[k] = attack.homingTargets.RandomChoice();
                }
            }
            for (int k = 0; k < raven_count; k++)
            {
                weapons_this_time[k].Activate(targets[k]);
            }
        }
    }
}