using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class ZoneController : MonoBehaviour
{
    [Header("CP")]
    [SerializeField, MinValue(0)] protected float cp;
    public float Cp
    {
        get { return cp; }
        set { cp = Mathf.Max(0, value); }
    }

    [SerializeField, MinValue(0)] protected float cpToEnterZone = 10000;
    public float CpToEnterZone
    {
        get { return cpToEnterZone; }
        set { cpToEnterZone = Mathf.Max(0, value); }
    }

    [SerializeField, MinValue(0)] protected float cpLossPerSec = 5;
    public float CpLossPerSec
    {
        get { return cpLossPerSec; }
        set { cpLossPerSec = Mathf.Max(0, value); }
    }

    // CP to maintain on reset. [0.0 (maintain none) ~ 1.0 (maintain all)]
    [SerializeField, Range(0, 1)] protected float cpMaintain = 0f;
    public void SetCpMaintain(float cp_maintain)
    {
        cpMaintain = Mathf.Clamp01(cp_maintain);
    }

    // Permanent bonus applyed when obtained cp.
    [SerializeField, MinValue(1)] protected float cpBonus = 1f;
    public void MultiplyCpBonus(float bonus)
    {
        float bonus_multiplier = Mathf.Max(1, bonus);
        cpBonus *= bonus_multiplier;
    }


    [Header("Combo")]
    public int combo;
    public float comboTimeout = 2.5f;


    [Header("Zone")]
    public bool isZone;
    public float zoneDuration { get; set; } = 15;

    float comboTimer;
    float zoneTimer;

    public Attack attack { get; protected set; }
    public bool has_comboBoostA { get; set; } = false;
    public bool has_comboBoostD { get; set; } = false;
    public bool has_comboBoostS { get; set; } = false;


    protected virtual void Start()
    {
        ResetCp();
        attack = GetComponentInChildren<Attack>();
        attack.OnKillCallback += OnKill;
        attack.fighterCondition.OnDeathCallback += OnDeath;
    }

    protected virtual void FixedUpdate()
    {
        if (!attack.IsOwner) return;
        if (attack.fighterCondition.isDead) return;
        UpdateCp();
    }



    // CP ///////////////////////////////////////////////////////////////////////////////////////////////
    protected void ResetCp(bool maintain_cp = false)
    {
        // Maintain some cp based on cpMaintain
        cp *= maintain_cp ? cpMaintain : 0;
        combo = 0;
        comboTimer = comboTimeout;
        zoneTimer = zoneDuration;
    }

    protected virtual void UpdateCp()
    {
        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0)
        {
            combo = 0;
            comboTimer = comboTimeout;
        }

        if (isZone)
        {
            zoneTimer -= Time.deltaTime;
            cp = zoneTimer / zoneDuration * cpToEnterZone;
            if (zoneTimer <= 0)
            {
                EndZone();
            }
        }

        else
        {
            if (cp >= cpToEnterZone)
            {
                StartZone();
                return;
            }
            else if (cp > 0)
            {
                cp -= cpLossPerSec * Time.deltaTime;
                cp = (cp < 0) ? 0 : cp;
            }
        }
    }

    protected float CalculateCpBonus(int combo)
    {
        float cp_bonus = 1;

        // Magnify CP by combo.
        const int combo_thresh = 3;
        if (combo >= combo_thresh)
        {
            // 3:x1.1, 4:x1.2, ... , 12:x2.0, 13:x2.0
            cp_bonus = 1 + 0.1f * (combo - combo_thresh + 1);
            cp_bonus = Mathf.Clamp(cp_bonus, 1.0f, 2.0f);
        }

        // Magnify CP by permanent cp bonus. (Bonus of Ability)
        cp_bonus *= cpBonus;

        return cp_bonus;
    }



    // Combo ////////////////////////////////////////////////////////////////////////////////////////////
    protected void IncrementCombo()
    {
        combo++;
        comboTimer = comboTimeout;

        // Combo Boost Ability. (combo == 5, 10, 15, ...)
        if (combo % 5 == 0 && combo > 0)
        {
            const int boost_grade = 1;
            const float boost_duration = 10;
            if (has_comboBoostA)
                attack.fighterCondition.power.Grade(boost_grade, boost_duration);
            if (has_comboBoostD)
                attack.fighterCondition.defence.Grade(boost_grade, boost_duration);
            if (has_comboBoostS)
                attack.fighterCondition.speed.Grade(boost_grade, boost_duration);
        }
    }



    // Zone /////////////////////////////////////////////////////////////////////////////////////////////
    protected virtual void StartZone()
    {
        isZone = true;
        zoneTimer = zoneDuration;
        cp = cpToEnterZone;
    }

    protected virtual void EndZone()
    {
        isZone = false;
        zoneTimer = zoneDuration;
        cp = 0;
    }



    // Death & Kill /////////////////////////////////////////////////////////////////////////////////////
    protected virtual void OnKill(int killed_no)
    {
        if (!attack.IsOwner) return;

        float cp_obtained;
        if (killed_no < 0)
            return;
        else if (killed_no < GameInfo.MAX_PLAYER_COUNT)
            cp_obtained = GameInfo.CP_FIGHTER;
        else
            cp_obtained = GameInfo.CP_ZAKO;

        IncrementCombo();
        float cp_bonus = CalculateCpBonus(combo);
        cp += cp_obtained * cp_bonus;
    }

    void OnDeath(int killer_no, string cause_of_death)
    {
        bool maintain_cp = true;
        ResetCp(maintain_cp);
        EndZone();
    }
}
