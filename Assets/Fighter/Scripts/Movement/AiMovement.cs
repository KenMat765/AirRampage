using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using DG.Tweening;

public class AiMovement : Movement
{
    protected override void Awake()
    {
        base.Awake();
        latestDestinations = new Vector3[MAX_CACHE];

        anim = GetComponentInChildren<Animator>();
        var rac = anim.runtimeAnimatorController;
        uturnTime = rac.animationClips.Where(a => a.name == "U-Turn").Select(b => b.length).ToArray()[0];
        somersaultTime = rac.animationClips.Where(a => a.name == "Flip").Select(b => b.length).ToArray()[0];
        rollTime = rac.animationClips.Where(a => a.name == "RightRoll").Select(b => b.length).ToArray()[0];

        attack = fighterCondition.GetComponentInChildren<Attack>();
        aiReceiver = fighterCondition.GetComponentInChildren<AiReceiver>();
    }

    protected override void FixedUpdate()
    {
        if (!IsOwner) return;
        if (fighterCondition.isDead) return;

        base.FixedUpdate();

        if (!controllable) return;

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
    Attack attack;
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
                    if (attack.lockonCount > 0)
                    {
                        if (targetFighter == null)
                        {
                            int target_no = attack.lockonTargetNos[0];
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

    float rotationSpeed;

    public Vector3 finalDestination { get; private set; }
    public Vector3 nextDestination { get; private set; }
    bool arrived_at_final_destination;
    bool arrived_at_next_destination;
    bool bypassing;
    const float ARRIVE_DISTANCE = 30f;
    const float SPHERE_CAST_RADIUS = 5;

    Vector3[] latestDestinations;
    const short MAX_CACHE = 10;
    int cache_idx = 0;

    void SetFinalDestination(Vector3 destination)
    {
        arrived_at_final_destination = false;
        finalDestination = destination;
        Array.Fill(latestDestinations, Vector3.zero);
        SetNextDestination();
    }

    void SetNextDestination()
    {
        arrived_at_next_destination = false;

        Vector3 my_position = transform.position;
        Vector3 relative_to_final = finalDestination - my_position;
        float distance_to_final = Vector3.Magnitude(relative_to_final);

        RaycastHit hit;
        Ray ray = new Ray(my_position, relative_to_final);
        bool obstacles_in_way = Physics.SphereCast(ray, SPHERE_CAST_RADIUS, out hit, distance_to_final, FighterCondition.obstacles_mask);
        if (obstacles_in_way)
        {
            bypassing = true;

            // Search for sub-targets around until trial reaches max_trial.
            int max_trial = 3;
            float search_radius = 150;
            List<Vector3> subTargetsAround = new List<Vector3>();       // Check for obstacles in way.
            List<Vector3> subTargetsAround_weak = new List<Vector3>();  // Does not check for obstacles.
            for (int trial = 1; trial <= max_trial; trial++)
            {
                // Expand search radius on each trial.
                float searchRadius = search_radius * trial;

                // Search sub-targets around.
                subTargetsAround_weak = Physics.OverlapSphere(my_position, searchRadius, SubTarget.mask, QueryTriggerInteraction.Collide)
                    .Select(s => s.transform.position)
                    .Where(t => !latestDestinations.Contains(t))
                    .ToList();
                subTargetsAround = subTargetsAround_weak
                    .Where(t =>
                    {
                        Ray ray_to_sub = new Ray(my_position, t - my_position);
                        float distance_to_sub = Vector3.Magnitude(t - my_position);
                        return !Physics.SphereCast(ray_to_sub, SPHERE_CAST_RADIUS, distance_to_sub, FighterCondition.obstacles_mask);
                    })
                    .ToList();

                // Break when sub-targets were found.
                if (subTargetsAround.Count > 0)
                {
                    break;
                }
            }

            // If no sub-targets were found, relax condition.
            if (subTargetsAround.Count < 1)
            {
                subTargetsAround = subTargetsAround_weak;
            }

            // Select sub-target which direction is closest to final destination.
            float min_degree = 360;
            foreach (Vector3 subTargetAround in subTargetsAround)
            {
                Vector3 relative_to_subAround = subTargetAround - my_position;
                float degree_to_final = Mathf.Abs(Vector3.SignedAngle(relative_to_final, relative_to_subAround, Vector3.up));
                if (degree_to_final < min_degree)
                {
                    min_degree = degree_to_final;
                    nextDestination = subTargetAround;
                }
            }

            // Update latest destinations.
            latestDestinations[cache_idx] = nextDestination;
            cache_idx = (cache_idx + 1) % MAX_CACHE;
        }
        else
        {
            bypassing = false;
            nextDestination = finalDestination;
        }
    }

    void ArrivalCheck()
    {
        if (arrived_at_next_destination) return;

        Vector3 relative_to_next = nextDestination - transform.position;
        if (Vector3.SqrMagnitude(relative_to_next) < Mathf.Pow(ARRIVE_DISTANCE, 2))
        {
            arrived_at_next_destination = true;
            if (bypassing)
            {
                SetNextDestination();
            }
            else
            {
                arrived_at_final_destination = true;
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
            SpawnPointFighter point = SpawnPointManager.I.GetSpawnPointFighter(no);
            Transform point_trans = point.transform;
            transform.position = point_trans.position;
            transform.rotation = point_trans.rotation;
        }
    }

    protected override void OnRevival()
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
