using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoMovement : Movement
{
    Transform trans;
    float rotationSpeed = 1.5f;
    public Transform array_point { get; set; }

    protected override void Awake()
    {
        base.Awake();

        // Cache the Transform of zakos since there are bunch of them. (GetComponent is called inside transform)
        trans = transform;
    }

    protected override void FixedUpdate()
    {
        if (!IsOwner) return;
        if (fighterCondition.isDead) return;
        base.FixedUpdate();
    }


    protected override void Rotate()
    {
        Vector3 diff_pos = array_point.position - trans.position;
        Vector3 relativeEulerAngle = Quaternion.LookRotation(diff_pos == Vector3.zero ? trans.forward : diff_pos).eulerAngles;
        float relativeYAngle = Vector3.SignedAngle(trans.forward, diff_pos, Vector3.up);
        Quaternion lookRotation = Quaternion.Euler(relativeEulerAngle.x, relativeEulerAngle.y, Mathf.Clamp(-relativeYAngle * 1.5f, -MAX_TILT_Z, MAX_TILT_Z));
        trans.rotation = Quaternion.Slerp(trans.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }


    protected override void OnDeath(int killed_no, int killer_no, Team killed_team, string cause_of_death)
    {
        base.OnDeath(killed_no, killer_no, killed_team, cause_of_death);
        Controllable(false);
    }
}
