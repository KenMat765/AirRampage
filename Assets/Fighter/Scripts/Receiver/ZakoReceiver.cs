using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoReceiver : Receiver
{
    public override void OnWeaponHit(int fighterNo)
    {
        base.OnWeaponHit(fighterNo);

        if (!IsOwner) return;

        ShakeBody();
    }

    protected override void OnDeath(int killer_no, string cause_of_death)
    {
        base.OnDeath(killer_no, cause_of_death);

        // Report BattleConductor that you are killed. (Only Host)
        if (IsHost)
        {
            BattleConductor.I.OnFighterDestroyed(fighterCondition, killer_no, cause_of_death);
        }
    }
}
