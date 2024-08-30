using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Linq;
using System.Linq.Expressions;

public class Crystal : MonoBehaviour
{
    // This is set in CrystalManager.InitCrystals
    CrystalManager crystalManager;

    [ShowNativeProperty]
    public int id { get; private set; }

    [SerializeField] float yOffset = 13.5f;
    [SerializeField] float maxReturnSpeed, maxChaseSpeed;
    [SerializeField] int score;
    [SerializeField] float hpDecreaseSpeed;
    [SerializeField] GameObject crystalRed, crystalBlue;
    [SerializeField] ParticleSystem getEffectRed, getEffectBlue;

    public int GetScore() { return score; }

    // Fighter Properties.
    FighterCondition fighterCondition;
    Transform bodyTrans;
    Receiver receiver;
    SkillController skillController;
    public int GetCarrierNo()
    {
        return fighterCondition ? fighterCondition.fighterNo.Value : -1;
    }


    // Called in CrystalManager.InitCrystals
    public void Init(CrystalManager manager, int id, Vector3 default_homePos)
    {
        crystalManager = manager;
        this.id = id;

        Transform trans = transform;
        crystalRed = trans.Find("Red").gameObject;
        crystalBlue = trans.Find("Blue").gameObject;
        getEffectRed = trans.Find("Crystal_Get_Red").GetComponent<ParticleSystem>();
        getEffectBlue = trans.Find("Crystal_Get_Blue").GetComponent<ParticleSystem>();

        // Call this to change appearance of crystal.
        SetTeam(team);

        SetHome(default_homePos);
        GoToTarget(homePos, -1);    // Moves immediately when second arg is negative.
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
            if (crystalManager.IsFighterCarryingCrystal(fighter_no))
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
                if (fighterCondition.isDead)
                {
                    ReleaseCrystal();
                    break;
                }
                Vector3 body_pos = bodyTrans.position;
                Vector3 target_pos = body_pos + Vector3.up * yOffset;
                GoToTarget(target_pos, maxChaseSpeed);
                fighterCondition.HPDecreaser(hpDecreaseSpeed * Time.deltaTime);
                receiver.AttackerDetector(-1, FighterCondition.SPECIFIC_DEATH_CRYSTAL);
                break;

            case State.RETURNING:
                GoToTarget(homePos, maxReturnSpeed);
                if (transform.position == homePos)
                {
                    state = State.PLACED;
                }
                break;
        }
    }


    /// <param name="max_speed">Goes to target immediately when negative</param>
    void GoToTarget(Vector3 target_pos, float max_speed)
    {
        Vector3 relative_pos = target_pos - transform.position;
        Vector3 dir = relative_pos.normalized;
        float dist = relative_pos.magnitude;
        float return_dist = dist;
        if (max_speed >= 0)
        {
            return_dist = Mathf.Clamp(return_dist, 0, max_speed * Time.deltaTime);
        }
        transform.position += dir * return_dist;
    }



    // State /////////////////////////////////////////////////////////////////////////////////////
    public enum State { PLACED, CARRIED, RETURNING }

    [ShowNativeProperty]
    public State state { get; private set; } = State.PLACED;

    public void CarryCrystal(FighterCondition fighter_condition)
    {
        state = State.CARRIED;
        fighterCondition = fighter_condition;
        bodyTrans = fighter_condition.transform.Find("fighterbody");
        receiver = fighter_condition.GetComponentInChildren<Receiver>();
        skillController = fighter_condition.GetComponentInChildren<SkillController>();
        skillController.LockAllSkills(true);
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
        skillController.LockAllSkills(false);
        fighterCondition = null;
        bodyTrans = null;
        receiver = null;
        skillController = null;
    }



    // Team /////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] Team team;

    public Team GetTeam()
    {
        return team;
    }

    public void SetTeam(Team new_team)
    {
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
        team = new_team;
    }



    // Home position /////////////////////////////////////////////////////////////////////////////////////
    Vector3 homePos;    // Position where this crystal returns.
    public void SetHome(Vector3 new_homePos)
    {
        homePos = new_homePos;
    }
}
