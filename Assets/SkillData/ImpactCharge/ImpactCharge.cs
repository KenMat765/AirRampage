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
                if(attack.homingTargets.Count > 0)
                {
                    target = attack.homingTargets[0];
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
            if(attack.homingTargets.Count > 0)
            {
                target = attack.homingTargets[0];
            }
            weapons[GetPrefabIndex()].Activate(target);
        }
    }
}
