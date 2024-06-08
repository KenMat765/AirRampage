using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiCondition : FighterCondition
{
    public override float my_cp { get; set; } = 1000;

    protected override void Start()
    {
        base.Start();
        CPStart();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);

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


    public override float revivalTime { get; set; } = 7;
    protected override void Revival()
    {
        base.Revival();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
        radarIcon.Visualize(true);
    }
}
