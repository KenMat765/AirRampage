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

    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
        SetPrefabLocalTransform(Vector3.zero, new Vector3(-90, 0, 0), new Vector3(0.035f, 0.035f, 0.035f));
        GeneratePrefab();
        effect = prefabs[0].GetComponent<ParticleSystem>();
    }

    public override void Activator(int[] transfer = null)
    {
        effect.Play();

        // Decreaser must be called from the owner of this fighter only, because HP is linked among all clients.
        if (!attack.IsOwner) return;

        base.Activator();
        MeterDecreaser();
        attack.fighterCondition.HPDecreaser(-repair_amount);
        if (IsHost) attack.SkillActivatorClientRpc(NetworkManager.Singleton.LocalClientId, skillNo);
        else attack.SkillActivatorServerRpc(NetworkManager.Singleton.LocalClientId, skillNo);
    }

    public override void ForceTermination()
    {
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (!attack.IsOwner) return;

        base.ForceTermination();
    }
}
