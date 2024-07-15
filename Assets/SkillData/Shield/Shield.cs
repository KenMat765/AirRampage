using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Shield : SkillAssist
{
    // スキルレベルによって変更の可能性があるパラメータ
    public float shield_durability { get; private set; }
    float exhaust_speed;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        shield_durability = levelData.FreeFloat1;
        exhaust_speed = levelData.FreeFloat2;
    }

    Collider col;
    ShieldHitDetector hit_detector;

    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        SetPrefabLocalTransform(new Vector3(0, 0, 0.025f), Vector3.zero, new Vector3(10, 10, 10));
        GeneratePrefab();
        col = GetComponent<Collider>();
        hit_detector = prefabs[0].GetComponent<ShieldHitDetector>();
    }

    public override int[] Activator(int[] received_data = null)
    {
        col.enabled = false;

        // Activate shield.
        hit_detector.ShieldActivator(shield_durability, exhaust_speed);

        if (skillController.IsOwner)
        {
            base.Activator();
        }

        return null;
    }

    public override void EndProcess()
    {
        col.enabled = true;
        hit_detector.DestroyShield();
        if (skillController.IsOwner)
        {
            ulong owner_id = NetworkManager.Singleton.LocalClientId;
            if (skillController.IsHost)
                skillController.SkillEndProcessClientRpc(owner_id, skillNo);
            else
                skillController.SkillEndProcessServerRpc(owner_id, skillNo);
        }
    }

    // Do not call EndProcess in ForceTermination, because ForceTermination is called in all clients, thus EndProcess might be called twice.
    public override void ForceTermination(bool maintain_charge)
    {
        base.ForceTermination(maintain_charge);
        col.enabled = true;
        hit_detector.DestroyShield(immediate: true);
    }
}
