using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoAttack : Attack
{
    public override float homingAngle {get; set;} = 10;
    public override float homingDist {get; set;} = 20;
    protected override float setInterval {get; set;} = 1;
    protected override int rapidCount {get; set;} = 1;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();


        // Only the owner (= host) executes the following processes.
        if(BattleInfo.isMulti && !IsHost) return;


        // Normal Blast. ////////////////////////////////////////////////////////////////////////////////////////
        if(homingCount > 0)
        {
            blastTimer -= Time.deltaTime;
            if(blastTimer < 0)
            {
                // Set timer.
                blastTimer = setInterval;

                // Determine target.
                int targetNo = homingTargetNos[0];
                GameObject target = ParticipantManager.I.fighterInfos[targetNo].body;

                // Blast normal bullets for yourself.
                NormalRapid(target, rapidCount);

                // If multiplayer, send to all clones to blast bullets.
                if(BattleInfo.isMulti) NormalRapidServerRpc(OwnerClientId, targetNo, rapidCount);
            }
        }
        else
        {
            blastTimer = 0;
        }
    }
}
