using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class OffenceTuner : SkillAssist
{
    // スキルレベルによって変更の可能性があるパラメータ
    int power_grade = 2;
    float duration = 20;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        power_grade = levelData.PowerGrade;
        duration = levelData.PowerDuration;
    }

    ParticleSystem effect;
    AudioSource audioSource;

    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
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
        if (BattleInfo.isMulti && !attack.IsOwner) return;

        base.Activator();
        MeterDecreaser(duration);
        attack.fighterCondition.power.Grade(power_grade, duration);
        if (BattleInfo.isMulti)
        {
            if (IsHost) attack.SkillActivatorClientRpc(NetworkManager.Singleton.LocalClientId, skillNo);
            else attack.SkillActivatorServerRpc(NetworkManager.Singleton.LocalClientId, skillNo);
        }
    }

    public override void ForceTermination(bool maintain_charge)
    {
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (BattleInfo.isMulti && !attack.IsOwner) return;

        base.ForceTermination(maintain_charge);
    }
}
