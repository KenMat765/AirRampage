﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZakoAttack : Attack
{
    // setInterval is not used in ZakoAttack.
    // set setInterval to zero in order to stop WaitForSeconds in NormalRapid()
    // protected override float setInterval { get; set; } = 1.0f;
    public override float setInterval { get; set; } = 0.05f;
    protected override int rapidCount { get; set; } = 1;

    [Header("Normal Bullet Color")]
    [SerializeField] Gradient bulletRed, bulletBlue;


    void FixedUpdate()
    {
        if (!attackable) return;

        if (BattleInfo.isMulti && !IsHost) return;

        // Normal Blast. ////////////////////////////////////////////////////////////////////////////////////////
        if (blastTimer > 0) blastTimer -= Time.deltaTime;

        else
        {
            ZakoCondition condition = (ZakoCondition)fighterCondition;
            List<int> fighter_nos = condition.fighterArray.detected_fighters_nos;
            List<int> terminal_nos = condition.fighterArray.detected_terminal_nos;
            if (fighter_nos.Count > 0 || (BattleInfo.rule == Rule.TERMINALCONQUEST && terminal_nos.Count > 0))
            {
                // Reset timer.
                // blastTimer = setInterval;
                blastTimer = Random.Range(0.4f, 1.0f);

                // Determine target.
                int targetNo = -1;
                GameObject target = null;

                // Do not home to terminals.
                if (fighter_nos.Count > 0)
                {
                    targetNo = fighter_nos.RandomChoice();
                    target = ParticipantManager.I.fighterInfos[targetNo].body;
                }

                // Blast normal bullets.
                NormalRapid(rapidCount, target);

                // If multiplayer, send to all clones to blast bullets.
                if (BattleInfo.isMulti) NormalRapidClientRpc(OwnerClientId, targetNo, rapidCount);
            }
        }
    }


    public void ChangeBulletTeam(Team new_team)
    {
        if (new_team == Team.NONE)
        {
            Debug.LogError("弾丸のチームにNONEを設定できません!!");
            return;
        }

        string layer_name;
        Gradient bullet_color;
        if (new_team == Team.RED)
        {
            layer_name = "RedBullet";
            bullet_color = bulletRed;
        }
        else
        {
            layer_name = "BlueBullet";
            bullet_color = bulletBlue;
        }

        // Change originals team to.
        originalNormalBullet.layer = LayerMask.NameToLayer(layer_name);
        ParticleSystem.MainModule origin_main = originalNormalBullet.GetComponent<Weapon>().parent_particle.main;
        origin_main.startColor = bullet_color;

        foreach (Weapon weapon in normalWeapons)
        {
            weapon.gameObject.layer = LayerMask.NameToLayer(layer_name);
            ParticleSystem.MainModule main = weapon.parent_particle.main;
            main.startColor = bullet_color;
        }
    }
}