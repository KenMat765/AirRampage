using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReceiver : Receiver
{
    public override void OnWeaponHit(int fighterNo)
    {
        base.OnWeaponHit(fighterNo);

        if (!IsOwner) return;

        // Flash the screen red briefly when weapon hit.
        uGUIMannager.I.ScreenColorSetter(new Color(1, 0, 0, 0.2f));
    }
}
