using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiReceiver : Receiver
{
    void Update()
    {
        if (!IsHost) return;

        if (!underAttack)
        {
            if (hitTimer > 0) hitTimer -= Time.deltaTime;
            else hitBulletCount = 0;
        }
        else
        {
            // Keep on updating shooter position when under attack.
            if (hitTimer > 0)
            {
                UpdateShooterPosition();
                hitTimer -= Time.deltaTime;
            }

            // Set underAttack to false if no attacks are received for a certain period.
            else
            {
                underAttack = false;
                hitBulletCount = 0;
                hitTimer = 0;
                shooterPos = Vector3.zero;
                relativeSPos = Vector3.zero;
                relativeSAngle = 0;
            }
        }
    }


    // Must be called on every clients.
    public override void OnDeath(int destroyerNo, string causeOfDeath)
    {
        base.OnDeath(destroyerNo, causeOfDeath);
        underAttack = false;
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

        // If not under attack.
        if (!underAttack)
        {
            // Count up hit bullet count.
            hitTimer = 5;
            hitBulletCount++;

            // If hit bullet count exceeds a certain threshhold, set underAttack to true.
            if (hitBulletCount > 10)
            {
                underAttack = true;
                currentShooter = ParticipantManager.I.fighterInfos[fighterNo].body;
                hitTimer = 7;
            }
        }

        // If already under attack, reset timer.
        else
        {
            hitTimer = 7;
        }
    }


    // Shooter Detection ///////////////////////////////////////////////////////////////////////////////////////////////
    public bool underAttack { get; private set; }
    public GameObject currentShooter { get; private set; }
    public Vector3 shooterPos { get; private set; }     // position of current shooter.
    public Vector3 relativeSPos { get; private set; }   // relative posiiont to current shooter.
    public float relativeSAngle { get; private set; }   // relative y-angle to current shooter.

    int hitBulletCount;
    float hitTimer;

    void UpdateShooterPosition()
    {
        shooterPos = currentShooter.transform.position;
        relativeSPos = shooterPos - transform.position;
        relativeSAngle = Vector3.SignedAngle(transform.forward, relativeSPos, Vector3.up);
    }
}
