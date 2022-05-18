using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoCondition : FighterCondition
{
    protected override void Awake()
    {
        // base.Start()内でspeedの初期化が行われるので、それよりも前にdefault_speedをセットする
        DefaultSpeedSetter(2.5f);
        base.Awake();
    }



    public override float default_HP {get; set;} = 5;
    public override float revivalTime {get; set;} = 3;
}
