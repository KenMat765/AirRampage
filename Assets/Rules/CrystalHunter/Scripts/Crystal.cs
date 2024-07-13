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

    public Vector3 placementPos { get; set; }

    [SerializeField] float yOffset = 13.5f;
    [SerializeField] float maxReturnSpeed, maxChaseSpeed;
    [SerializeField] float hpDecreaseSpeed;
    [SerializeField] GameObject crystalRed, crystalBlue;
    [SerializeField] ParticleSystem getEffectRed, getEffectBlue;

    // Fighter Properties.
    FighterCondition fighterCondition;
    SkillExecuter skillExecuter;

    [Button]
    void InitCrystal()
    {
        crystalRed = transform.Find("Red").gameObject;
        crystalBlue = transform.Find("Blue").gameObject;
        getEffectRed = crystalRed.transform.Find("Crystal_Get").GetComponent<ParticleSystem>();
        getEffectBlue = crystalBlue.transform.Find("Crystal_Get").GetComponent<ParticleSystem>();
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
                Vector3 target_pos = body_pos + Vector3.up * yOffset;
                GoToTarget(target_pos, maxChaseSpeed);
                fighterCondition.HPDecreaser(hpDecreaseSpeed * Time.deltaTime);
                fighterCondition.receiver.LastShooterDetector(-1, FighterCondition.SPECIFIC_DEATH_CRYSTAL);
                break;

            case State.RETURNING:
                GoToTarget(placementPos, maxReturnSpeed);
                if (transform.position == placementPos)
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
        skillExecuter = fighterCondition.GetComponentInChildren<SkillExecuter>();
        skillExecuter.LockAllSkills(true);
        CrystalManager.I.carrierNos[id] = fighterCondition.fighterNo.Value;
        switch (team)
        {
            case Team.RED:
                getEffectRed.Play();
                break;

            case Team.BLUE:
                getEffectBlue.Play();
                break;
        }
    }

    public void ReleaseCrystal()
    {
        state = State.RETURNING;
        skillExecuter.LockAllSkills(false);
        fighterCondition = null;
        skillExecuter = null;
        CrystalManager.I.carrierNos[id] = -1;
    }


    // Set team of crystal via this method.
    public void ChangeTeam(Team new_team)
    {
        team = new_team;
        switch (new_team)
        {
            case Team.RED:
                crystalRed.SetActive(true);
                crystalBlue.SetActive(false);
                getEffectRed.Play();
                break;

            case Team.BLUE:
                crystalRed.SetActive(false);
                crystalBlue.SetActive(true);
                getEffectBlue.Play();
                break;

            default:
                Debug.LogWarning("Crystal team was set to NONE!!", gameObject);
                break;
        }
    }
}
