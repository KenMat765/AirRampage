using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Shuriken : SkillAttack
{
    // スキルレベルによって変更の可能性があるパラメータ
    int shuriken_count;
    float lifespan;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        shuriken_count = levelData.WeaponCount;
        lifespan = levelData.Lifespan;
    }

    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        InitTransformsLists(shuriken_count);
        for (int k = 0; k < shuriken_count; k++) SetPrefabLocalTransforms(k, Vector3.zero, new Vector3(Random.value * 360, Random.value * 360, Random.value * 360), new Vector3(4, 4, 4));
        GeneratePrefabs(shuriken_count);
    }

    public override void Activator(int[] transfer = null)
    {
        base.Activator();
        MeterDecreaser(lifespan);

        // 今回の発射で使用するweapons
        Weapon[] weapons_this_time = new Weapon[shuriken_count];
        int[] ready_indexes = GetPrefabIndexes(shuriken_count);
        for (int k = 0; k < shuriken_count; k++) weapons_this_time[k] = weapons[ready_indexes[k]];
        foreach (Weapon weapon in weapons_this_time) weapon.Activate(gameObject);

        // Send Rpc to your clones.
        if (skillController.IsOwner)
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm.IsHost)
                skillController.SkillActivatorClientRpc(nm.LocalClientId, skillNo);
            else
                skillController.SkillActivatorServerRpc(nm.LocalClientId, skillNo);
        }
    }
}
