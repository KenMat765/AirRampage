using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class AiAttack : Attack
{
    void FixedUpdate()
    {
        if (!attackable) return;
        if (!IsOwner) return;

        // === Normal Blast === //
        if (blastTimer > 0)
        {
            blastTimer -= Time.deltaTime;
        }
        else
        {
            SetLockonTargetNos();
            if (lockonCount > 0)
            {
                blastTimer = blastInterval;
                int targetNo = lockonTargetNos[0];
                GameObject target = ParticipantManager.I.fighterInfos[targetNo].body;
                int rapid_count = 3;
                NormalRapid(rapid_count, target);
            }
        }
    }
}
