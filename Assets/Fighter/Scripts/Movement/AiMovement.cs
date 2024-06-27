using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class AiMovement : Movement
{
    // Emergency Avoidance
    const float AVOID_DISTANCE = 80;    // Perform an avoidance if the distance is less than this value.
    bool avoiding = false;


    protected override void Awake()
    {
        base.Awake();

        anim = GetComponentInChildren<Animator>();
        var rac = anim.runtimeAnimatorController;
        uturnTime = rac.animationClips.Where(a => a.name == "U-Turn").Select(b => b.length).ToArray()[0];
        flipTime = rac.animationClips.Where(a => a.name == "Flip").Select(b => b.length).ToArray()[0];
        rollTime = rac.animationClips.Where(a => a.name == "RightRoll").Select(b => b.length).ToArray()[0];

        aiReceiver = (AiReceiver)fighterCondition.receiver;
        aiAttack = (AiAttack)fighterCondition.attack;

        latestDestinations = new Vector3[max_cashe];
    }


    protected override void FixedUpdate()
    {
        // Only the host can control AI.
        if (!IsHost) return;

        base.FixedUpdate();

        if (fighterCondition.isDead || !controllable) return;

        // Emergency Avoidance (Check for obstacles in front to avoid crashing in to it)
        if (!avoiding)
        {
            if (ObstacleIsInFront(AVOID_DISTANCE))
            {
                // For Debug.
                // Debug.Log("<color=green>Avoid</color>", gameObject);

                // Set avoiding to true, and reset it after u-turn is finished.
                avoiding = true;
                DOVirtual.DelayedCall(uturnTime, () => avoiding = false).Play();
                // U-Turn to avoid obstacle.
                Uturn();
                // Reset next destination.
                SetNextDestination();
            }
        }

        ChangeCondition();
        ActionOnEachCondition();
        ArrivalCheck();
    }


    public override void Controllable(bool controllable)
    {
        base.Controllable(controllable);
        if (controllable)
        {
            switch (BattleInfo.rule)
            {
                // Start going to random position when Battle Royal.
                case Rule.BATTLEROYAL: SetFinalDestination(SubTarget.GetRandomPosition()); break;

                // Start going to random opponent terminal.
                case Rule.TERMINALCONQUEST:
                    targetTerminal = TerminalManager.I.GetOpponentTerminals(fighterCondition.fighterTeam.Value).RandomChoice();
                    SetFinalDestination(targetTerminal.transform.position, true);
                    break;
            }
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


    bool can_rotate = true;
    protected override void Rotate()
    {
        if (!can_rotate)
        {
            return;
        }

        Vector3 targetAngle = Quaternion.LookRotation(((relative_to_next == Vector3.zero) ? transform.forward : relative_to_next) * uTurndirection).eulerAngles;
        if (relativeYAngle < -135)
        {
            targetAngle.z = (-maxTiltZ / 45 * relativeYAngle - 4 * maxTiltZ) * uTurndirection * -1;
        }
        else if (-135 <= relativeYAngle && relativeYAngle <= 135)
        {
            float normY = relativeYAngle / 135;
            targetAngle.z = maxTiltZ / 2 * (Mathf.Pow(normY, 3) - 3 * normY) * uTurndirection;
        }
        else
        {
            targetAngle.z = (-maxTiltZ / 45 * relativeYAngle + 4 * maxTiltZ) * uTurndirection * -1;
        }
        Quaternion lookRotation = Quaternion.Euler(targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }



    // 4アクション ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] BurnerController burnerController;
    [SerializeField] ParticleSystem rollSpark;
    [SerializeField] AudioSource flipAudio, uturnAudio, rollAudio;

    protected override void FourActionExe()
    {
        if (Vector3.SqrMagnitude(relative_to_next) >= SQR_DISTANCE_BORDER)
        {
            if (relativeYAngle >= -60 && relativeYAngle <= -45)
            {
                LeftRoll(3);
            }
            else if (relativeYAngle >= 45 && relativeYAngle <= 60)
            {
                RightRoll(3);
            }
            else if (relativeYAngle <= -135 || relativeYAngle >= 135)
            {
                Uturn();
            }
            else return;
        }
        else
        {
            if (relativeYAngle <= -135 || relativeYAngle >= 135)
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

    protected override IEnumerator flip()
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
        float speed_temp = fighterCondition.speed;
        fighterCondition.PauseGradingSpeed(0);

        // Restart moving a bit faster than flip time.
        float resume_offset = 0.3f;
        yield return new WaitForSeconds(flipTime - resume_offset);
        fighterCondition.ResumeGradingSpeed();
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

        // Move transform & camera. (Owner only)
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

        // Move transform & camera. (Owner only)
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
    enum Conditions { ATTACK, SEARCH, GOBACK, FARESCAPE, FLIP, COUNTER, ATTACK_TERMINAL, DEFENCE_TERMINAL }
    [SerializeField] Conditions condition = Conditions.SEARCH;
    [SerializeField] Conditions prev_condition = Conditions.SEARCH;

    GameObject targetFighter;
    Terminal targetTerminal;    // Attacking or defencing terminal.

    float relativeYAngle => Vector3.SignedAngle(transform.forward * uTurndirection, relative_to_next, Vector3.up);

    AiReceiver aiReceiver;
    AiAttack aiAttack;


    void ChangeCondition()
    {
        prev_condition = condition;

        // Under Attack.
        // Take necessary action despite of rule.
        if (aiReceiver.underAttack)
        {
            // 現在の標的を削除
            targetFighter = null;

            GameObject shooter = aiReceiver.currentShooter;
            Vector3 relativePos = aiReceiver.relativeSPos;
            float relativeAngle = aiReceiver.relativeSAngle;

            // When Shooter is alive.
            if (shooter.activeSelf == true)
            {
                if (relativeAngle >= -30 && relativeAngle <= 30) condition = Conditions.COUNTER;
                else if (relativeAngle <= -135 || relativeAngle >= 135)
                {
                    if (Vector3.SqrMagnitude(relativePos) < SQR_DISTANCE_BORDER) condition = Conditions.FLIP;
                    // else condition = Conditions.FARESCAPE;
                    else condition = Conditions.COUNTER;
                }
                else condition = Conditions.GOBACK;
            }

            // When Shooter is dead.
            else condition = Conditions.SEARCH;
        }

        // Not Under Attack.
        // Act appropriately according to the rule.
        else
        {
            switch (BattleInfo.rule)
            {
                case Rule.BATTLEROYAL:
                    // When there are opponents in front.
                    if (aiAttack.homingCount > 0)
                    {
                        // Set target fighter if null.
                        if (targetFighter == null)
                        {
                            int target_no = aiAttack.homingTargetNos[0];
                            targetFighter = ParticipantManager.I.fighterInfos[target_no].body;
                        }
                        condition = Conditions.ATTACK;
                    }

                    // When nobody is in front.
                    else
                    {
                        // Set target fighter to null.
                        if (targetFighter != null) targetFighter = null;
                        condition = Conditions.SEARCH;
                    }
                    break;

                case Rule.TERMINALCONQUEST:
                    // Set target to terminal.
                    // Judge whether ally team is currently winning or not.
                    float my_team_point_per_sec = 0;
                    float opponent_team_point_per_sec = 0;
                    switch (fighterCondition.fighterTeam.Value)
                    {
                        case Team.RED:
                            my_team_point_per_sec = TerminalManager.redPoint_per_second;
                            opponent_team_point_per_sec = TerminalManager.bluePoint_per_second;
                            break;

                        case Team.BLUE:
                            my_team_point_per_sec = TerminalManager.bluePoint_per_second;
                            opponent_team_point_per_sec = TerminalManager.redPoint_per_second;
                            break;
                    }

                    // If winning, defence ally terminal.
                    if (my_team_point_per_sec > opponent_team_point_per_sec)
                    {
                        // When current target is null or opponent, set new target terminal.
                        if (targetTerminal == null || targetTerminal.team != fighterCondition.fighterTeam.Value)
                        {
                            // Prioritize owner terminals.
                            List<Terminal> owner_terminals;

                            // If owner terminals were found, choose random one to defence.
                            if (TerminalManager.I.TryGetOwnerTerminals(fighterCondition.fighterNo.Value, out owner_terminals))
                            {
                                targetTerminal = owner_terminals.RandomChoice();
                            }

                            // If there were no owner terminals, defence random ally terminal.
                            else
                            {
                                targetTerminal = TerminalManager.I.GetAllyTerminals(fighterCondition.fighterTeam.Value).RandomChoice();
                            }
                        }

                        // If detected opponent fighter during defencing ally terminal, chase and attack it.
                        if (aiAttack.homingCount > 0)
                        {
                            if (targetFighter == null)
                            {
                                int targetNo = aiAttack.homingTargetNos[0];
                                targetFighter = ParticipantManager.I.fighterInfos[targetNo].body;
                            }
                            condition = Conditions.ATTACK;
                        }

                        // Otherwise, fly around target (= ally) terminal.
                        else
                        {
                            if (targetFighter != null) targetFighter = null;
                            condition = Conditions.DEFENCE_TERMINAL;
                        }
                    }

                    // If losing or draw.
                    else
                    {
                        // Set opponent terminal with least HP as a target when current target is null or is ally terminal.
                        if (targetTerminal == null || targetTerminal.team == fighterCondition.fighterTeam.Value)
                        {
                            targetTerminal = TerminalManager.I.GetNearestOpponentTerminal(fighterCondition.fighterTeam.Value, transform.position);
                        }
                        condition = Conditions.ATTACK_TERMINAL;
                    }
                    break;
            }
        }
    }


    // Rotation speed differs by each condition.
    const float QUICK_ROTATIONSPEED = 2f;
    const float SLOW_ROTATIONSPEED = 0.5f;
    void ActionOnEachCondition()
    {
        switch (condition)
        {
            // Not Under Attack.
            case Conditions.ATTACK:
                rotationSpeed = QUICK_ROTATIONSPEED;
                if (targetFighter != null)
                {
                    SetFinalDestination(targetFighter.transform.position);
                }
                break;

            case Conditions.SEARCH:
                rotationSpeed = QUICK_ROTATIONSPEED;

                if (arrived_at_final_destination)
                {
                    SetFinalDestination(SubTarget.GetRandomPosition());
                }

                // If previous condition was ATTACK, just set new final_destination. (No need to arrive at target_fighters position any more.)
                else if (prev_condition == Conditions.ATTACK ||
                        prev_condition == Conditions.GOBACK ||
                        prev_condition == Conditions.COUNTER)
                {
                    SetFinalDestination(SubTarget.GetRandomPosition());
                }

                break;


            // Under Attack.
            case Conditions.GOBACK:
                rotationSpeed = SLOW_ROTATIONSPEED;
                SetFinalDestination(aiReceiver.shooterPos + aiReceiver.currentShooter.transform.forward * -DISTANCE_BORDER);
                break;

            case Conditions.FARESCAPE:
                rotationSpeed = QUICK_ROTATIONSPEED;
                if (arrived_at_final_destination) SetFinalDestination(SubTarget.GetRandomPosition());
                break;

            case Conditions.FLIP:
                Flip();
                break;

            case Conditions.COUNTER:
                rotationSpeed = QUICK_ROTATIONSPEED;
                SetFinalDestination(aiReceiver.shooterPos);
                break;


            // Only On Terminal Conquest.
            case Conditions.ATTACK_TERMINAL:
                rotationSpeed = QUICK_ROTATIONSPEED;

                if (arrived_at_final_destination)
                {
                    if (destination_is_terminal) SetFinalDestination(targetTerminal.GetRandomSubTargetPositionAround());
                    else SetFinalDestination(targetTerminal.transform.position, true);
                }

                // If previous condition was ATTACK, just set new final_destination. (No need to arrive at target_fighters position any more.)
                else if (prev_condition == Conditions.ATTACK ||
                        prev_condition == Conditions.GOBACK ||
                        prev_condition == Conditions.COUNTER)
                {
                    SetFinalDestination(targetTerminal.transform.position, true);
                }

                break;

            case Conditions.DEFENCE_TERMINAL:
                rotationSpeed = SLOW_ROTATIONSPEED;

                if (arrived_at_final_destination)
                {
                    if (destination_is_terminal) SetFinalDestination(targetTerminal.GetRandomSubTargetPositionAround());
                    else SetFinalDestination(targetTerminal.transform.position, true);
                }

                // If previous condition was ATTACK, just set new final_destination. (No need to arrive at target_fighters position any more.)
                else if (prev_condition == Conditions.ATTACK ||
                        prev_condition == Conditions.GOBACK ||
                        prev_condition == Conditions.COUNTER)
                {
                    SetFinalDestination(targetTerminal.transform.position, true);
                }

                break;
        }
    }






    void OnDrawGizmos()
    {
        if (!controllable) return;

        Transform trans = transform;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(trans.position, trans.position + relative_to_final);

        if (bypassing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(trans.position, trans.position + relative_to_next);
        }

        // Gizmos.color = Color.green;
        // Gizmos.DrawWireSphere(trans.position, searchRadius_orig);

        // Gizmos.color = Color.cyan;
        // Gizmos.DrawWireSphere(trans.position, distanceBorder);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(trans.position + trans.forward * uTurndirection * AVOID_DISTANCE, CAST_RADIUS);
    }
}
