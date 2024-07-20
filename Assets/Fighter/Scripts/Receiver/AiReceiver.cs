using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiReceiver : Receiver
{
    void FixedUpdate()
    {
        if (!IsOwner) return;
        if (fighterCondition.isDead) return;

        if (hitTimer > 0)
        {
            hitTimer -= Time.deltaTime;
        }
        else
        {
            underAttack = false;
            hitBulletCount = 0;
            hitTimer = 0;
        }
    }


    // Must be called on every clients.
    protected override void OnDeath(int killer_no, string cause_of_death)
    {
        base.OnDeath(killer_no, cause_of_death);

        underAttack = false;
        hitBulletCount = 0;
        hitTimer = 0;
    }


    // Damage ///////////////////////////////////////////////////////////////////////////////////////////////////////
    public override void OnWeaponHit(int fighterNo)
    {
        base.OnWeaponHit(fighterNo);

        if (!IsOwner) return;

        // Do nothing when shooter is not fighter.
        if (fighterNo < 0)
        {
            return;
        }

        if (underAttack)
        {
            hitTimer = hitResetTime;
        }
        else
        {
            hitTimer = hitResetTime;
            hitBulletCount++;
            if (hitBulletCount > hitBulletThresh)
            {
                underAttack = true;
                shooterBody = ParticipantManager.I.fighterInfos[fighterNo].body;
                hitTimer = hitResetTime;
            }
        }
    }


    // Shooter Detection ///////////////////////////////////////////////////////////////////////////////////////////////
    [Header("Shooter Detection")]
    [SerializeField, Tooltip("Considered under attack when hit bullet count exceed this value")]
    int hitBulletThresh;

    [SerializeField, Tooltip("Time until the hit bullet count is reset")]
    float hitResetTime;

    public bool underAttack { get; private set; }
    public GameObject shooterBody { get; private set; }
    int hitBulletCount;
    float hitTimer;
}
