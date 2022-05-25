using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : Attack
{
    public override float homingAngle {get; set;} = 20;
    public override float homingDist {get; set;} = 20;
    protected override float setInterval {get; set;} = 0.6f;
    protected override int rapidCount {get; set;} = 3;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if(BattleInfo.isMulti && !IsOwner) return;

        if(uGUIMannager.onBlast)
        {
            blastTimer -= Time.deltaTime;
            if(blastTimer < 0)
            {
                // Set timer.
                blastTimer = setInterval;

                // Determine target.
                int targetNo = -1;
                if(homingCount > 0) targetNo = homingTargetNos[0];

                GameObject target = null;
                if(targetNo != -1) target = ParticipantManager.I.fighterInfos[targetNo].body;

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

    public override void OnDeath()
    {
        foreach(Skill skill in skills) if(skill != null) skill.ForceTermination();
    }
}
