using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiCondition : FighterCondition
{
    protected override void Start()
    {
        base.Start();
        CPStart();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        CPUpdate();
    }


    public override void HPDecreaser(float deltaHP)
    {
        if (!IsOwner) return;
        base.HPDecreaser(deltaHP);
        HpDecreaser_UIServerRPC(Hp);
    }

    protected override void OnDeath(int destroyerNo, string causeOfDeath)
    {
        base.OnDeath(destroyerNo, causeOfDeath);
        ReportDeath(destroyerNo, causeOfDeath);
    }

    protected override void OnRevival()
    {
        base.OnRevival();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
        radarIcon.Visualize(true);
    }
}
