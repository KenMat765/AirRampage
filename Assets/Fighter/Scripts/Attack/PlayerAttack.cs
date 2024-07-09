using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttack : Attack
{
    // Blasts {rapidCount} bullets in {setInterval} seconds.
    public override float blastInterval { get; set; } = 0.6f;

    // This if DEATH_NORMAL_BLAST for fighters, but change this to SPECIFIC_DEATH_CANNON for cannons.
    protected override string causeOfDeath { get; set; } = FighterCondition.DEATH_NORMAL_BLAST;

    // Blast direction control.
    [Header("Blast Direction Control")]
    [SerializeField] float blastAngle = 30;
    [SerializeField] float sensitivity = 0.2f;
    NetworkVariable<Quaternion> muzzleRot = new NetworkVariable<Quaternion>(writePerm: NetworkVariableWritePermission.Owner);
    bool isBlasting = false;

    void FixedUpdate()
    {
        if (!attackable) return;

        // Only the owner needs homing target nos.
        if (!IsOwner) return;

        SetLockonTargetNos();

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
                blastTimer = blastInterval;               // Reset blast timer.
                muzzleRot.Value = transform.rotation;   // Reset muzzle rotation.
            }

            // Determine blast direction.
            Vector2 diff_pos = uGUIMannager.normBlastDiffPos;
            float target_xAngle = Utilities.R2R(-diff_pos.y, 0, blastAngle, Utilities.FunctionType.linear);
            float target_yAngle = Utilities.R2R(diff_pos.x, 0, blastAngle, Utilities.FunctionType.linear);
            Quaternion targetRot = Quaternion.Euler(target_xAngle, target_yAngle, 0);
            muzzleRot.Value = Quaternion.Slerp(muzzleRot.Value, targetRot, sensitivity);

            // Count down blast timer.
            blastTimer -= Time.deltaTime;
            if (blastTimer < 0)
            {
                // Reset timer.
                blastTimer = blastInterval;

                // Blast normal bullets for yourself.
                int rapid_count = 3;
                NormalRapid(rapid_count, null);
                // Send to all clones to blast bullets.
                NormalRapidServerRpc(OwnerClientId, rapid_count, -1);

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
        Gizmos.DrawWireSphere(transform.position, lockonDistance);
    }
}
