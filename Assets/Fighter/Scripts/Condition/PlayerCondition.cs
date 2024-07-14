using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCondition : FighterCondition
{
    protected override void Start()
    {
        base.Start();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
    }


    public override void HPDecreaser(float deltaHP)
    {
        if (!IsOwner) return;
        base.HPDecreaser(deltaHP);
        HpDecreaser_UIServerRPC(Hp);
    }

    [ServerRpc]
    public void HpDecreaser_UIServerRPC(float curHp)
    {
        float normHp = curHp.Normalize(0, defaultHp);
        HpDecreaser_UIClientRPC(normHp);
    }

    [ClientRpc]
    void HpDecreaser_UIClientRPC(float normHp)
    {
        uGUIMannager.I.HPDecreaser_UI(fighterNo.Value, normHp);
    }


    protected override void OnRevival()
    {
        base.OnRevival();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
        radarIcon.Visualize(true);
    }
}
