using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCondition : FighterCondition
{
    protected override void Start()
    {
        base.Start();
        CPStart();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
        uGUIMannager.I.default_combo_disp_timer = comboTimeout;
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


    protected override void CPUpdate()
    {
        if (uGUIMannager.I.animating_zone) return;
        base.CPUpdate();
    }

    public override float Combo(float inc_cp)
    {
        float cp_magnif = base.Combo(inc_cp);
        uGUIMannager.I.BookCombo(combo, cp_magnif);
        return cp_magnif;
    }

    protected override void StartZone()
    {
        base.StartZone();
        uGUIMannager.I.StartZoneAnim();
    }

    protected override void EndZone()
    {
        base.EndZone();
        uGUIMannager.I.EndZoneAnim();
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
