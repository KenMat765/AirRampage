using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class AiReceiver : Receiver
{
    void Update()
    {
        if (BattleInfo.isMulti && !IsHost) return;

        if (!underAttack)
        {
            if (hitTimer > 0) hitTimer -= Time.deltaTime;
            else hitBulletCount = 0;
        }
        else
        {
            if (hitTimer > 0)
            {
                UpdateShooterPosition();
                hitTimer -= Time.deltaTime;
            }
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
    public override void OnDeath(int destroyerNo, string destroyerSkillName)
    {
        base.OnDeath(destroyerNo, destroyerSkillName);

        string my_name = fighterCondition.fighterName.Value.ToString();
        Color arrowColor = Color.gray;
        if (fighterCondition.fighterTeam.Value == Team.RED) arrowColor = Color.blue;
        else if (fighterCondition.fighterTeam.Value == Team.BLUE) arrowColor = Color.red;

        if (destroyerSkillName == "Crystal")
        {
            uGUIMannager.I.BookRepo("Crystal", my_name, arrowColor, null);
            return;
        }

        string destroyer_name = ParticipantManager.I.fighterInfos[destroyerNo].fighterCondition.fighterName.Value.ToString();
        Sprite skill_sprite;
        if (destroyerSkillName == "NormalBlast") skill_sprite = null;
        else skill_sprite = SkillDatabase.I.SearchSkillByName(destroyerSkillName.ToString()).GetSprite();

        uGUIMannager.I.BookRepo(destroyer_name, my_name, arrowColor, skill_sprite);

        underAttack = false;
    }


    // Damage ///////////////////////////////////////////////////////////////////////////////////////////////////////
    public override void OnWeaponHit(int fighterNo)
    {
        base.OnWeaponHit(fighterNo);
        if (!underAttack)
        {
            hitTimer = 5;
            hitBulletCount++;
            if (hitBulletCount > 10)
            {
                underAttack = true;
                currentShooter = ParticipantManager.I.fighterInfos[fighterNo].body;
                hitTimer = 7;
            }
        }
        else
        {
            // currentShooter = weapon.owner;
            hitTimer = 7;
        }
    }


    // Detect Shooter ///////////////////////////////////////////////////////////////////////////////////////////////
    public bool underAttack { get; private set; }
    public GameObject currentShooter { get; private set; }
    public Vector3 shooterPos { get; private set; }
    public Vector3 relativeSPos { get; private set; }
    public float relativeSAngle { get; private set; }

    int hitBulletCount;
    float hitTimer;

    void UpdateShooterPosition()
    {
        shooterPos = currentShooter.transform.position;
        relativeSPos = shooterPos - transform.position;
        relativeSAngle = Vector3.SignedAngle(transform.forward, relativeSPos, Vector3.up);
    }
}
