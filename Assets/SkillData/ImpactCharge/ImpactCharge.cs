using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactCharge : SkillAttack
{   
    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
        SetPrefabLocalTransform(new Vector3(0, 0, 0.16f), Vector3.zero, new Vector3(0.4f, 0.4f, 0.4f));
        GeneratePrefab();
    }

    public override void Activator(int[] received_targetNos = null)
    {
        base.Activator();
        MeterDecreaser();

        GameObject target = null;

        // Multi Players.
        if(BattleInfo.isMulti)
        {
            if(attack.IsOwner)
            {
                // As you need to send an ARRAY through RPC, define an array of size 1.
                int[] target_no = new int[1];
                target_no[0] = -1;

                // Activate your own skill.
                if(attack.homingCount > 0)
                {
                    int targetNo = attack.homingTargetNos[0];
                    target = ParticipantManager.I.fighterInfos[targetNo].body;
                    target_no[0] = targetNo;
                }
                weapons[GetPrefabIndex()].Activate(target);

                // Send Rpc to your clones.
                attack.SkillActivatorServerRpc(OwnerClientId, skillNo, target_no);
            }
            else
            {
                // Receive Rpc from the owner.
                if(received_targetNos[0] != -1)
                {
                    target = ParticipantManager.I.fighterInfos[(int)received_targetNos[0]].body;
                }
                weapons[GetPrefabIndex()].Activate(target);
            }
        }

        // Solo Player.
        else
        {
            // Activate your own skill.
            if(attack.homingCount > 0)
            {
                int targetNo = attack.homingTargetNos[0];
                target = ParticipantManager.I.fighterInfos[targetNo].body;
            }
            weapons[GetPrefabIndex()].Activate(target);
        }
    }
}
