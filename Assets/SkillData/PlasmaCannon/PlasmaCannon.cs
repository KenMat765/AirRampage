using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

public class PlasmaCannon : SkillAttack
{
    // スキルレベルによって変更の可能性があるパラメータ
    float grow_duration;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        grow_duration = levelData.FreeFloat1;
    }

    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        SetPrefabLocalTransform(new Vector3(0, 0, 0.12f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f));
        GeneratePrefab();
    }

    public override void Activator(int[] received_targetNos = null)
    {
        base.Activator();
        MeterDecreaser();

        if (skillController.IsOwner)
        {
            // Activate your own skill. (target is null for this skill)
            weapons[GetPrefabIndex()].Activate(null);

            // Send Rpc to your clones.
            NetworkManager nm = NetworkManager.Singleton;
            if (nm.IsHost)
                skillController.SkillActivatorClientRpc(nm.LocalClientId, skillNo);
            else
                skillController.SkillActivatorServerRpc(nm.LocalClientId, skillNo);
        }
        else
        {
            // Receive Rpc from the owner.
            weapons[GetPrefabIndex()].Activate(null);
        }
    }

    protected override System.Func<float> StayMotionGenerator(GameObject prefab)
    {
        System.Func<float> motion;
        motion = () =>
        {
            prefab.transform.localScale = Vector3.zero;
            prefab.transform.DOScale(new Vector3(0.05f, 0.05f, 0.05f), grow_duration).SetEase(Ease.Linear);
            return grow_duration;
        };
        return motion;
    }
}
