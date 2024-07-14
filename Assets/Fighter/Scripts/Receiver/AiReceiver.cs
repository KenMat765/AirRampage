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

        // Report BattleConductor that you are killed. (Only Host)
        if (IsHost)
        {
            BattleConductor.I.OnFighterDestroyed(fighterCondition, killer_no, cause_of_death);
        }

        // Send uGUIManger to report death of this fighter.
        string my_name = fighterCondition.fighterName.Value.ToString();
        Team my_team = fighterCondition.fighterTeam.Value;
        if (FighterCondition.IsSpecificDeath(cause_of_death))
        {
            uGUIMannager.I.BookRepo(cause_of_death, my_name, my_team, cause_of_death);
        }
        else
        {
            string destroyer_name = ParticipantManager.I.fighterInfos[attackerNo].fighterCondition.fighterName.Value.ToString();
            uGUIMannager.I.BookRepo(destroyer_name, my_name, my_team, cause_of_death);
        }
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
