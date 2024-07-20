using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttack : Attack
{
    const int RAPID_COUNT = 3;

    // Blast direction control.
    [Header("Blast Direction Control")]
    [SerializeField] float blastMaxAngle = 30;
    [SerializeField] float sensitivity = 0.2f;
    NetworkVariable<Quaternion> muzzleRot = new NetworkVariable<Quaternion>(writePerm: NetworkVariableWritePermission.Owner);
    bool isBlasting = false;

    void FixedUpdate()
    {
        if (!IsOwner) return;
        if (fighterCondition.isDead) return;
        if (!attackable) return;

        SetLockonTargetNos();

        // === Normal Blast === //
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
            float target_xAngle = Utilities.R2R(-diff_pos.y, 0, blastMaxAngle, Utilities.FunctionType.linear);
            float target_yAngle = Utilities.R2R(diff_pos.x, 0, blastMaxAngle, Utilities.FunctionType.linear);
            Quaternion targetRot = Quaternion.Euler(target_xAngle, target_yAngle, 0);
            muzzleRot.Value = Quaternion.Slerp(muzzleRot.Value, targetRot, sensitivity);

            blastTimer -= Time.deltaTime;
            if (blastTimer < 0)
            {
                blastTimer = blastInterval;
                NormalRapid(RAPID_COUNT);
            }
        }
        else if (isBlasting)
        {
            isBlasting = false;
        }
    }

    protected override void NormalBlast(int target_no = -1)
    {
        Weapon bullet = normalWeapons[GetNormalBulletIndex()];
        blastImpact.Play();
        blastSound.Play();
        float fighter_power = fighterCondition.power.value;

        // No lockon target.
        if (target_no < 0)
        {
            // Change rotation of bullet
            bullet.transform.localRotation = muzzleRot.Value;
            bullet.Activate(null, fighter_power);
        }

        // Has lockon target.
        else
        {
            GameObject target = ParticipantManager.I.fighterInfos[target_no].body;
            bullet.Activate(target, fighter_power);
        }
    }
}
