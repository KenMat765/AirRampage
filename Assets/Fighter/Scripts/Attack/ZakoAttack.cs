using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class ZakoAttack : Attack
{
    // setInterval is not used in ZakoAttack.
    // set setInterval to small number in order to stop WaitForSeconds in NormalRapid()
    public override float blastInterval { get; set; } = 0.05f;

    // This if DEATH_NORMAL_BLAST for fighters, but change this to SPECIFIC_DEATH_CANNON for cannons.
    protected override string causeOfDeath { get; set; } = FighterCondition.DEATH_NORMAL_BLAST;

    [SerializeField, MinMaxSlider(0, 3)]
    Vector2 minMaxInterval;

    [Header("Normal Bullet Color")]
    [SerializeField] Gradient bulletRed;
    [SerializeField] Gradient bulletBlue;


    void FixedUpdate()
    {
        if (!attackable) return;

        if (!IsHost) return;

        // Normal Blast. ///////////////////////////////////////////////////////////////////////////////////////
        if (blastTimer > 0) blastTimer -= Time.deltaTime;

        else
        {
            ZakoCondition condition = (ZakoCondition)fighterCondition;
            List<int> fighter_nos = condition.fighterArray.detected_fighters_nos;
            List<int> terminal_nos = condition.fighterArray.detected_terminal_nos;
            if (fighter_nos.Count > 0 || (BattleInfo.rule == Rule.TERMINAL_CONQUEST && terminal_nos.Count > 0))
            {
                // Reset timer.
                // blastTimer = setInterval;
                blastTimer = Random.Range(minMaxInterval[0], minMaxInterval[1]);

                // Determine target.
                int targetNo = -1;
                GameObject target = null;

                // Do not home to terminals.
                if (fighter_nos.Count > 0)
                {
                    targetNo = fighter_nos.RandomChoice();
                    target = ParticipantManager.I.fighterInfos[targetNo].body;
                }

                // Blast normal bullet.
                int rapid_count = 1;
                NormalRapid(rapid_count, target);
                // Send to all clones to blast bullets.
                NormalRapidClientRpc(OwnerClientId, rapid_count, targetNo);
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
