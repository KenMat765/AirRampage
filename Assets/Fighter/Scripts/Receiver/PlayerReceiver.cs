using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReceiver : Receiver
{
    // Must be called on every clients.
    public override void OnDeath(int destroyerNo, string causeOfDeath)
    {
        base.OnDeath(destroyerNo, causeOfDeath);
    }

    public override void OnWeaponHit(int fighterNo)
    {
        base.OnWeaponHit(fighterNo);

        if (!IsOwner) return;

        // Flash the screen red briefly when weapon hit.
        uGUIMannager.I.ScreenColorSetter(new Color(1, 0, 0, 0.2f));
    }
}
