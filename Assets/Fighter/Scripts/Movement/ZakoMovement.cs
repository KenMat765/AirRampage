using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoMovement : Movement
{
    Transform trans;
    public Transform array_point { get; set; }

    protected override void Awake()
    {
        base.Awake();
        rotationSpeed = 1.5f;
        trans = transform;
    }

    protected override void FixedUpdate()
    {
        if (!IsHost) return;
        base.FixedUpdate();
    }


    protected override void Rotate()
    {
        Vector3 diff_pos = array_point.position - trans.position;
        Vector3 relativeEulerAngle = Quaternion.LookRotation(diff_pos == Vector3.zero ? trans.forward : diff_pos).eulerAngles;
        float relativeYAngle = Vector3.SignedAngle(trans.forward, diff_pos, Vector3.up);
        Quaternion lookRotation = Quaternion.Euler(relativeEulerAngle.x, relativeEulerAngle.y, Mathf.Clamp(-relativeYAngle * 1.5f, -maxTiltZ, maxTiltZ));
        trans.rotation = Quaternion.Slerp(trans.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }


    public override void OnDeath()
    {
        base.OnDeath();
        Controllable(false);
    }
}
