using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ZakoMovement : Movement
{
    List<Vector3> allSubTargetPos;
    ZakoAttack zakoAttack;

    protected override void Awake()
    {   
        base.Awake();
        rotationSpeed = 1.5f;
        zakoAttack = (ZakoAttack)fighterCondition.attack;
    }

    void Start()
    {
        allSubTargetPos = SubTarget.sub_targets.Select(t => t.transform.position).ToList();
    }

    protected override void FixedUpdate()
    {
        if(BattleInfo.isMulti && !IsHost) return;

        base.FixedUpdate();

        if(fighterCondition.isDead) return;

        if(zakoAttack.homingTargets.Count > 0)
        {
            Vector3 target = zakoAttack.homingTargets[0].transform.position;
            TargetSetter(target, true);
        }
        else
        {
            if(mainArrived) TargetSetter(allSubTargetPos[Random.Range(0, allSubTargetPos.Count)], true);
            else if(subArrived) TargetSetter(allSubTargetPos[Random.Range(0, allSubTargetPos.Count)], false);
        }

        ArrivalJudge();
    }



    float relativeYAngle;

    protected override void UpdateTrans()
    {
        base.UpdateTrans();
        relativePos = targetPos - myPos;
        relativeYAngle = (relativeSubPos == null)? (Vector3.SignedAngle(transform.forward * uTurndirection, relativePos, Vector3.up)) : (Vector3.SignedAngle(transform.forward * uTurndirection, relativeSubPos.GetValueOrDefault(), Vector3.up));
        relativeSubPos = (subTargetPos == null)? (null) : (subTargetPos - myPos);
    }

    protected override void Rotate()
    {
        Vector3 relativeEulerAngle = (subTargetPos == null)? (Quaternion.LookRotation(((relativePos == Vector3.zero)? transform.forward : relativePos)*uTurndirection).eulerAngles) : (Quaternion.LookRotation(((relativeSubPos.GetValueOrDefault() == Vector3.zero)? transform.forward : relativeSubPos.GetValueOrDefault())*uTurndirection).eulerAngles);
        Quaternion lookRotation = Quaternion.Euler(relativeEulerAngle.x, relativeEulerAngle.y, Mathf.Clamp(-relativeYAngle * 1.5f, -maxTiltZ, maxTiltZ));
        transform.rotation = Quaternion.Slerp(myRot, lookRotation, rotationSpeed * Time.deltaTime);
    }
}
