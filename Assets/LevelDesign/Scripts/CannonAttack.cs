using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonAttack : Attack
{
    // Set from Inspector.
    [Header("Cannon Settings")]
    [SerializeField] float rotationSpeed;
    [SerializeField] float rapidInterval;
    [SerializeField] Gradient bulletRed, bulletBlue;

    // This if DEATH_NORMAL_BLAST for fighters, but change this to SPECIFIC_DEATH_CANNON for cannons.
    protected override string causeOfDeath { get; set; } = FighterCondition.SPECIFIC_DEATH_CANNON;

    // DO NOT call base.Awake (You need to change bullet properties between getting condition and pooling bullets)
    protected override void Awake()
    {
        // Get cannon condition.
        CannonCondition cannonCondition = GetComponentInParent<CannonCondition>();
        Team team = cannonCondition.team;

        // Set fighterCondition properties here, because ParticipantManager does not set for cannons.
        fighterCondition = cannonCondition;
        fighterCondition.fighterNo.Value = CannonCondition.CANNON_NO;
        fighterCondition.fighterName.Value = CannonCondition.CANNON_NAME;
        fighterCondition.fighterTeam.Value = team;

        // Set bullet color & layer. (Do this before pooling bullets)
        string layer_name;
        Gradient bullet_color;
        if (team == Team.RED)
        {
            layer_name = "RedBullet";
            bullet_color = bulletRed;
        }
        else
        {
            layer_name = "BlueBullet";
            bullet_color = bulletBlue;
        }
        originalNormalBullet.layer = LayerMask.NameToLayer(layer_name);
        ParticleSystem.MainModule origin_main = originalNormalBullet.GetComponent<Weapon>().parent_particle.main;
        origin_main.startColor = bullet_color;

        // Pool normal bullets after setting color & layer of original bullet.
        PoolNormalBullets(5);

        // Set blast interval from Inspector. (It does not change by ability)
        blastInterval = rapidInterval;

        // Set attackable by yourself. (It is not set from ParticipantManager)
        attackable = true;
    }

    void FixedUpdate()
    {
        if (!attackable) return;

        // Only the owner (= host) executes the following processes.
        if (!IsHost) return;

        // Normal Blast. ////////////////////////////////////////////////////////////////////////////////////////
        if (blastTimer > 0) blastTimer -= Time.deltaTime;
        else
        {
            SetLockonTargetNos();
            if (lockonCount > 0)
            {
                blastTimer = blastInterval;
                int rapid_count = 3;
                NormalRapid(rapid_count);
            }
        }

        // Muzzle Rotation. ////////////////////////////////////////////////////////////////////////////////////////
        if (lockonCount > 0)
        {
            // Determine target.
            int targetNo = lockonTargetNos[0];
            GameObject target = ParticipantManager.I.fighterInfos[targetNo].body;

            // Rotate muzzle toward target.
            Transform trans = transform;
            Vector3 relative_pos = target.transform.position - trans.position;
            Quaternion look_rotation = Quaternion.LookRotation(relative_pos);
            trans.rotation = Quaternion.Slerp(trans.rotation, look_rotation, rotationSpeed * Time.deltaTime);
        }
    }



    // For Debug ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void OnDrawGizmos()
    {
        Transform trans = transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(trans.position, lockonDistance);
    }
}
