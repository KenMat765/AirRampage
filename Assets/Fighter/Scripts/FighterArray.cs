using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FighterArray : MonoBehaviour
{
    public const int fighter_in_array = 5;



    // ================================================================================================ //
    // === First Setup === //
    // ================================================================================================ //

    public Transform[] points { get; private set; }
    public Team team { get; set; }
    public bool standby { get; set; } = true;
    public int zako_left { get; set; }

    // Call setup function from spawning point (or Terminal).
    public void Setup()
    {
        this.points = gameObject.GetComponentsInChildrenWithoutSelf<Transform>();
    }



    // ================================================================================================ //
    // === Activation === //
    // ================================================================================================ //
    public void Activate(Team team, Vector3 position)
    {
        this.team = team;

        // Set Fighter-Root's layer to enemy_mask.
        switch (team)
        {
            case Team.RED:
                fighters_mask = 1 << 18;
                terminals_mask = (1 << 19) + (1 << 21);
                break;
            case Team.BLUE:
                fighters_mask = 1 << 17;
                terminals_mask = (1 << 19) + (1 << 20);
                break;
        }

        standby = false;
        zako_left = fighter_in_array;
        transform.position = position;

        gameObject.SetActive(true);

        switch (BattleInfo.rule)
        {
            case Rule.BATTLE_ROYAL:
                // Go to random subtarget position when started.
                SetDestination(SubTarget.GetRandomPosition());
                break;

            case Rule.TERMINAL_CONQUEST:
                float my_team_point_per_sec = 0;
                float opponent_team_point_per_sec = 0;
                switch (team)
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

                // When winning : defence ally terminal.
                if (my_team_point_per_sec > opponent_team_point_per_sec)
                    targetTerminal = TerminalManager.I.GetAllyTerminals(team).RandomChoice();
                // When losing or draw : attack opponent terminal.
                else
                    targetTerminal = TerminalManager.I.GetOpponentTerminals(team).RandomChoice();
                SetDestination(targetTerminal.transform.position, true);
                break;
        }
    }



    // ================================================================================================ //
    // === Main Loop === //
    // ================================================================================================ //
    void FixedUpdate()
    {
        if (standby) return;

        if (zako_left == 0)
        {
            standby = true;
            gameObject.SetActive(false);
            return;
        }

        DetectFighters();
        ChangeCondition();
        OnEachCondition();
        MoveTowardNextDestination();
        CheckIfArrived();
    }



    // ================================================================================================ //
    // === Condition === //
    // ================================================================================================ //

    enum Conditions { ATTACK, SEARCH, ATTACK_TERMINAL, DEFENCE_TERMINAL }
    [SerializeField] Conditions condition = Conditions.SEARCH;
    [SerializeField] Conditions prev_condition = Conditions.SEARCH;

    GameObject targetFighter;
    Terminal targetTerminal;

    void ChangeCondition()
    {
        prev_condition = condition;
        switch (BattleInfo.rule)
        {
            case Rule.BATTLE_ROYAL:

                // When there are opponents in front.
                if (detected_fighters_nos.Count > 0)
                {
                    // Set target fighter if null.
                    if (targetFighter == null)
                    {
                        int target_no = detected_fighters_nos[0];
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

            case Rule.TERMINAL_CONQUEST:
                // Set target to terminal.
                // Judge whether ally team is currently winning or not.
                float my_team_point_per_sec = 0;
                float opponent_team_point_per_sec = 0;
                switch (team)
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

                // When winning : defence ally terminal.
                if (my_team_point_per_sec > opponent_team_point_per_sec)
                {
                    // When current target is null or opponent, set new target terminal.
                    if (!targetTerminal || targetTerminal.team != team)
                    {
                        targetTerminal = TerminalManager.I.GetAllyTerminals(team).RandomChoice();
                    }

                    // If detected opponent fighter during defencing ally terminal, chase and attack it.
                    if (detected_fighters_nos.Count > 0)
                    {
                        if (!targetFighter)
                        {
                            int targetNo = detected_fighters_nos[0];
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

                // When losing or draw : attack opponent terminal.
                else
                {
                    // Set opponent terminal with least HP as a target when current target is null or is ally terminal.
                    if (targetTerminal == null || targetTerminal.team == team)
                    {
                        targetTerminal = TerminalManager.I.GetNearestOpponentTerminal(team, transform.position);
                    }
                    condition = Conditions.ATTACK_TERMINAL;
                }
                break;
        }
    }

    void OnEachCondition()
    {
        switch (condition)
        {
            case Conditions.ATTACK:
                // Keep updating final destination (Vector3) to targetFighter's latest position.
                if (targetFighter != null)
                {
                    SetDestination(targetFighter.transform.position);
                }
                break;

            case Conditions.SEARCH:
                if (arrived)
                {
                    // Keep on going for final destination.
                    if (bypassing)
                    {
                        SetDestination(final_destination);
                    }
                    // Set final destination to random position.
                    else
                    {
                        SetDestination(SubTarget.GetRandomPosition());
                    }
                }

                // If previous condition was ATTACK, just set new final_destination. (No need to arrive at target_fighters position any more.)
                else if (prev_condition == Conditions.ATTACK)
                {
                    SetDestination(SubTarget.GetRandomPosition());
                }

                break;


            // Only On Terminal Conquest.
            case Conditions.ATTACK_TERMINAL:
                if (arrived)
                {
                    if (bypassing)
                    {
                        SetDestination(final_destination, destination_is_terminal);
                    }
                    else
                    {
                        if (destination_is_terminal) SetDestination(targetTerminal.GetRandomSubTargetPositionAround());
                        else SetDestination(targetTerminal.transform.position, true);
                    }
                }

                // If previous condition was ATTACK, just set new final_destination. (No need to arrive at target_fighters position any more.)
                else if (prev_condition == Conditions.ATTACK)
                {
                    SetDestination(targetTerminal.transform.position, true);
                }

                break;

            case Conditions.DEFENCE_TERMINAL:
                if (arrived)
                {
                    if (bypassing)
                    {
                        SetDestination(final_destination);
                    }
                    else
                    {
                        SetDestination(targetTerminal.GetRandomSubTargetPositionAround());
                    }
                }

                // If previous condition was ATTACK, just set new final_destination. (No need to arrive at target_fighters position any more.)
                else if (prev_condition == Conditions.ATTACK)
                {
                    SetDestination(targetTerminal.GetRandomSubTargetPositionAround());
                }

                break;
        }
    }



    // ================================================================================================ //
    // === Movement === //
    // ================================================================================================ //

    [Header("Movement")]
    public float speed;
    public float angular_speed;
    void MoveTowardNextDestination()
    {
        // Rotation.
        Vector3 relative_to_next = next_destination - transform.position;
        Vector3 relativeEulerAngle = Quaternion.LookRotation(((relative_to_next == Vector3.zero) ? transform.forward : relative_to_next)).eulerAngles;
        Quaternion lookRotation = Quaternion.Euler(relativeEulerAngle.x, relativeEulerAngle.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, angular_speed * Time.deltaTime);

        // Move Forward.
        transform.position = Vector3.MoveTowards(transform.position, transform.position + (transform.forward * speed * Time.deltaTime), speed);
    }



    // ================================================================================================ //
    // === Destination Setting === //
    // ================================================================================================ //

    bool bypassing;
    [SerializeField] Vector3 final_destination, next_destination;

    const int bypass_cashe_count = 5;
    int next_cashe_index = 0;
    Vector3[] bypassedPoints = new Vector3[bypass_cashe_count];

    bool destination_is_terminal = false;

    void SetDestination(Vector3 final_destination, bool destination_is_terminal = false)
    {
        // Set final destination.
        this.final_destination = final_destination;

        // Set arrived false.
        arrived = false;

        // Set whether destination is terminal.
        this.destination_is_terminal = destination_is_terminal;

        // Check if there are obstacles in the way.
        RaycastHit hit;
        Vector3 my_pos = transform.position;
        bool obstacle_in_way = Physics.Raycast(my_pos, final_destination - my_pos, out hit, Vector3.Magnitude(final_destination - my_pos), FighterCondition.obstacles_mask);

        // Do not count targetTerminal as obstacle.
        if (targetTerminal && hit.transform == targetTerminal.transform) obstacle_in_way = false;

        // If obstacle is in the way.
        if (obstacle_in_way)
        {
            // Set as bypassing.
            bypassing = true;

            // Search for sub targets around until trial reaches max_trial.
            List<Vector3> subTargetsAround = new List<Vector3>();       // Check for obstacles in way.
            List<Vector3> subTargetsAround_weak = new List<Vector3>();  // Does not check for obstacles.
            const float searchRadius_orig = 150;
            const int max_trial = 3;
            for (int trial = 1; trial <= max_trial; trial++)
            {
                // Expand search radius on each trial.
                float searchRadius = searchRadius_orig * trial;

                // Search sub targets around.
                subTargetsAround_weak = Physics.OverlapSphere(my_pos, searchRadius, SubTarget.mask, QueryTriggerInteraction.Collide)
                    .Select(s => s.transform.position)
                    .Where(t => !bypassedPoints.Contains(t))
                    .ToList();
                subTargetsAround = subTargetsAround_weak
                    .Where(t => !Physics.Raycast(my_pos, t - my_pos, Vector3.Magnitude(t - my_pos), FighterCondition.obstacles_mask))
                    .ToList();

                // break when sub target was found.
                if (subTargetsAround.Count > 0)
                {
                    break;
                }
            }

            if (subTargetsAround.Count < 1)
            {
#if UNITY_EDITOR
                Debug.LogWarning("サブターゲットの検索に失敗しました. subTargetsAround_weak: " + subTargetsAround_weak.Count, gameObject);
#endif
                // If no sub targets were found, relax condiion.
                subTargetsAround = subTargetsAround_weak;
            }

            // Set next destination when sub targets were found.
            float min_degree = 360;
            foreach (Vector3 subTargetAround in subTargetsAround)
            {
                Vector3 relative_to_subAround = subTargetAround - my_pos;
                float degree_to_final = Mathf.Abs(Vector3.SignedAngle(final_destination - my_pos, relative_to_subAround, Vector3.up));
                if (degree_to_final < min_degree)
                {
                    min_degree = degree_to_final;
                    next_destination = subTargetAround;
                }
            }
        }

        // If no obstacles are in the way.
        else
        {
            bypassing = false;
            next_destination = final_destination;
        }
    }



    // ================================================================================================ //
    // === Arrival Check === //
    // ================================================================================================ //

    bool arrived;
    float sqr_arrival_distance = 25 * 25;

    void CheckIfArrived()
    {
        // Do nothing when already arrived.
        if (arrived) return;

        // Expand distance border when destination is terminal.
        float sqr_distanceBorder = destination_is_terminal ? this.sqr_arrival_distance * 3 * 3 : this.sqr_arrival_distance;

        // When arrived at next destination.
        if (Vector3.SqrMagnitude(next_destination - transform.position) < sqr_distanceBorder) OnArrival();
    }

    void OnArrival()
    {
        arrived = true;

        // When arrived at bypass point.
        if (bypassing)
        {
            bypassedPoints[next_cashe_index] = next_destination;
            next_cashe_index = (next_cashe_index + 1) % bypass_cashe_count;
        }

        // When arrived at final destination.
        else
        {
            for (int k = 0; k < bypass_cashe_count; k++) bypassedPoints[k] = Vector3.zero;
        }
    }



    // ================================================================================================ //
    // === Fighter Detection === //
    // ================================================================================================ //

    [Header("Fighter Detection")]
    public float detectDistance;
    public float detectAngle;

    public List<int> detected_fighters_nos { get; private set; } = new List<int>();
    LayerMask fighters_mask;
    LayerMask terminals_mask;

    void DetectFighters()
    {
        Vector3 my_pos = transform.position;

        // Detect Fighter-Root GameObject in order to detect target regardless of oponents shield activation.
        Collider[] colliders = Physics.OverlapSphere(my_pos, detectDistance, fighters_mask);

        // Detect targets, and set them to detected_fighter_nos.
        if (colliders.Length > 0)
        {
            var possibleTargets = colliders.Select(t => t.transform);

            // Get fighter number of targets.
            detected_fighters_nos = possibleTargets.Where(p =>

                // Check if target is inside homing range.
                Vector3.Angle(transform.forward, p.position - my_pos) < detectAngle &&

                // Check if there are no obstacles (terrain + terminals) between self and target.
                !Physics.Raycast(my_pos, p.position - my_pos, Vector3.Magnitude(p.position - my_pos), FighterCondition.obstacles_mask))

                // Get fighter number of target from its name.
                .Select(r => int.Parse(r.name))

                // Filter dead fighters.
                .Where(no => !ParticipantManager.I.fighterInfos[no].fighterCondition.isDead).ToList();
        }
        else
        {
            // Clean up list.
            detected_fighters_nos.Clear();
        }
    }



    // void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawLine(transform.position, final_destination);

    //     if (bypassing)
    //     {
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawLine(transform.position, next_destination);
    //     }
    // }
}
