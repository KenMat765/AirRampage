using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerAttack : Attack
{
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Set isBlasting true if pressing blast button.
        // When multiplayer, only the owner set its own isBlasting true, and send a RPC to set isBlasting true at every other clones.
        if(uGUIMannager.onBlast)
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
        // Set isBlasting false if not pressing blast button.
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

    public override void OnDeath()
    {
        foreach(Skill skill in skills) if(skill != null) skill.ForceTermination();
    }

    public override float homingAngle {get; set;} = 20;
    public override float homingDist {get; set;} = 20;
    protected override float normalInterval {get; set;} = 0.16f;
}
