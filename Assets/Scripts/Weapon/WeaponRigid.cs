using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponRigid : Weapon
{
    Rigidbody rb;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    protected override void OnStartMoving()
    {
        base.OnStartMoving();
        rb.isKinematic = false;
        rb.velocity = transform.forward * speed;
    }

    protected override void OnMoving()
    {
        base.OnMoving();
        if(Vector3.Dot(transform.forward, rb.velocity) != rb.velocity.magnitude) transform.rotation = Quaternion.LookRotation(rb.velocity);
    }

    protected override void PlayHitEffect()
    {
        base.PlayHitEffect();
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
    }

    protected override void KillWeapon()
    {
        base.KillWeapon();
        rb.isKinematic = true;
    }
}
