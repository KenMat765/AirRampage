using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : Attack
{
    // Blasts {rapidCount} bullets in {setInterval} seconds.
    public override float setInterval { get; set; } = 0.6f;
    protected override int rapidCount { get; set; } = 3;

    // Blast direction control.
    [SerializeField] float blastRange = 30;
    [SerializeField] float sensitivity = 0.2f;
    Quaternion muzzleRot;

    void FixedUpdate()
    {
        if (!attackable) return;

        // Only the owner needs homing target nos.
        if (!IsOwner) return;

        SetHomingTargetNos();

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.Space))
#else
        if (uGUIMannager.onBlast)
#endif
        // if (uGUIMannager.onBlast)
        {
            // Determine blast direction.
            int k = 30;
            Vector2 diff_pos = uGUIMannager.normBlastDiffPos;
            float target_xAngle = Utilities.R2R(-diff_pos.y, 0, blastRange, Utilities.FunctionType.linear, k);
            float target_yAngle = Utilities.R2R(diff_pos.x, 0, blastRange, Utilities.FunctionType.linear, k);
            Quaternion targetRot = Quaternion.Euler(target_xAngle, target_yAngle, 0);
            muzzleRot = Quaternion.Slerp(muzzleRot, targetRot, sensitivity);

            blastTimer -= Time.deltaTime;
            if (blastTimer < 0)
            {
                // Set timer.
                blastTimer = setInterval;

                // Determine target.
                int targetNo = -1;
                if (homingCount > 0) targetNo = homingTargetNos[0];
                GameObject target = null;
                if (targetNo != -1) target = ParticipantManager.I.fighterInfos[targetNo].body;

                // Blast normal bullets for yourself.
                // NormalRapid(rapidCount, target); // Pre-Homing
                NormalRapid(rapidCount, null); // No Pre-Homing

                // If multiplayer, send to all clones to blast bullets.
                // NormalRapidServerRpc(OwnerClientId, rapidCount, targetNo); // Pre-Homing
                NormalRapidServerRpc(OwnerClientId, rapidCount, -1); // No Pre-Homing
            }
        }
        else
        {
            blastTimer = setInterval;
            muzzleRot = transform.rotation;
        }
    }

    protected override void NormalBlast(GameObject target = null)
    {
        if (!attackable) return;

        Weapon bullet = normalWeapons[GetNormalBulletIndex()];

        // Change rotation of bullet
        if (target == null)
        {
            bullet.transform.localRotation = muzzleRot;
        }

        blastImpact.Play();
        blastSound.Play();
        bullet.Activate(target);
    }

    public override void OnDeath()
    {
        TerminateAllSkills();
    }



    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, homingDist);
    }
}
