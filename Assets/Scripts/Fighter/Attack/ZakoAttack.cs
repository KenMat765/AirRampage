using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoAttack : Attack
{
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Set isBlasting true if there are targets.
        // When multiplayer, only the owner set its own isBlasting true, and send a RPC to set isBlasting true at every other clones.
        if(homingTargets.Count > 0)
        {
            if(BattleInfo.isMulti)
            {
                if(IsOwner) SetIsBlastingServerRpc(true);
            }
            else
            {
                isBlasting.Value = true;
            }
        }
        // Set isBlasting false if there are no targets.
        // When multiplayer, only the owner set its own isBlasting false, and send a RPC to set isBlasting false at every other clones.
        else
        {
            if(BattleInfo.isMulti)
            {
                if(IsOwner) SetIsBlastingServerRpc(false);
            }
            else
            {
                isBlasting.Value = false;
            }
        }

        // Blast normal bullets if isBlasting is true.
        if(isBlasting.Value) NormalBlast();
    }

    public override float homingAngle {get; set;} = 10;
    public override float homingDist {get; set;} = 20;
    protected override float normalInterval {get; set;} = 0.75f;
}
