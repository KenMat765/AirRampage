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
}
