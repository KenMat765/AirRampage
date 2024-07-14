using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpeedTuner : SkillAssist
{
    // スキルレベルによって変更の可能性があるパラメータ
    int speed_grade = 2;
    float duration = 20;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        speed_grade = levelData.SpeedGrade;
        duration = levelData.SpeedDuration;
    }

    ParticleSystem effect;
    AudioSource audioSource;

    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        SetPrefabLocalTransform(Vector3.zero, new Vector3(-90, 0, 0), new Vector3(0.035f, 0.035f, 0.035f));
        GeneratePrefab();
        effect = prefabs[0].GetComponent<ParticleSystem>();
        audioSource = prefabs[0].GetComponent<AudioSource>();
    }

    public override void Activator(int[] transfer = null)
    {
        effect.Play();
        audioSource.Play();

        // Grader must be called from the owner of this fighter only.
        if (!skillController.IsOwner) return;

        base.Activator();
        MeterDecreaser(duration);
        skillController.fighterCondition.speed.Grade(speed_grade, duration);

        NetworkManager nm = NetworkManager.Singleton;
        if (nm.IsHost)
            skillController.SkillActivatorClientRpc(nm.LocalClientId, skillNo);
        else
            skillController.SkillActivatorServerRpc(nm.LocalClientId, skillNo);
    }

    public override void ForceTermination(bool maintain_charge)
    {
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (BattleInfo.isMulti && !skillController.IsOwner) return;

        base.ForceTermination(maintain_charge);
    }
}
