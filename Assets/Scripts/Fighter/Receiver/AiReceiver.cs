using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class AiReceiver : Receiver
{
    void Update()
    {
        if(BattleInfo.isMulti && !IsHost) return;

        if(!underAttack)
        {
            if(hitTimer > 0) hitTimer -= Time.deltaTime;
            else hitBulletCount = 0;
        }
        else
        {
            if(hitTimer > 0)
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
    public override void OnDeath()
    {
        string destroyer_name = lastShooterName.Value.ToString();
        string my_name = gameObject.transform.parent.name;

        Color arrowColor;
        if(fighterCondition.team == Team.Red) arrowColor = Color.blue;
        else arrowColor = Color.red;

        Sprite skill_sprite;
        if(lastSkillName.Value == "NormalBlast") skill_sprite = null;
        else skill_sprite = SkillDatabase.I.SearchSkillByName(lastSkillName.Value.ToString()).GetSprite();

        uGUIMannager.I.BookRepo(destroyer_name, my_name, arrowColor, skill_sprite);

        underAttack = false;
    }


    // Damage ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // Link these strings, because all clients uses these to show destroy repo when killed.
    NetworkVariable<FixedString32Bytes> lastShooterName = new NetworkVariable<FixedString32Bytes>();
    NetworkVariable<FixedString32Bytes> lastSkillName = new NetworkVariable<FixedString32Bytes>();

    // Damage is called only from every clones.
    // Filtering is neccesarry in order to be called only from the owner of this fighter (= Host).
    public override void Damage(Weapon weapon)
    {
        base.Damage(weapon);

        if(BattleInfo.isMulti && !IsHost) return;

        lastShooterName.Value = weapon.owner.transform.root.name;
        lastSkillName.Value = weapon.skill_name;

        // Shooterを検知
        if(!underAttack)
        {
            hitTimer = 5;
            hitBulletCount ++;
            if(hitBulletCount > 10)
            {
                underAttack = true;
                currentShooter = weapon.owner;
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
    public bool underAttack {get; private set;}
    public GameObject currentShooter {get; private set;}
    public Vector3 shooterPos {get; private set;}
    public Vector3 relativeSPos {get; private set;}
    public float relativeSAngle {get; private set;}

    int hitBulletCount;
    float hitTimer;

    void UpdateShooterPosition()
    {
        shooterPos = currentShooter.transform.position;
        relativeSPos = shooterPos - transform.position;
        relativeSAngle = Vector3.SignedAngle(transform.forward, relativeSPos, Vector3.up);
    }
}
