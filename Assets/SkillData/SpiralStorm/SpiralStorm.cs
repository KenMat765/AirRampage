using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiralStorm : SkillDisturb
{
    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
        SetPrefabLocalTransform(Vector3.zero, new Vector3(-60, 60, 0), new Vector3(0.5f, 0.5f, 0.5f));
        GeneratePrefab();
    }

    public override void Activator(string infoCode = null)
    {
        base.Activator();
        MeterDecreaser();

        GameObject target = null;

        // Multi Players.
        if(BattleInfo.isMulti)
        {
            if(attack.IsOwner)
            {
                // Activate your own skill.
                if(attack.homingCount > 0)
                {
                    int targetNo = attack.homingTargetNos[0];
                    target = ParticipantManager.I.fighterInfos[targetNo].body;
                }
                weapons[GetPrefabIndex()].Activate(target);

                // Send Rpc to your clones.
                string targetNoCode = TargetNosEncoder(target);
                attack.SkillActivatorServerRpc(OwnerClientId, skillNo, targetNoCode);
            }
            else
            {
                // Receive Rpc from the owner.
                if(infoCode != null)
                {
                    target = TargetNosDecoder(infoCode)[0];
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