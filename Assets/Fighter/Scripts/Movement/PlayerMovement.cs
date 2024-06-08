using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMovement : Movement
{
    protected override void Awake()
    {
        base.Awake();

        anim = GetComponentInChildren<Animator>();
        var rac = anim.runtimeAnimatorController;
        uturnTime = rac.animationClips.Where(a => a.name == "U-Turn").Select(b => b.length).ToArray()[0];
        flipTime = rac.animationClips.Where(a => a.name == "Flip").Select(b => b.length).ToArray()[0];
        rollTime = rac.animationClips.Where(a => a.name == "RightRoll").Select(b => b.length).ToArray()[0];
    }

    protected override void FixedUpdate()
    {
        // Only the owner can controll fighter.
        if (BattleInfo.isMulti && !IsOwner) return;

        // Don't move when dead.
        if (fighterCondition.isDead) return;

        MoveForward();

        // Don't rotate or invoke 4 actions when not controllable.
        if (!controllable) return;

        Rotate();
        FourActionExe();

#if UNITY_EDITOR
        float maxRotSpeed = 40;
        float maxTiltX = 55;  //縦
        float maxTiltZ = 60;  //左右
        float targetRotX = 0, relativeRotY = 0, targetRotZ = 0;
        Quaternion targetRot = default(Quaternion);

        if (Input.anyKey)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                targetRotX += maxTiltX;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                targetRotX -= maxTiltX;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                relativeRotY = maxRotSpeed;
                targetRotZ = maxTiltZ;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                relativeRotY = maxRotSpeed * -1;
                targetRotZ = maxTiltZ * -1;
            }
            targetRot = Quaternion.Euler(targetRotX * -1, transform.rotation.eulerAngles.y + relativeRotY, targetRotZ * -1);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.05f);
        }
        else
        {
            FixTilt();
        }
#endif
    }



    public override void OnRevival()
    {
        base.OnRevival();
        if (uTurndirection == -1)
        {
            uTurndirection = 1;

            if (BattleInfo.isMulti && !IsOwner) return;

            CameraController.I.TurnCamera(uTurndirection);
        }
    }



    float maxRotSpeed = 40;
    public int stickReverse { get; set; } = -1;    // 設定で変更可能に
    bool can_rotate = true;

    protected override void Rotate()
    {
        if (can_rotate)
        {
            if (uGUIMannager.onStick)
            {
                const int k = 100;
                float targetRotX = Utilities.R2R(uGUIMannager.norm_diffPos.y, 0, maxTiltX, Utilities.FunctionType.convex_down, k);
                float relativeRotY = Utilities.R2R(uGUIMannager.norm_diffPos.x, 0, maxRotSpeed, Utilities.FunctionType.convex_down, k);
                float targetRotZ = Utilities.R2R(uGUIMannager.norm_diffPos.x, 0, maxTiltZ, Utilities.FunctionType.convex_down, k);
                Quaternion targetRot = Quaternion.Euler(targetRotX * stickReverse * uTurndirection, transform.rotation.eulerAngles.y + relativeRotY, targetRotZ * -1 * uTurndirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.05f);
            }
            else
            {
                FixTilt();
            }
        }
    }



    // 機体の傾きを戻す ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void FixTilt()
    {
        const float fix_time = 0.05f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0), fix_time);
    }



    // 4アクション ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] BurnerController burnerController;
    [SerializeField] ParticleSystem rollSpark;

    protected override IEnumerator uTurn()
    {
        StartCoroutine(base.uTurn());

        if (BattleInfo.isMulti && !IsOwner) yield break;

        yield return new WaitForSeconds(0.33f);

        CameraController.I.TurnCamera(uTurndirection);
    }

    protected override IEnumerator flip()
    {
        burnerController.PlayImpact();
        StartCoroutine(base.flip());

        if (BattleInfo.isMulti && !IsOwner) yield break;

        can_rotate = false;
        CameraController.I.LookUp(-90 * uTurndirection, flipTime / 2);

        yield return new WaitForSeconds(flipTime);

        can_rotate = true;
    }

    protected override IEnumerator leftroll(float delay)
    {
        burnerController.PlayImpact(Direction.right);
        rollSpark.Play();
        burnerController.PlayBurstAudio();
        StartCoroutine(base.leftroll(delay));

        if (BattleInfo.isMulti && !IsOwner) yield break;

        CameraController.I.ShiftCameraPos(transform.right * roll_distance * uTurndirection, 80);

        yield return new WaitForSeconds(0.1f);

        CameraController.I.ResetCameraPos(30);
    }

    protected override IEnumerator rightroll(float delay)
    {
        burnerController.PlayImpact(Direction.left);
        rollSpark.Play();
        burnerController.PlayBurstAudio();
        StartCoroutine(base.rightroll(delay));

        if (BattleInfo.isMulti && !IsOwner) yield break;

        CameraController.I.ShiftCameraPos(-transform.right * roll_distance * uTurndirection, 80);

        yield return new WaitForSeconds(0.1f);

        CameraController.I.ResetCameraPos(30);
    }

    protected override void FourActionExe()
    {
        if (CSManager.swipeDown) { Uturn(); }
        else if (CSManager.swipeUp) { Flip(); }
        else if (CSManager.swipeLeft) { LeftRoll(0.2f); }
        else if (CSManager.swipeRight) { RightRoll(0.2f); }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.S)) { Uturn(); }
        else if (Input.GetKeyDown(KeyCode.W)) { Flip(); }
        else if (Input.GetKeyDown(KeyCode.A)) { LeftRoll(0.2f); }
        else if (Input.GetKeyDown(KeyCode.D)) { RightRoll(0.2f); }
#endif
    }
}
