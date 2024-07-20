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
        somersaultTime = rac.animationClips.Where(a => a.name == "Flip").Select(b => b.length).ToArray()[0];
        rollTime = rac.animationClips.Where(a => a.name == "RightRoll").Select(b => b.length).ToArray()[0];
    }



    // Movement ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    bool can_rotate = true;
    public float maxRotSpeed { get; set; } = 40; // Might be changeable by availability.
    public int stickReverse { get; set; } = -1; // TODO: make this changeable by settings.

    protected override void Rotate()
    {
        if (!can_rotate)
        {
            return;
        }

        float targetRotX = 0;
        float relativeRotY = 0;
        float targetRotZ = 0;

        // Set target rotation (Branch by platform)
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (Input.anyKey)
        {
            if (Input.GetKey(KeyCode.W))
            {
                targetRotX += MAX_TILT_X;
            }
            if (Input.GetKey(KeyCode.S))
            {
                targetRotX -= MAX_TILT_X;
            }
            if (Input.GetKey(KeyCode.D))
            {
                relativeRotY = maxRotSpeed;
                targetRotZ = MAX_TILT_Z;
            }
            if (Input.GetKey(KeyCode.A))
            {
                relativeRotY = -maxRotSpeed;
                targetRotZ = -MAX_TILT_Z;
            }
#else
        if (uGUIMannager.onStick)
        {
            const int insensitivity = 100;
            targetRotX = Utilities.R2R(uGUIMannager.norm_diffPos.y, 0, MAX_TILT_X, Utilities.FunctionType.convex_down, insensitivity);
            relativeRotY = Utilities.R2R(uGUIMannager.norm_diffPos.x, 0, maxRotSpeed, Utilities.FunctionType.convex_down, insensitivity);
            targetRotZ = Utilities.R2R(uGUIMannager.norm_diffPos.x, 0, MAX_TILT_Z, Utilities.FunctionType.convex_down, insensitivity);
#endif
            Quaternion targetRot = Quaternion.Euler(targetRotX * stickReverse * uTurndirection, transform.rotation.eulerAngles.y + relativeRotY, targetRotZ * -1 * uTurndirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.05f);
        }
        else
        {
            FixTilt();
        }
    }

    void FixTilt()
    {
        const float fix_time = 0.05f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0), fix_time);
    }



    // 4-Actions ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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

    protected override IEnumerator somersault()
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
        float tmp_speed = 0;
        System.Guid guid = fighterCondition.speed.ApplyTempStatus(tmp_speed);

        // Look up camera (Owner only)
        if (IsOwner)
        {
            CameraController.I.LookUp(-90 * uTurndirection, somersaultTime / 2);
        }

        // Restart moving a bit faster than flip time.
        float resume_offset = 0.3f;
        yield return new WaitForSeconds(somersaultTime - resume_offset);
        fighterCondition.speed.RemoveTempStatus(guid);
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

        // Move fighter transform & camera. (Owner only)
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

        // Move fighter transform & camera. (Owner only)
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
        else if (CSManager.swipeUp) { Somersault(); }
        else if (CSManager.swipeLeft) { LeftRoll(0.2f); }
        else if (CSManager.swipeRight) { RightRoll(0.2f); }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (Input.GetKeyDown(KeyCode.DownArrow)) { Uturn(); }
        else if (Input.GetKeyDown(KeyCode.UpArrow)) { Somersault(); }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) { LeftRoll(0.2f); }
        else if (Input.GetKeyDown(KeyCode.RightArrow)) { RightRoll(0.2f); }
#endif
    }



    // Death & Revival ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected override IEnumerator DeathAnimation()
    {
        yield return StartCoroutine(base.DeathAnimation());

        // Return to start position. (Only the owner should control transforms)
        if (IsOwner)
        {
            int no = fighterCondition.fighterNo.Value;
            SpawnPointFighter point = BattleConductor.spawnPointManager.GetSpawnPointFighter(no);
            Transform point_trans = point.transform;
            transform.position = point_trans.position;
            transform.rotation = point_trans.rotation;
        }
    }

    protected override void OnRevival()
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
}
