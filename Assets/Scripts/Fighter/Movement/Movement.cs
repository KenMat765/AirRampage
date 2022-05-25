using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

// 機体を動かすクラス
public abstract class Movement : NetworkBehaviour
{
    public FighterCondition fighterCondition {get; set;}

    // Only the owner needs to call this.
    public virtual void OnDeath()
    {
        ready4action = false;
        StartCoroutine(ReturnToStartPos());
    }

    IEnumerator ReturnToStartPos()
    {
        // 1.6f : effect play time of explosion dead.
        yield return new WaitForSeconds(1.6f);
        transform.position = start_pos;
        transform.rotation = start_rot;
    }

    // Must be called on every clients.
    public virtual void OnRevival()
    {
        StartCoroutine(Enable4Actions());
    }

    IEnumerator Enable4Actions()
    {
        yield return new WaitForSeconds(3);
        ready4action = true;
    }



    protected Vector3 myPos;
    protected Quaternion myRot;
    Vector3 start_pos;
    Quaternion start_rot;

    protected float maxTiltX = 55;  //縦
    protected float maxTiltZ = 60;  //左右



    protected virtual void Awake()
    {
        fighterCondition = GetComponent<FighterCondition>();
        start_pos = transform.position;
        start_rot = transform.rotation;
    }

    protected virtual void FixedUpdate()
    {
        if(fighterCondition.isDead) return;
        UpdateTrans();
        MoveForward();
        Rotate();
        FourActionExe();
    }

    protected virtual void UpdateTrans() { myPos = transform.position; myRot = transform.rotation; }

    protected void MoveForward()
    {
        float speed = fighterCondition.speed;
        transform.position = Vector3.MoveTowards(
            myPos,
            myPos + (transform.forward * speed * Time.deltaTime * uTurndirection) + (transform.right * rollSpeed * Time.deltaTime * uTurndirection * rollDirection),
            speed + rollSpeed);
    }

    protected abstract void Rotate();


    
    // 4アクション ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected Animator anim;
    protected float uturnTime, flipTime, rollTime;

    // KariCameraで使うためにpublic(後でprotectedに直す)
    public int uTurndirection {get; set;} = 1;
    
    bool ready4action = true;

    protected void Uturn()
    {
        if(ready4action) StartCoroutine(uTurn());
        if(BattleInfo.isMulti && IsOwner) UturnServerRpc(OwnerClientId);
    }
    protected virtual IEnumerator uTurn()
    {
        ready4action = false;
        anim.SetInteger("FighterAnim",2*uTurndirection);

        yield return new WaitForSeconds(0.33f);

        uTurndirection *= -1;
        anim.SetInteger("FighterAnim",uTurndirection);

        yield return new WaitForSeconds(uturnTime - 0.33f);

        ready4action = true;
    }

    protected void Flip()
    {
        if(ready4action) StartCoroutine(flip());
        if(BattleInfo.isMulti && IsOwner) FlipServerRpc(OwnerClientId);
    }
    protected virtual IEnumerator flip()
    {
        ready4action = false;
        anim.SetInteger("FighterAnim",3*uTurndirection);

        yield return new WaitForSeconds(1.5f);

        anim.SetInteger("FighterAnim",uTurndirection);

        yield return new WaitForSeconds(flipTime - 1.5f);

        ready4action = true;
    }

    bool rollReady = true;
    int rollDirection = 1;   //right:1 left:-1
    float rollSpeed;

    protected void LeftRole(float delay)
    {
        if(ready4action && rollReady) StartCoroutine(leftrole(delay));
        if(BattleInfo.isMulti && IsOwner) LeftRoleServerRpc(OwnerClientId, delay);
    }
    protected virtual IEnumerator leftrole(float delay)
    {
        ready4action = false;
        rollReady = false;
        anim.SetInteger("FighterAnim",5*uTurndirection);
        rollDirection = -1;
        rollSpeed = 1f;

        yield return new WaitForSeconds(rollTime);

        anim.SetInteger("FighterAnim",uTurndirection);
        rollSpeed = 0;
        ready4action = true;

        yield return new WaitForSeconds(delay);

        rollReady = true;
    }

    protected void RightRole(float delay)
    {
        if(ready4action && rollReady) StartCoroutine(rightrole(delay));
        if(BattleInfo.isMulti && IsOwner) RightRoleServerRpc(OwnerClientId, delay);
    }
    protected virtual IEnumerator rightrole(float delay)
    {
        ready4action = false;
        rollReady = false;
        anim.SetInteger("FighterAnim",4*uTurndirection);
        rollDirection = 1;
        rollSpeed = 1f;

        yield return new WaitForSeconds(rollTime);

        anim.SetInteger("FighterAnim",uTurndirection);
        rollSpeed = 0;
        ready4action = true;

        yield return new WaitForSeconds(delay);

        rollReady = true;
    }

    protected virtual void FourActionExe() {}

    [ServerRpc]
    void FlipServerRpc(ulong senderId) => FlipClientRpc(senderId);
    [ServerRpc]
    void UturnServerRpc(ulong senderId) => UturnClientRpc(senderId);
    [ServerRpc]
    void LeftRoleServerRpc(ulong senderId, float delay) => LeftRoleClientRpc(senderId, delay);
    [ServerRpc]
    void RightRoleServerRpc(ulong senderId, float delay) => RightRoleClientRpc(senderId, delay);
    [ClientRpc]
    void FlipClientRpc(ulong senderId)
    {
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        Flip();
    }
    [ClientRpc]
    void UturnClientRpc(ulong senderId)
    {
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        Uturn();
    }
    [ClientRpc]
    void LeftRoleClientRpc(ulong senderId, float delay)
    {
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        LeftRole(delay);
    }
    [ClientRpc]
    void RightRoleClientRpc(ulong senderId, float delay)
    {
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        RightRole(delay);
    }



    // AIのみ使用 /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected Vector3 targetPos;    //Sub Class で設定する。常に存在する。
    protected Vector3 relativePos;
    protected Vector3? subTargetPos;    //Sub Class で設定する。存在しない場合がある。
    protected Vector3? relativeSubPos;
    Vector3? latestSubTargetPos;
    protected bool mainArrived {get; set;} = true;  //main と sub のどちらに到着しても true にする。
    protected bool subArrived {get; set;}
    protected float farNearBorder {get; private set;} = 5f;
    protected float rotationSpeed {get; set;}

    protected void TargetSetter(Vector3 targetPosSet, bool set_as_main)
    {
        if(set_as_main) targetPos = targetPosSet;
        if(subArrived)
        {
            LayerMask rayMask = LayerMask.GetMask("Terrain");
            if(Physics.Raycast(myPos, relativePos, Vector3.Magnitude(relativePos), rayMask))
            {
                //20
                float searchRaidus = 20;
                LayerMask sphereMask = LayerMask.GetMask("SubTarget");
                List<Vector3> subTargetPosesAround = Physics.OverlapSphere(myPos, searchRaidus, sphereMask, QueryTriggerInteraction.Collide)
                    .Select(s => s.transform.position)
                    .Where(t => !Physics.Raycast(myPos, t - myPos, Vector3.Magnitude(t - myPos), rayMask))
                    .ToList();
                subTargetPosesAround.Remove(latestSubTargetPos.GetValueOrDefault());
                float degree = 180;
                foreach(Vector3 subTargetPosAround in subTargetPosesAround)
                {
                    Vector3 relativeSubTargetPosAround = subTargetPosAround - myPos;
                    if(Mathf.Abs(Vector3.SignedAngle(relativePos, relativeSubTargetPosAround, Vector3.up)) < degree)
                    {
                        degree = Mathf.Abs(Vector3.SignedAngle(relativePos, relativeSubTargetPosAround, Vector3.up));
                        subTargetPos = subTargetPosAround;
                        latestSubTargetPos = subTargetPos;
                    }
                }
            }
            else
            {
                subTargetPos = null;
                latestSubTargetPos = null;
            }
        }
    }

    protected void ArrivalJudge()
    {
        if(Vector3.SqrMagnitude(relativePos) < Mathf.Sqrt(farNearBorder)) mainArrived = true;
        else mainArrived = false;

        if(Vector3.SqrMagnitude(relativeSubPos.GetValueOrDefault()) < Mathf.Sqrt(farNearBorder) && subArrived == false) subArrived = true;
        else subArrived = false;
    }
}
