using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class AiMovement : Movement
{
    protected override void Awake()
    {
        base.Awake();

        anim = GetComponentInChildren<Animator>();
        var rac = anim.runtimeAnimatorController;
        uturnTime = rac.animationClips.Where(a => a.name == "U-Turn").Select(b => b.length).ToArray()[0];
        somersaultTime = rac.animationClips.Where(a => a.name == "Flip").Select(b => b.length).ToArray()[0];
        rollTime = rac.animationClips.Where(a => a.name == "RightRoll").Select(b => b.length).ToArray()[0];

        aiReceiver = (AiReceiver)fighterCondition.receiver;
    }

    protected override void FixedUpdate()
    {
        if (!IsOwner) return;

        base.FixedUpdate();

        if (fighterCondition.isDead || !controllable) return;

        // Emergency Avoidance (Check for obstacles in front to avoid crashing in to it)
        if (!avoiding)
        {
            Ray ray = new Ray(transform.position, transform.forward * uTurndirection);
            bool obstacle_in_front = Physics.SphereCast(ray, SPHERE_CAST_RADIUS, AVOID_DISTANCE, FighterCondition.obstacles_mask);
            if (obstacle_in_front)
            {
                // Set avoiding to true, and reset it after u-turn is finished.
                avoiding = true;
                DOVirtual.DelayedCall(uturnTime, () => avoiding = false).Play();

                // U-Turn to avoid obstacle.
                Uturn();

                // Search for a detour.
                SetNextDestination();
            }
        }

        DecideAction();
        MovementByAction();
        ArrivalCheck();
    }



    // Movement ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    bool can_rotate = true;

    // Emergency Avoidance
    const float AVOID_DISTANCE = 80;  // Perform an avoidance if the distance is less than this value.
    bool avoiding = false;

    protected override void Rotate()
    {
        if (!can_rotate)
        {
            return;
        }

        Vector3 relative_to_next = nextDestination - transform.position;
        float relative_y_angle = Vector3.SignedAngle(transform.forward * uTurndirection, relative_to_next, Vector3.up);
        Vector3 targetAngle = Quaternion.LookRotation(((relative_to_next == Vector3.zero) ? transform.forward : relative_to_next) * uTurndirection).eulerAngles;
        if (relative_y_angle < -135)
        {
            targetAngle.z = (-MAX_TILT_Z / 45 * relative_y_angle - 4 * MAX_TILT_Z) * uTurndirection * -1;
        }
        else if (-135 <= relative_y_angle && relative_y_angle <= 135)
        {
            float normY = relative_y_angle / 135;
            targetAngle.z = MAX_TILT_Z / 2 * (Mathf.Pow(normY, 3) - 3 * normY) * uTurndirection;
        }
        else
        {
            targetAngle.z = (-MAX_TILT_Z / 45 * relative_y_angle + 4 * MAX_TILT_Z) * uTurndirection * -1;
        }
        Quaternion lookRotation = Quaternion.Euler(targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    public override void Controllable(bool controllable)
    {
        base.Controllable(controllable);
        if (controllable)
        {
            switch (BattleInfo.rule)
            {
                case Rule.BATTLE_ROYAL:
                    SetFinalDestination(SubTarget.GetRandomPosition());
                    break;

                case Rule.TERMINAL_CONQUEST:
                    break;

                case Rule.CRYSTAL_HUNTER:
                    break;
            }
        }
    }



    // 4-Actions ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] BurnerController burnerController;
    [SerializeField] ParticleSystem rollSpark;
    [SerializeField] AudioSource flipAudio, uturnAudio, rollAudio;

    protected override void FourActionExe()
    {
        float far_distance = 30;
        Vector3 relative_to_next = nextDestination - transform.position;
        float relative_y_angle = Vector3.SignedAngle(transform.forward * uTurndirection, relative_to_next, Vector3.up);
        if (Vector3.SqrMagnitude(relative_to_next) >= Mathf.Pow(far_distance, 2))
        {
            if (relative_y_angle >= -60 && relative_y_angle <= -45)
            {
                LeftRoll(3);
            }
            else if (relative_y_angle >= 45 && relative_y_angle <= 60)
            {
                RightRoll(3);
            }
            else if (relative_y_angle <= -135 || relative_y_angle >= 135)
            {
                Uturn();
            }
            else return;
        }
        else
        {
            if (relative_y_angle <= -135 || relative_y_angle >= 135)
            {
                Uturn();
            }
            else return;
        }
    }

    protected override IEnumerator uTurn()
    {
        // Disable 4actions.
        ready4action = false;

        // Start animation & effects.
        anim.SetInteger("FighterAnim", 2 * uTurndirection);
        burnerController.PlayImpact();
        burnerController.PlaySpark();
        uturnAudio.Play();

        // Direction flips at this point.
        yield return new WaitForSeconds(0.33f);
        uTurndirection *= -1;
        anim.SetInteger("FighterAnim", uTurndirection);

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

        // Animation & Effects.
        anim.SetInteger("FighterAnim", 3 * uTurndirection);
        burnerController.PlayImpact();
        burnerController.PlayStaticBurner();
        burnerController.PlaySpark();
        flipAudio.Play();

        // Stop moving.
        float tmp_speed = 0;
        System.Guid guid = fighterCondition.speed.ApplyTempStatus(tmp_speed);

        // Restart moving a bit faster than flip time.
        float resume_offset = 0.3f;
        yield return new WaitForSeconds(somersaultTime - resume_offset);
        fighterCondition.speed.RemoveTempStatus(guid);
        burnerController.StopStaticBurner();
        burnerController.StopSpark();

        // Flip completes here.
        yield return new WaitForSeconds(resume_offset);
        can_rotate = true;
        anim.SetInteger("FighterAnim", uTurndirection);

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

        // Move fighter transform. (Owner only)
        if (IsOwner)
        {
            transform.DOBlendableMoveBy(-transform.right * rollDistance * uTurndirection, rollTime)
                .SetEase(Ease.OutQuint);
        }

        // Rolling ends here.
        yield return new WaitForSeconds(rollTime);
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

        // Move fighter transform. (Owner only)
        if (IsOwner)
        {
            transform.DOBlendableMoveBy(transform.right * rollDistance * uTurndirection, rollTime)
                .SetEase(Ease.OutQuint);
        }

        // Rolling ends here.
        yield return new WaitForSeconds(rollTime);
        anim.SetInteger("FighterAnim", uTurndirection);
        burnerController.StopSpark();

        // Wait for a while to enable 4actions.
        yield return new WaitForSeconds(freeze_time);
        ready4action = true;
    }



    // AI Controll /////////////////////////////////////////////////////////////////////////////////////////////////////////
    enum Actions { ATTACK, SEARCH, GOBACK, FARESCAPE, SOMERSAULT, COUNTER }
    Actions action = Actions.SEARCH;
    Actions prev_action = Actions.SEARCH;
    GameObject targetFighter;
    AiReceiver aiReceiver;

    void DecideAction()
    {
        prev_action = action;

        if (aiReceiver.underAttack)
        {
            targetFighter = null;
            Vector3 relative_pos = aiReceiver.shooterBody.transform.position - transform.position;
            float relative_y_ang = Vector3.SignedAngle(transform.forward * uTurndirection, relative_pos, Vector3.up);
            if (relative_y_ang >= -30 && relative_y_ang <= 30)
            {
                action = Actions.COUNTER;
            }
            else if (relative_y_ang <= -135 || relative_y_ang >= 135)
            {
                float somersault_distance = 30;
                if (Vector3.SqrMagnitude(relative_pos) < Mathf.Pow(somersault_distance, 2))
                {
                    action = Actions.SOMERSAULT;
                }
                else
                {
                    action = Actions.COUNTER;
                }
            }
            else
            {
                action = Actions.GOBACK;
            }
        }

        else
        {
            switch (BattleInfo.rule)
            {
                case Rule.BATTLE_ROYAL:
                    if (fighterCondition.attack.lockonCount > 0)
                    {
                        if (targetFighter == null)
                        {
                            int target_no = fighterCondition.attack.lockonTargetNos[0];
                            targetFighter = ParticipantManager.I.fighterInfos[target_no].body;
                        }
                        action = Actions.ATTACK;
                    }
                    else
                    {
                        if (targetFighter != null)
                        {
                            targetFighter = null;
                        }
                        action = Actions.SEARCH;
                    }
                    break;

                case Rule.TERMINAL_CONQUEST:
                    break;

                case Rule.CRYSTAL_HUNTER:
                    break;
            }
        }
    }


    // Rotation speed differs by each condition.
    const float QUICK_ROTATIONSPEED = 2f;
    const float SLOW_ROTATIONSPEED = 0.5f;
    void MovementByAction()
    {
        switch (action)
        {
            // === Not Under Attack === //
            case Actions.ATTACK:
                {
                    rotationSpeed = QUICK_ROTATIONSPEED;
                    SetFinalDestination(targetFighter.transform.position);
                    break;
                }

            case Actions.SEARCH:
                {
                    rotationSpeed = QUICK_ROTATIONSPEED;
                    if (arrived_at_final_destination)
                    {
                        SetFinalDestination(SubTarget.GetRandomPosition());
                    }
                    // If chasing other fighter, set new final_destination. (No need to arrive at target fighters position any more.)
                    else if (prev_action == Actions.ATTACK ||
                            prev_action == Actions.GOBACK ||
                            prev_action == Actions.COUNTER)
                    {
                        SetFinalDestination(SubTarget.GetRandomPosition());
                    }
                    break;
                }


            // === Under Attack === //
            case Actions.GOBACK:
                {
                    rotationSpeed = SLOW_ROTATIONSPEED;
                    float back_offset = -30;
                    Transform shooter_trans = aiReceiver.shooterBody.transform;
                    SetFinalDestination(shooter_trans.position + shooter_trans.forward * back_offset);
                    break;
                }

            case Actions.FARESCAPE:
                {
                    rotationSpeed = QUICK_ROTATIONSPEED;
                    if (arrived_at_final_destination) SetFinalDestination(SubTarget.GetRandomPosition());
                    break;
                }

            case Actions.SOMERSAULT:
                {
                    Somersault();
                    break;
                }

            case Actions.COUNTER:
                {
                    rotationSpeed = QUICK_ROTATIONSPEED;
                    Transform shooter_trans = aiReceiver.shooterBody.transform;
                    SetFinalDestination(shooter_trans.position);
                    break;
                }
        }
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

    public override void OnRevival()
    {
        base.OnRevival();

        // Reset u-turn.
        if (uTurndirection == -1) uTurndirection = 1;

        // Stop burner effects.
        burnerController.StopStaticBurner();
        burnerController.StopSpark();
    }



    // For Debug ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void OnDrawGizmos()
    {
        if (!controllable) return;

        Vector3 my_position = transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(my_position, finalDestination);

        if (bypassing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(my_position, nextDestination);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(my_position + transform.forward * uTurndirection * AVOID_DISTANCE, SPHERE_CAST_RADIUS);
    }
}
