using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using NaughtyAttributes;

public class ZakoAttack : Attack
{

    [SerializeField, MinMaxSlider(0, 3)]
    Vector2 minMaxInterval;

    [Header("Normal Bullet Color")]
    [SerializeField] Gradient bulletRed;
    [SerializeField] Gradient bulletBlue;


    void FixedUpdate()
    {
        if (!IsOwner) return;
        if (fighterCondition.isDead) return;
        if (!attackable) return;

        // === Normal Blast === //
        if (blastTimer > 0) blastTimer -= Time.deltaTime;

        else
        {
            ZakoCondition condition = (ZakoCondition)fighterCondition;
            List<int> fighter_nos = condition.fighterArray.detected_fighters_nos;
            if (fighter_nos.Count > 0)
            {
                blastTimer = Random.Range(minMaxInterval[0], minMaxInterval[1]);
                int targetNo = fighter_nos.RandomChoice();
                GameObject target = ParticipantManager.I.fighterInfos[targetNo].body;
                int rapid_count = 1;
                NormalRapid(rapid_count, target);
            }
        }
    }


    void ChangeBulletTeam(Team new_team)
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

    [ClientRpc]
    public void ChangeBulletTeamClientRpc(Team new_team) => ChangeBulletTeam(new_team);

}
