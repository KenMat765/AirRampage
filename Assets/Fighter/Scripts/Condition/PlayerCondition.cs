using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCondition : FighterCondition
{
    public override float my_cp { get; set; } = 1000;

    protected override void Start()
    {
        base.Start();
        CPStart();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
        uGUIMannager.I.default_combo_disp_timer = default_combo_timer;

        // Abilities
        if (has_quickRepair)
        {
            revivalTime = 4;
        }
    }

    protected override void Update()
    {
        base.Update();
        CPUpdate();
    }


    public override void HPDecreaser(float deltaHP)
    {
        if (!IsOwner) return;
        base.HPDecreaser(deltaHP);
        HpDecreaser_UIServerRPC(HP);
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

    public override float revivalTime { get; set; } = 7;
    protected override void OnRevival()
    {
        base.OnRevival();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
        radarIcon.Visualize(true);
    }
}
