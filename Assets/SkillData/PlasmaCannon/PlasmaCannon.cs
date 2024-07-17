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

    public override int[] Activator(int[] received_data = null)
    {
        base.Activator();
        MeterDecreaser();
        float fighter_power = skillController.fighterCondition.power.value;
        weapons[GetPrefabIndex()].Activate(null, fighter_power);
        return null;
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
