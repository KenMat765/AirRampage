using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCondition : FighterCondition
{
    protected override void Start()
    {
        base.Start();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
    }



    public override float default_HP {get; set;} = 100;
    public override void HPDecreaser(float deltaHP)
    {
        base.HPDecreaser(deltaHP);
        HpDecreaser_UIServerRPC(HP);
    }

    public override float revivalTime {get; set;} = 7;
    protected override void Revival()
    {
        base.Revival();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
    }
}
