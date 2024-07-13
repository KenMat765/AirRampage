using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpiralStorm : SkillDisturb
{
    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        SetPrefabLocalTransform(Vector3.zero, new Vector3(-60, 60, 0), new Vector3(0.5f, 0.5f, 0.5f));
        GeneratePrefab();
    }

    public override void Activator(int[] received_targetNos = null)
    {
        base.Activator();
        MeterDecreaser();

        GameObject target = null;

        if (skillExecuter.IsOwner)
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

            // Send Rpc to your clones.
            NetworkManager nm = NetworkManager.Singleton;
            if (nm.IsHost)
                skillExecuter.SkillActivatorClientRpc(nm.LocalClientId, skillNo, target_no);
            else
                skillExecuter.SkillActivatorServerRpc(nm.LocalClientId, skillNo, target_no);
        }
        else
        {
            // Receive Rpc from the owner.
            if (received_targetNos[0] != -1)
            {
                target = ParticipantManager.I.fighterInfos[(int)received_targetNos[0]].body;
            }
            weapons[GetPrefabIndex()].Activate(target);
        }
    }
}