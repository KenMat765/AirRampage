using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoAttack : Attack
{
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // 通常弾発射
        if(homingTargets.Count > 0) NormalBlast();
    }

    public override float homingAngle {get; set;} = 10;
    public override float homingDist {get; set;} = 20;
    protected override float normalInterval {get; set;} = 0.75f;
}
