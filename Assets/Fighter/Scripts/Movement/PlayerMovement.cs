using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

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

        // For Debug.
#if UNITY_EDITOR
        if (!can_rotate)
        {
            return;
        }

        float maxRotSpeed = 40;
        float maxTiltX = 55;  //縦
        float maxTiltZ = 60;  //左右
        float targetRotX = 0, relativeRotY = 0, targetRotZ = 0;
        Quaternion targetRot = default(Quaternion);

        if (Input.anyKey)
        {
            if (Input.GetKey(KeyCode.W))
            {
                targetRotX += maxTiltX;
            }
            if (Input.GetKey(KeyCode.S))
            {
                targetRotX -= maxTiltX;
            }
            if (Input.GetKey(KeyCode.D))
            {
                relativeRotY = maxRotSpeed;
                targetRotZ = maxTiltZ;
            }
            if (Input.GetKey(KeyCode.A))
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

        // Reset u-turn.
        if (uTurndirection == -1)
        {
            uTurndirection = 1;
            if (!IsOwner) return;
            CameraController.I.TurnCamera(uTurndirection);
        }

        // Stop burner effects.
        burnerController.StopStaticBurner();
        burnerController.StopSpark();
    }



    float maxRotSpeed = 40;
    public int stickReverse { get; set; } = -1;    // 設定で変更可能に
    bool can_rotate = true;

    protected override void Rotate()
    {
        if (!can_rotate)
        {
            return;
        }

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



    // 機体の傾きを戻す ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void FixTilt()
    {
        const float fix_time = 0.05f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0), fix_time);
    }



    // 4アクション ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] BurnerController burnerController;
    [SerializeField] ParticleSystem rollSpark;
    [SerializeField] AudioSource flipAudio, uturnAudio, rollAudio;

    protected override IEnumerator uTurn()
    {
        // Disable 4actions.
        ready4action = false;

        // Play animation & effects.
        anim.SetInteger("FighterAnim", 2 * uTurndirection);
        burnerController.PlayImpact();
        burnerController.PlaySpark();
        uturnAudio.Play();

        // Direction flips at this point.
        yield return new WaitForSeconds(0.33f);
        uTurndirection *= -1;
        anim.SetInteger("FighterAnim", uTurndirection);
        if (IsOwner)
        {
            CameraController.I.TurnCamera(uTurndirection);
        }

        // U-turn animation finishes here.
        yield return new WaitForSeconds(uturnTime - 0.33f);
        ready4action = true;
        burnerController.StopSpark();
    }

    protected override IEnumerator flip()
    {
        // Disable rotation & 4actions.
        ready4action = false;
        can_rotate = false;

        // Play animation & effects.
        anim.SetInteger("FighterAnim", 3 * uTurndirection);
        burnerController.PlayImpact();
        burnerController.PlayStaticBurner();
        burnerController.PlaySpark();
        flipAudio.Play();

        // Stop moving.
        float speed_temp = fighterCondition.speed;
        fighterCondition.PauseGradingSpeed(0);

        // Look up camera (Owner only)
        if (IsOwner)
        {
            CameraController.I.LookUp(-90 * uTurndirection, flipTime / 2);
        }

        // Restart moving a bit faster than flip time.
        float resume_offset = 0.3f;
        yield return new WaitForSeconds(flipTime - resume_offset);
        fighterCondition.ResumeGradingSpeed();
        burnerController.StopStaticBurner();

        // Flip completes here.
        yield return new WaitForSeconds(resume_offset);
        can_rotate = true;
        anim.SetInteger("FighterAnim", uTurndirection);
        burnerController.StopSpark();

        // Enable 4actions after few seconds.
        yield return new WaitForSeconds(3.0f);
        ready4action = true;
    }

    protected override IEnumerator leftroll(float freeze_time)
    {
        // Disable 4actions.
        ready4action = false;

        // Play animation & effects.
        anim.SetInteger("FighterAnim", 5 * uTurndirection);
        burnerController.PlayImpact(Direction.right);
        burnerController.PlaySpark();
        burnerController.PlayBurstAudio();
        rollSpark.Play();
        rollAudio.Play();

        // Move transform & camera. (Owner only)
        if (IsOwner)
        {
            transform.DOBlendableMoveBy(-transform.right * rollDistance * uTurndirection, rollTime)
                .SetEase(Ease.OutQuint);
            CameraController.I.ShiftCameraPos(transform.right * rollDistance * uTurndirection, 80);
        }

        float camera_delay = 0.15f;
        yield return new WaitForSeconds(camera_delay);
        CameraController.I.ResetCameraPos(30);

        // Rolling ends here.
        yield return new WaitForSeconds(rollTime - camera_delay);
        anim.SetInteger("FighterAnim", uTurndirection);
        burnerController.StopSpark();

        // Wait for a while to enable 4actions.
        yield return new WaitForSeconds(freeze_time);
        ready4action = true;
    }

    protected override IEnumerator rightroll(float freeze_time)
    {
        // Disable 4actions.
        ready4action = false;

        // Play animation & effects.
        anim.SetInteger("FighterAnim", 4 * uTurndirection);
        burnerController.PlayImpact(Direction.left);
        burnerController.PlaySpark();
        burnerController.PlayBurstAudio();
        rollSpark.Play();
        rollAudio.Play();

        // Move transform & camera. (Owner only)
        if (IsOwner)
        {
            transform.DOBlendableMoveBy(transform.right * rollDistance * uTurndirection, rollTime)
                .SetEase(Ease.OutQuint);
            CameraController.I.ShiftCameraPos(-transform.right * rollDistance * uTurndirection, 80);
        }

        float camera_delay = 0.15f;
        yield return new WaitForSeconds(camera_delay);
        CameraController.I.ResetCameraPos(30);

        // Rolling ends here.
        yield return new WaitForSeconds(rollTime - camera_delay);
        anim.SetInteger("FighterAnim", uTurndirection);
        burnerController.StopSpark();

        // Wait for a while to enable 4actions.
        yield return new WaitForSeconds(freeze_time);
        ready4action = true;
    }

    protected override void FourActionExe()
    {
        if (CSManager.swipeDown) { Uturn(); }
        else if (CSManager.swipeUp) { Flip(); }
        else if (CSManager.swipeLeft) { LeftRoll(0.2f); }
        else if (CSManager.swipeRight) { RightRoll(0.2f); }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.DownArrow)) { Uturn(); }
        else if (Input.GetKeyDown(KeyCode.UpArrow)) { Flip(); }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) { LeftRoll(0.2f); }
        else if (Input.GetKeyDown(KeyCode.RightArrow)) { RightRoll(0.2f); }
#endif
    }
}
