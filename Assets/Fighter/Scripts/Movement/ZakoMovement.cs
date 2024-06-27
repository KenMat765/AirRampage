using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoMovement : Movement
{
    ZakoCondition zakoCondition;
    ZakoAttack zakoAttack;
    public Transform array_point { get; set; }

    protected override void Awake()
    {
        base.Awake();
        rotationSpeed = 1.5f;
        zakoCondition = (ZakoCondition)fighterCondition;
        zakoAttack = (ZakoAttack)fighterCondition.attack;
    }

    protected override void FixedUpdate()
    {
        if (BattleInfo.isMulti && !IsHost) return;
        base.FixedUpdate();
    }


    protected override void Rotate()
    {
        Vector3 diff_pos = array_point.position - transform.position;
        Vector3 relativeEulerAngle = Quaternion.LookRotation(((diff_pos == Vector3.zero) ? transform.forward : diff_pos)).eulerAngles;
        float relativeYAngle = Vector3.SignedAngle(transform.forward, diff_pos, Vector3.up);
        Quaternion lookRotation = Quaternion.Euler(relativeEulerAngle.x, relativeEulerAngle.y, Mathf.Clamp(-relativeYAngle * 1.5f, -maxTiltZ, maxTiltZ));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }


    public override void OnDeath()
    {
        base.OnDeath();
        Controllable(false);
    }
}
