using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RepairDevice : SkillHeal
{
    // スキルレベルによって変更の可能性があるパラメータ
    float repair_amount;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        repair_amount = levelData.RepairAmount;
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

        // Decreaser must be called from the owner of this fighter only, because HP is linked among all clients.
        if (!skillController.IsOwner) return;

        base.Activator();
        MeterDecreaser();
        skillController.fighterCondition.HPDecreaser(-repair_amount);

        NetworkManager nm = NetworkManager.Singleton;
        if (nm.IsHost)
            skillController.SkillActivatorClientRpc(nm.LocalClientId, skillNo);
        else
            skillController.SkillActivatorServerRpc(nm.LocalClientId, skillNo);
    }

    public override void ForceTermination(bool maintain_charge)
    {
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (!skillController.IsOwner) return;

        base.ForceTermination(maintain_charge);
    }
}
