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
        rollTime = rac.animationClips.Where(a => a.name == "RightRole").Select(b => b.length).ToArray()[0];
    }



    public override void OnRevival()
    {
        base.OnRevival();
        if(uTurndirection == -1)
        {
            uTurndirection = 1;
            cameraController.CameraTurn(uTurndirection);
        }
    }



    float maxRotSpeed = 40;
    public int stickReverse {get; set;} = -1;    // 設定で変更可能に
    bool can_rotate = true;

    protected override void Rotate()
    {
        if(can_rotate)
        {
            if(uGUIMannager.onStick)
            {
                const int k = 100;
                float targetRotX = Utilities.R2R(uGUIMannager.norm_diffPos.y, 0, maxTiltX, Utilities.FunctionType.convex_down, k);;
                float relativeRotY = Utilities.R2R(uGUIMannager.norm_diffPos.x, 0, maxRotSpeed, Utilities.FunctionType.convex_down, k);
                float targetRotZ = Utilities.R2R(uGUIMannager.norm_diffPos.x, 0, maxTiltZ, Utilities.FunctionType.convex_down, k);;
                Quaternion targetRot = Quaternion.Euler(targetRotX * stickReverse * uTurndirection, myRot.eulerAngles.y + relativeRotY, targetRotZ * -1 * uTurndirection);
                transform.rotation = Quaternion.Slerp(myRot, targetRot, 0.05f);
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
        transform.rotation = Quaternion.Slerp(myRot, Quaternion.Euler(0, myRot.eulerAngles.y, 0), fix_time);
    }



    // 4アクション ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public CameraController cameraController;    // NitroBoostでも使用

    protected override IEnumerator uTurn()
    {
        StartCoroutine(base.uTurn());

        yield return new WaitForSeconds(0.33f);

        cameraController.CameraTurn(uTurndirection);
    }

    protected override IEnumerator flip()
    {
        StartCoroutine(base.flip());
        can_rotate = false;
        cameraController.CameraLookUp(-90*uTurndirection, flipTime/2);
        float speed_temp = fighterCondition.speed;
        fighterCondition.PauseGradingSpeed(0);

        yield return new WaitForSeconds(1.2f);

        fighterCondition.ResumeGradingSpeed();
        
        yield return new WaitForSeconds(flipTime - 1.2f);
        can_rotate = true;
    }

    protected override void FourActionExe()
    {
        if(CSManager.swipeDown) {Uturn();}
        else if(CSManager.swipeUp) {Flip();}
        else if(CSManager.swipeLeft) {LeftRole(0);}
        else if(CSManager.swipeRight) {RightRole(0);}
    }
}
