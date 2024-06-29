using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttack : Attack
{
    // Blasts {rapidCount} bullets in {setInterval} seconds.
    public override float setInterval { get; set; } = 0.6f;
    protected override int rapidCount { get; set; } = 3;

    // Blast direction control.
    [SerializeField] float blastRange = 30;
    [SerializeField] float sensitivity = 0.2f;
    NetworkVariable<Quaternion> muzzleRot = new NetworkVariable<Quaternion>(writePerm: NetworkVariableWritePermission.Owner);
    bool isBlasting = false;

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
        {
            // On start blasting (= isBlasting is false)
            if (!isBlasting)
            {
                isBlasting = true;
                blastTimer = setInterval;       // Reset blast timer.
                muzzleRot.Value = transform.rotation; // Reset muzzle rotation.
            }

            // Determine blast direction.
            int k = 30;
            Vector2 diff_pos = uGUIMannager.normBlastDiffPos;
            float target_xAngle = Utilities.R2R(-diff_pos.y, 0, blastRange, Utilities.FunctionType.linear, k);
            float target_yAngle = Utilities.R2R(diff_pos.x, 0, blastRange, Utilities.FunctionType.linear, k);
            Quaternion targetRot = Quaternion.Euler(target_xAngle, target_yAngle, 0);
            muzzleRot.Value = Quaternion.Slerp(muzzleRot.Value, targetRot, sensitivity);

            // Count down blast timer.
            blastTimer -= Time.deltaTime;
            if (blastTimer < 0)
            {
                // Reset timer.
                blastTimer = setInterval;

                // Blast normal bullets for yourself.
                NormalRapid(rapidCount, null);

                // Send to all clones to blast bullets.
                NormalRapidServerRpc(OwnerClientId, rapidCount, -1);

                // === Pre-Homing (Automatically looks at opponent) === //
                // Determine target.
                // int targetNo = -1;
                // if (homingCount > 0) targetNo = homingTargetNos[0];
                // GameObject target = null;
                // if (targetNo != -1) target = ParticipantManager.I.fighterInfos[targetNo].body;
                // NormalRapid(rapidCount, target);
                // NormalRapidServerRpc(OwnerClientId, rapidCount, targetNo);
            }
        }
        else if (isBlasting)
        {
            isBlasting = false;
        }
    }

    protected override void NormalBlast(GameObject target = null)
    {
        if (fighterCondition.isDead) return;
        if (!attackable) return;

        Weapon bullet = normalWeapons[GetNormalBulletIndex()];

        // Change rotation of bullet
        if (target == null)
        {
            bullet.transform.localRotation = muzzleRot.Value;
        }

        blastImpact.Play();
        blastSound.Play();
        bullet.Activate(target);
    }

    public override void OnDeath()
    {
        base.OnDeath();
        TerminateAllSkills();
    }



    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, homingDist);
    }
}
