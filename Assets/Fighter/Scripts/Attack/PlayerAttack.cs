using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttack : Attack
{
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
                blastTimer = blastInterval;
                muzzleRot.Value = transform.rotation;
            }

            // Determine blast direction.
            Vector2 diff_pos = uGUIMannager.normBlastDiffPos;
            float target_xAngle = Utilities.R2R(-diff_pos.y, 0, blastAngle, Utilities.FunctionType.linear);
            float target_yAngle = Utilities.R2R(diff_pos.x, 0, blastAngle, Utilities.FunctionType.linear);
            Quaternion targetRot = Quaternion.Euler(target_xAngle, target_yAngle, 0);
            muzzleRot.Value = Quaternion.Slerp(muzzleRot.Value, targetRot, sensitivity);

            blastTimer -= Time.deltaTime;
            if (blastTimer < 0)
            {
                blastTimer = blastInterval;
                int rapid_count = 3;
                NormalRapid(rapid_count, null);
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
}
