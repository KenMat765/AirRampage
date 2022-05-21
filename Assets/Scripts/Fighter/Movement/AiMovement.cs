using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AiMovement : Movement
{
    protected override void Awake()
    {
        base.Awake();

        anim = GetComponentInChildren<Animator>();
        var rac = anim.runtimeAnimatorController;
        uturnTime = rac.animationClips.Where(a => a.name == "U-Turn").Select(b => b.length).ToArray()[0];
        flipTime = rac.animationClips.Where(a => a.name == "Flip").Select(b => b.length).ToArray()[0];
        rollTime = rac.animationClips.Where(a => a.name == "RightRole").Select(b => b.length).ToArray()[0];
        aiReceiver = (AiReceiver)fighterCondition.receiver;
        aiAttack = (AiAttack)fighterCondition.attack;
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
        
        ChangeCondition();
        ActionOnEachCondition();        
        ArrivalJudge();
    }



    public override void OnRevival()
    {
        base.OnRevival();
        if(uTurndirection == -1) uTurndirection = 1;
    }



    protected override void UpdateTrans()
    {
        base.UpdateTrans();
        relativePos = targetPos - myPos;
        relativeYAngle = (relativeSubPos == null)? (Vector3.SignedAngle(transform.forward * uTurndirection, relativePos, Vector3.up)) : (Vector3.SignedAngle(transform.forward * uTurndirection, relativeSubPos.GetValueOrDefault(), Vector3.up));
        relativeSubPos = (subTargetPos == null)? (null) : (subTargetPos - myPos);
    }

    protected override void Rotate()
    {
        Vector3 targetAngle = (subTargetPos == null)? (Quaternion.LookRotation(((relativePos == Vector3.zero)? transform.forward : relativePos)*uTurndirection).eulerAngles) : (Quaternion.LookRotation(((relativeSubPos.GetValueOrDefault() == Vector3.zero)? transform.forward : relativeSubPos.GetValueOrDefault())*uTurndirection).eulerAngles);
        if(relativeYAngle < -135)
        {
            targetAngle.z = ((-maxTiltZ/45)*relativeYAngle - 4*maxTiltZ)*uTurndirection*-1;
        }
        else if(-135 <= relativeYAngle && relativeYAngle <= 135)
        {
            float normY = relativeYAngle/135;
            targetAngle.z = (maxTiltZ/2)*(Mathf.Pow(normY, 3) - 3*normY)*uTurndirection;
        }
        else
        {
            targetAngle.z = ((-maxTiltZ/45)*relativeYAngle + 4*maxTiltZ)*uTurndirection*-1;
        }
        Quaternion lookRotation = Quaternion.Euler(targetAngle);
        transform.rotation = Quaternion.Slerp(myRot, lookRotation, rotationSpeed*Time.deltaTime);
    }

    protected override void FourActionExe()
    {
        if(Vector3.SqrMagnitude((relativeSubPos == null)? relativePos : relativeSubPos.GetValueOrDefault()) >= Mathf.Sqrt(farNearBorder))
        {
            if(relativeYAngle >= -60 && relativeYAngle <= -45)
            {
                LeftRole(3);
                if(BattleInfo.isMulti)
                {
                    LeftRoleClientRpc(OwnerClientId, 3);
                }
            }
            else if(relativeYAngle >= 45 && relativeYAngle <= 60)
            {
                RightRole(3);
                if(BattleInfo.isMulti)
                {
                    RightRoleClientRpc(OwnerClientId, 3);
                }
            }
            else if(relativeYAngle <= -135 || relativeYAngle >= 135)
            {
                Uturn();
                if(BattleInfo.isMulti)
                {
                    UturnClientRpc(OwnerClientId);
                }
            }
            else return;
        }
        else
        {
            if(relativeYAngle <= -135 || relativeYAngle >= 135)
            {
                Uturn();
                if(BattleInfo.isMulti)
                {
                    UturnClientRpc(OwnerClientId);
                }
            }
            else return;
        }
    }



    // AI Controll /////////////////////////////////////////////////////////////////////////////////////////////////////////
    enum Conditions { attack, search, goBack, farEscape, flip, counter }
    Conditions condition = Conditions.search;
    GameObject targetCraft;    //MainTarget, SubTarget両方に使用。常に存在する。
    float relativeYAngle;
    List<Vector3> allSubTargetPos;
    AiReceiver aiReceiver;
    AiAttack aiAttack;

    void ChangeCondition() 
    {
        // 攻撃されている時
        if(aiReceiver.underAttack)
        {
            // 現在の標的を削除
            targetCraft = null;

            GameObject shooter = aiReceiver.currentShooter;
            Vector3 relativePos = aiReceiver.relativeSPos;
            float relativeAngle = aiReceiver.relativeSAngle;

            // Shooterが生きている時
            if(shooter.activeSelf == true)
            {
                if(relativeAngle >= -30 && relativeAngle <= 30) condition = Conditions.counter;
                else if(relativeAngle <= -135 || relativeAngle >= 135)
                {
                    if(Vector3.SqrMagnitude(relativePos) < farNearBorder) condition = Conditions.flip;
                    else condition = Conditions.farEscape;
                }
                else condition = Conditions.goBack;
            }

            // Shooterが死んでいる時
            else condition = Conditions.search;
        }

        // 攻撃されていない時
        else
        {
            if(aiAttack.homingCount > 0)
            {
                condition = Conditions.attack;
                if(targetCraft == null)
                {
                    int targetNo = aiAttack.homingTargetNos[0];
                    targetCraft = ParticipantManager.I.fighterInfos[targetNo].body;
                }
            }
            else
            {
                condition = Conditions.search;
                targetCraft = null;
            }
        }
    }

    void ActionOnEachCondition()
    {
        switch(condition)
        {
            // 攻撃されていない時
            case Conditions.attack:
                rotationSpeed = Mathf.Lerp(rotationSpeed, 0.8f, 0.1f);
                if(targetCraft != null) TargetSetter(targetCraft.transform.position, true);
            break;

            case Conditions.search:
                rotationSpeed = Mathf.Lerp(rotationSpeed, 0.8f, 0.05f);
                if(mainArrived) TargetSetter(allSubTargetPos[Random.Range(0, allSubTargetPos.Count)], true);
                else if(subArrived) TargetSetter(allSubTargetPos[Random.Range(0, allSubTargetPos.Count)], false);
            break;

            // 攻撃されている時
            case Conditions.goBack:
                TargetSetter(aiReceiver.shooterPos + aiReceiver.currentShooter.transform.forward * -farNearBorder, true);
                rotationSpeed = Mathf.Lerp(rotationSpeed, 0.5f, 0.1f);
            break;

            case Conditions.farEscape:
                rotationSpeed = Mathf.Lerp(rotationSpeed, 0.8f, 0.1f);
                if(mainArrived)
                {
                    TargetSetter(allSubTargetPos[Random.Range(0, allSubTargetPos.Count)], true);
                }
                else if(subArrived)
                {
                    TargetSetter(allSubTargetPos[Random.Range(0, allSubTargetPos.Count)], false);
                }
            break;

            case Conditions.flip:
                Flip();
                if(BattleInfo.isMulti)
                {
                    FlipClientRpc(OwnerClientId);
                }
            break;

            case Conditions.counter:
                TargetSetter(aiReceiver.shooterPos, true);
                rotationSpeed = Mathf.Lerp(rotationSpeed, 0.8f, 0.5f);
            break;
        }
    }






    /*void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(MyPos, MyPos + relativePos);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(MyPos, MyPos + relativeSubPos.GetValueOrDefault());
        Gizmos.color = Color.white;
    }*/
}
