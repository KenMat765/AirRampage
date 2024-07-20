using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class AiAttack : Attack
{
    const int RAPID_COUNT = 3;

    void FixedUpdate()
    {
        if (!IsOwner) return;
        if (fighterCondition.isDead) return;
        if (!attackable) return;

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
                int target_no = lockonTargetNos[0];
                NormalRapid(RAPID_COUNT, target_no);
            }
        }
    }
}
