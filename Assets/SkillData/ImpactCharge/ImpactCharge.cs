using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ImpactCharge : SkillAttack
{
    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        SetPrefabLocalTransform(new Vector3(0, 0, 0.16f), Vector3.zero, new Vector3(0.4f, 0.4f, 0.4f));
        GeneratePrefab();
    }

    public override int[] Activator(int[] received_data = null)
    {
        base.Activator();
        MeterDecreaser();

        GameObject target = null;
        if (skillController.IsOwner)
        {
            int[] target_no = new int[1];
            target_no[0] = -1;

            // Activate your own skill.
            if (attack.lockonCount > 0)
            {
                int targetNo = attack.lockonTargetNos[0];
                target = ParticipantManager.I.fighterInfos[targetNo].body;
                target_no[0] = targetNo;
            }
            weapons[GetPrefabIndex()].Activate(target);
            return target_no;
        }
        else
        {
            // Receive Rpc from the owner.
            if (received_data[0] != -1)
            {
                target = ParticipantManager.I.fighterInfos[received_data[0]].body;
            }
            weapons[GetPrefabIndex()].Activate(target);
            return null;
        }
    }
}
