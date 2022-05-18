using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
        InitTransformsLists(crown_count);
        const float r = 0.18f;
        for(int k = 0; k < crown_count; k++)
        {
            var angle = Mathf.PI/2 + k*2*Mathf.PI/crown_count;
            var crown_localPos = new Vector3(r * Mathf.Cos(angle), r * Mathf.Sin(angle), 0);
            SetPrefabLocalTransforms(k, crown_localPos, Vector3.zero, Vector3.zero);
        }
        GeneratePrefabs(crown_count);
    }

    public override void Activator(string infoCode = null)
    {
        base.Activator();
        MeterDecreaser(interval * crown_count * 2);

        GameObject[] targets = new GameObject[crown_count];

        // Multi Players.
        if(BattleInfo.isMulti)
        {
            if(attack.IsOwner)
            {
                // Activate your own skill.
                if(attack.homingTargets.Count > 0)
                {
                    for(int k = 0; k < crown_count; k ++)
                    {
                        targets[k] = attack.homingTargets.RandomChoice();
                    }
                }
                StartCoroutine(activator(targets));

                // Send Rpc to your clones.
                string targetNoCode = TargetNosEncoder(targets);
                ActivatorServerRpc(OwnerClientId, targetNoCode);
            }
            else
            {
                // Receive Rpc from the owner.
                if(infoCode != null)
                {
                    targets = TargetNosDecoder(infoCode);
                }
                StartCoroutine(activator(targets));
            }
        }

        // Solo Player.
        else
        {
            // Activate your own skill.
            if(attack.homingTargets.Count > 0)
            {
                for(int k = 0; k < crown_count; k ++)
                {
                    targets[k] = attack.homingTargets.RandomChoice();
                }
            }
            StartCoroutine(activator(targets));
        }
    }

    IEnumerator activator(GameObject[] targets)
    {
        // 今回の発射で使用するweapons
        Weapon[] weapons_this_time = new Weapon[crown_count];
        int[] ready_indexes = GetPrefabIndexes(crown_count);
        for(int k = 0; k < crown_count; k++) weapons_this_time[k] = weapons[ready_indexes[k]];

        // Blast bullets.
        for (int k = 0; k < crown_count; k++)
        {
            weapons_this_time[k].Activate(targets[k]);
            yield return new WaitForSeconds(interval);
        }
    }

    protected override System.Func<float> StayMotionGenerator(GameObject prefab)
    {
        System.Func<float> motion;
        motion = () =>
        {
            prefab.transform.localScale = Vector3.zero;
            prefab.transform.DOScale(new Vector3(0.07f, 0.07f, 0.07f), grow_duration);
            return interval * crown_count;
        };
        return motion;
    }
}