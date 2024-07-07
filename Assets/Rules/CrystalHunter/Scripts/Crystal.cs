using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Linq;

public class Crystal : MonoBehaviour
{
    public int id;

    [ShowNativeProperty]
    public Team team { get; private set; } = Team.NONE;

    public enum State { PLACED, CARRIED, RETURNING }

    [ShowNativeProperty]
    public State state { get; private set; } = State.PLACED;

    public Vector3 placement_pos { get; set; }
    public FighterCondition fighterCondition { get; set; }

    [SerializeField] float y_offset = 13.5f;
    [SerializeField] float maxReturnSpeed, maxChaseSpeed;
    [SerializeField] float hpDecreaseSpeed;
    [SerializeField] GameObject red_crystal, blue_crystal;
    [SerializeField] ParticleSystem red_get, blue_get;

    [Button]
    void InitCrystal()
    {
        red_crystal = transform.Find("Red").gameObject;
        blue_crystal = transform.Find("Blue").gameObject;
        red_get = red_crystal.transform.Find("Crystal_Get").GetComponent<ParticleSystem>();
        blue_get = blue_crystal.transform.Find("Crystal_Get").GetComponent<ParticleSystem>();
    }


    void OnTriggerEnter(Collider col)
    {
        // Do nothing when already carried.
        if (state == State.CARRIED)
        {
            return;
        }

        GameObject col_obj = col.gameObject;

        // Return when col was not fighter
        string col_tag = col_obj.tag;
        if (col_tag != "Player" && col_tag != "AI")
        {
            return;
        }

        // Try to get FighterCondition.
        FighterCondition fighter_condition;
        if (col.TryGetComponent<FighterCondition>(out fighter_condition))
        {
            // Return when the fighter is same team.
            Team fighter_team = fighter_condition.fighterTeam.Value;
            if (fighter_team == team)
            {
                return;
            }

            // Return if the fighter is already carring other crystal.
            int fighter_no = fighter_condition.fighterNo.Value;
            if (CrystalManager.I.carrierNos.Contains(fighter_no))
            {
                return;
            }

            // Set this crystal carried.
            CarryCrystal(fighter_condition);
        }
        else
        {
            Debug.LogError("Could not get component FighterCondition!!", col_obj);
        }
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.PLACED:
                break;

            case State.CARRIED:
                Vector3 body_pos = fighterCondition.body.transform.position;
                Vector3 target_pos = body_pos + Vector3.up * y_offset;
                GoToTarget(target_pos, maxChaseSpeed);
                fighterCondition.HPDecreaser(hpDecreaseSpeed * Time.deltaTime);
                fighterCondition.receiver.LastShooterDetector(-1, FighterCondition.SPECIFIC_DEATH_CRYSTAL);
                break;

            case State.RETURNING:
                GoToTarget(placement_pos, maxReturnSpeed);
                if (transform.position == placement_pos)
                {
                    state = State.PLACED;
                }
                break;
        }
    }

    void GoToTarget(Vector3 target_pos, float max_speed)
    {
        Vector3 relative_pos = target_pos - transform.position;
        Vector3 dir = relative_pos.normalized;
        float dist = relative_pos.magnitude;
        float return_dist = dist;
        return_dist = Mathf.Clamp(return_dist, 0, max_speed * Time.deltaTime);
        transform.position += dir * return_dist;
    }


    // Set state of crystal via these methods.
    public void CarryCrystal(FighterCondition fighterCondition)
    {
        state = State.CARRIED;
        this.fighterCondition = fighterCondition;
        fighterCondition.attack.LockAllSkills(true);
        CrystalManager.I.carrierNos[id] = fighterCondition.fighterNo.Value;
        switch (team)
        {
            case Team.RED:
                red_get.Play();
                break;

            case Team.BLUE:
                blue_get.Play();
                break;
        }
    }
    public void ReleaseCrystal()
    {
        state = State.RETURNING;
        fighterCondition.attack.LockAllSkills(false);
        fighterCondition = null;
        CrystalManager.I.carrierNos[id] = -1;
    }


    // Set team of crystal via this method.
    public void ChangeTeam(Team new_team)
    {
        team = new_team;
        switch (new_team)
        {
            case Team.RED:
                red_crystal.SetActive(true);
                blue_crystal.SetActive(false);
                red_get.Play();
                break;

            case Team.BLUE:
                red_crystal.SetActive(false);
                blue_crystal.SetActive(true);
                blue_get.Play();
                break;

            default:
                Debug.LogWarning("Crystal team was set to NONE!!", gameObject);
                break;
        }
    }
}
