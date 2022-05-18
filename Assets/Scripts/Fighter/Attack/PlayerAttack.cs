using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerAttack : Attack
{
    protected NetworkVariable<bool> blasting = new NetworkVariable<bool>();

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if(uGUIMannager.onBlast) NormalBlast();
    }

    public override void OnDeath()
    {
        foreach(Skill skill in skills) if(skill != null) skill.ForceTermination();
    }

    public override float homingAngle {get; set;} = 20;
    public override float homingDist {get; set;} = 20;
    protected override float normalInterval {get; set;} = 0.16f;
}
