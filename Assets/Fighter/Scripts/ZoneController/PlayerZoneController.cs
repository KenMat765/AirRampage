using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerZoneController : ZoneController
{
    protected override void Start()
    {
        base.Start();
        uGUIMannager.I.default_combo_disp_timer = comboTimeout;
    }

    protected override void UpdateCp()
    {
        if (uGUIMannager.I.animating_zone) return;
        base.UpdateCp();
    }

    protected override void OnKill(int killed_no)
    {
        if (!attack.IsOwner) return;

        float cp_obtained;
        if (killed_no < 0)
            return;
        else if (killed_no < GameInfo.MAX_PLAYER_COUNT)
            cp_obtained = GameInfo.CP_FIGHTER;
        else
            cp_obtained = GameInfo.CP_ZAKO;

        IncrementCombo();
        float cp_bonus = CalculateCpBonus(combo);
        cp += cp_obtained * cp_bonus;

        uGUIMannager.I.BookCombo(combo, cp_bonus);
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
}
