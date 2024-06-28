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

    public override void Combo(float inc_cp)
    {
        // Increase combo. (combo is independent of Zone.)
        combo++;
        const int combo_thresh = 3;
        combo_timer = default_combo_timer;

        // Do not increase cp when Zone.
        if (isZone) return;

        // Increase cp.
        if (combo >= combo_thresh)
        {
            // 3:x1.1, 4:x1.2, ... , 12:x2.0, 13:x2.0
            float cp_magnif = 1 + 0.1f * (combo - combo_thresh + 1);
            cp_magnif = Mathf.Clamp(cp_magnif, 1.0f, 2.0f);
            inc_cp *= cp_magnif;
            // Book combo repo.
            uGUIMannager.I.BookCombo(combo, cp_magnif);
        }
        cp += inc_cp;
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


    public override float revivalTime { get; set; } = 7;
    protected override void OnRevival()
    {
        base.OnRevival();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
        radarIcon.Visualize(true);
    }
}
