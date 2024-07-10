using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Linq;
using System;

// Fighterの状態を保持するクラス
// このクラスのプロパティをもとに、Movement、Attack、Receiverを動かす
public abstract class FighterCondition : NetworkBehaviour
{
    // Awake is called BEFORE fighterNo, fighterName, fighterTeam is assigned by ParticipantManager.
    void Awake()
    {
        Hp = defaultHp;
        speed = new FighterStatus(defaultSpeed);
        power = new FighterStatus(defaultPower);
        defence = new FighterStatus(defaultDefence);
    }

    // Start is called AFTER fighterNo, fighterName, fighterTeam is assigned by ParticipantManager.
    protected virtual void Start()
    {
        if (!IsOwner) return;

        SetLayerMasks(fighterTeam.Value);
    }

    protected virtual void FixedUpdate()
    {
        // Only the owner of this fighter calls Update().
        if (!IsOwner) return;

        if (isDead)
        {
            reviveTimer += Time.deltaTime;
            if (reviveTimer > revivalTime)
            {
                reviveTimer = 0;
                Revival();
            }
        }

        else
        {
            if (Hp <= 0)
            {
                Death(receiver.lastShooterNo, receiver.lastCauseOfDeath);
                return;
            }
            speed.Timer();
            power.Timer();
            defence.Timer();
        }
    }



    [Header("Components")]
    public GameObject body;
    public Movement movement;
    public Attack attack;
    public Receiver receiver;
    public RadarIconController radarIcon;



    // Layer Masks //////////////////////////////////////////////////////////////////////////////////////////////////
    public static LayerMask obstacles_mask { get; private set; } = GameInfo.terrainMask + GameInfo.structureMask + Terminal.allMask;
    public LayerMask fighters_mask { get; private set; }
    public LayerMask terminals_mask { get; private set; }
    protected void SetLayerMasks(Team my_team)
    {
        // Set Fighter-Root's layer to enemy_mask.
        switch (my_team)
        {
            case Team.RED:
                fighters_mask = GameInfo.blueFighterMask;
                terminals_mask = Terminal.defaultMask + Terminal.blueMask;
                break;

            case Team.BLUE:
                fighters_mask = GameInfo.redFighterMask;
                terminals_mask = Terminal.defaultMask + Terminal.redMask;
                break;

            default: Debug.LogError("FighterConditionにチームが設定されていません!!"); break;
        }
    }



    // ParticipantManagerからセット ///////////////////////////////////////////////////////////////////////////////////
    public NetworkVariable<int> fighterNo { get; set; } = new NetworkVariable<int>();   // How many participant is this fighter.
    public NetworkVariable<FixedString32Bytes> fighterName { get; set; } = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<Team> fighterTeam { get; set; } = new NetworkVariable<Team>();



    // Status //////////////////////////////////////////////////////////////////////////////////////////////////
    [Header("Current Status")]
    public float Hp; // Only the owner of this fighter knows HP.
    public FighterStatus speed;  // Only reffered by owner. (Movement)
    public FighterStatus power;  // Reffered in every clients. (Weapon.Activate)
    public FighterStatus defence;  // Only reffered by owner. (Receiver.Damage)

    [Header("Default Status")]
    public float defaultHp;
    public float defaultSpeed;
    public float defaultPower;
    public float defaultDefence;

    public virtual void HPDecreaser(float deltaHP)
    {
        Hp -= deltaHP;
        Hp = Mathf.Clamp(Hp, 0, defaultHp);
    }

    [ServerRpc]
    public void HpDecreaser_UIServerRPC(float curHp)
    {
        float normHp = curHp.Normalize(0, defaultHp);
        HpDecreaser_UIClientRPC(normHp);
    }

    [ClientRpc]
    public void HpDecreaser_UIClientRPC(float normHp)
    {
        uGUIMannager.I.HPDecreaser_UI(fighterNo.Value, normHp);
    }



    // Combo & CP (Concentration Point) ////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Variables
    public float cp { get; set; }   // CP currently have.
    public int combo { get; set; }
    protected float combo_timer;
    float zone_timer;
    public bool isZone { get; set; }

    // Constants
    public float full_cp { get; set; } = 10000; // CP necessary to get in the Zone.
    public abstract float my_cp { get; set; }   // CP to give to opponent when killed.
    public float dec_cp_per_sec { get; set; } = 5;  // Decreasing amount of CP over time.
    public float cp_maintain { get; set; } = 0f;    // 0.0 (maintain none) ~ 1.0 (maintain all)
    public float default_combo_timer { get; set; } = 2.5f;  // Time until the combo runs out.
    public float default_zone_timer { get; set; } = 15; // Duration of zone.

    protected void CPStart()
    {
        cp = 0;
        combo = 0;
        combo_timer = default_combo_timer;
        zone_timer = default_zone_timer;
    }

    protected virtual void CPUpdate()
    {
        // Stop combo when combo timer is over. (combo is independent of Zone.)
        combo_timer -= Time.deltaTime;
        if (combo_timer <= 0)
        {
            combo = 0;
            combo_timer = default_combo_timer;
        }

        if (isZone)
        {
            // Decrease zone_timer.
            zone_timer -= Time.deltaTime;
            cp = zone_timer / default_zone_timer * full_cp;
            if (zone_timer <= 0)
            {
                EndZone();
            }
        }

        else
        {
            // Into Zone when cp is over full_cp.
            if (cp >= full_cp)
            {
                StartZone();
                return;
            }
            // Decrease cp by dec_cp_per_sec.
            cp -= dec_cp_per_sec * Time.deltaTime;
            cp = (cp < 0) ? 0 : cp;
        }
    }

    void CPResetter()
    {
        cp *= cp_maintain;
        combo = 0;
        combo_timer = default_combo_timer;
        zone_timer = default_zone_timer;
    }

    public virtual float Combo(float inc_cp)
    {
        // Increase combo. (combo is independent of Zone.)
        combo++;
        const int combo_thresh = 3;
        combo_timer = default_combo_timer;

        // Combo Boost Ability. (combo == 5, 10, 15, ...)
        if (combo % 5 == 0 && combo > 0)
        {
            const int boost_grade = 1;
            const float boost_duration = 10;
            if (has_comboBoostA)
            {
                power.Grade(boost_grade, boost_duration);
            }
            if (has_comboBoostD)
            {
                defence.Grade(boost_grade, boost_duration);
            }
            if (has_comboBoostS)
            {
                speed.Grade(boost_grade, boost_duration);
            }
        }

        // Do not increase cp when Zone.
        if (isZone) return 0;

        // Magnify CP if combo is over combo_thresh.
        float cp_magnif = 1;
        if (combo >= combo_thresh)
        {
            // 3:x1.1, 4:x1.2, ... , 12:x2.0, 13:x2.0
            cp_magnif = 1 + 0.1f * (combo - combo_thresh + 1);
            cp_magnif = Mathf.Clamp(cp_magnif, 1.0f, 2.0f);
        }
        inc_cp *= cp_magnif;

        // Bonus of ability.
        if (has_deepAbsorb)
        {
            inc_cp *= 1.5f;
        }

        // Increase cp.
        cp += inc_cp;

        // Return magnification of CP. (Used in uGUIManager)
        return cp_magnif;
    }

    protected virtual void StartZone()
    {
        isZone = true;
        zone_timer = default_zone_timer;
        cp = full_cp;
    }

    protected virtual void EndZone()
    {
        isZone = false;
        zone_timer = default_zone_timer;
        cp = 0;
    }



    // Death & Revival //////////////////////////////////////////////////////////////////////////////////////////////
    public bool isDead { get; private set; }
    public abstract float revivalTime { get; set; }
    public float reviveTimer { get; private set; }

    // Causes of death.
    public const string DEATH_NORMAL_BLAST = "Normal Blast";
    public const string SPECIFIC_DEATH_CANNON = "Cannon";               // Specific death (Death other than enemy attacks)
    public const string SPECIFIC_DEATH_CRYSTAL = "Crystal Kill";        // Specific death (Death other than enemy attacks)
    public const string SPECIFIC_DEATH_COLLISION = "Collision Crash";   // Specific death (Death other than enemy attacks)
    static string[] specificDeath = { SPECIFIC_DEATH_CANNON, SPECIFIC_DEATH_CRYSTAL, SPECIFIC_DEATH_COLLISION };
    public static bool IsSpecificDeath(string causeOfDeath) { return specificDeath.Contains(causeOfDeath); }

    // Processes run at the time of death. (Should be called on every clients)
    protected virtual void OnDeath(int destroyerNo, string causeOfDeath)
    {
        if (isDead)
        {
            return;
        }
        isDead = true;

        movement.OnDeath();
        attack.OnDeath();
        receiver.OnDeath(destroyerNo, causeOfDeath);
        radarIcon.Visualize(false);

        // If specific cause of death. (Not killed by enemy)
        if (IsSpecificDeath(causeOfDeath))
        {
            return;
        }

        // Give combo to destroyer. (Only the owner of the destroyer needs to count combos)
        FighterCondition destroyer_condition = ParticipantManager.I.fighterInfos[destroyerNo].fighterCondition;
        if (destroyer_condition.IsOwner)
        {
            destroyer_condition.Combo(my_cp);
        }

        if (IsHost)
        {
            BattleConductor.I.OnFighterDestroyed(this, destroyerNo, causeOfDeath);
        }
    }

    // Method to call OnDeath() at all clients.
    public void Death(int destroyerNo, string causeOfDeath)
    {
        // Call for yourself.
        OnDeath(destroyerNo, causeOfDeath);

        // Call for clones at other clients.
        if (IsHost)
            DeathClientRpc(OwnerClientId, destroyerNo, causeOfDeath);
        else
            DeathServerRpc(OwnerClientId, destroyerNo, causeOfDeath);
    }

    [ServerRpc]
    void DeathServerRpc(ulong senderId, int destroyerNo, string causeOfDeath)
    {
        DeathClientRpc(senderId, destroyerNo, causeOfDeath);
    }

    [ClientRpc]
    void DeathClientRpc(ulong senderId, int destroyerNo, string causeOfDeath)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        OnDeath(destroyerNo, causeOfDeath);
    }

    // Processes run at the time of revival. (Should be called on every clients)
    protected virtual void OnRevival()
    {
        isDead = false;

        movement.OnRevival();
        attack.OnRevival();
        receiver.OnRevival();

        if (IsOwner)
        {
            Hp = defaultHp;
            speed.Reset();
            power.Reset();
            defence.Reset();
            CPResetter();
            EndZone();
        }
    }

    // Method to call OnRevival() at all clients.
    public void Revival()
    {
        // Call for yourself.
        OnRevival();

        // Call for clones at other clients.
        if (IsHost)
            RevivalClientRpc(OwnerClientId);
        else
            RevivalServerRpc(OwnerClientId);
    }

    [ServerRpc]
    void RevivalServerRpc(ulong senderId)
    {
        RevivalClientRpc(senderId);
    }

    [ClientRpc]
    void RevivalClientRpc(ulong senderId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        OnRevival();
    }

    // Tell uGUIManager to report death of this fighter. (Called from Player and AI fighters)
    protected void ReportDeath(int destroyerNo, string causeOfDeath)
    {
        string my_name = fighterName.Value.ToString();
        Team my_team = fighterTeam.Value;

        // Specific cause of death.
        if (IsSpecificDeath(causeOfDeath))
        {
            uGUIMannager.I.BookRepo(causeOfDeath, my_name, my_team, causeOfDeath);
            return;
        }

        // Death by enemy attacks.
        string destroyer_name = ParticipantManager.I.fighterInfos[destroyerNo].fighterCondition.fighterName.Value.ToString();
        uGUIMannager.I.BookRepo(destroyer_name, my_name, my_team, causeOfDeath);
    }



    // Special Abilities //////////////////////////////////////////////////////////////////////////////////////////////
    public bool has_skillKeep { get; set; } = false;
    public bool has_deepAbsorb { get; set; } = false;
    public bool has_comboBoostA { get; set; } = false;
    public bool has_comboBoostD { get; set; } = false;
    public bool has_comboBoostS { get; set; } = false;
}



public class FighterStatus
{
    public float value { get; private set; }
    public float defaultValue { get; private set; }

    // (Debuff) -3 << 0 >> 3 : (Buff)
    public int grade { get; private set; }
    public float gradeDuration { get; private set; }
    float gradeTimer;

    // Use this when you want to temporarily assign a different value to the status, suspending updates based on grade.
    Dictionary<Guid, float> tmpStatusStack;

    public FighterStatus(float defaultValue)
    {
        this.defaultValue = defaultValue;
        tmpStatusStack = new Dictionary<Guid, float>();
        Reset();
    }

    public void Reset()
    {
        value = defaultValue;
        grade = 0;
        gradeDuration = 0;
        gradeTimer = 0;
        tmpStatusStack.Clear();
    }

    public void Timer()
    {
        if (grade != 0)
        {
            gradeTimer += Time.deltaTime;
            if (gradeTimer > gradeDuration)
            {
                Reset();
            }
        }
    }

    public void Grade(int delta_grade, float duration)
    {
        // Update grade.
        int pre_grade = grade;
        grade += delta_grade;
        grade = Mathf.Clamp(grade, -3, 3);

        // Update grade duration.
        if (grade == 0)
        {
            gradeTimer = 0;
            gradeDuration = 0;
        }
        else
        {
            if (pre_grade == 0)
            {
                gradeTimer = 0;
                gradeDuration = duration;
            }
            else
            {
                // When buffs and debuffs are swapped.
                bool sign_flipped = pre_grade * grade < 0;
                if (sign_flipped)
                {
                    gradeTimer = 0;
                    gradeDuration = duration;
                }
                else
                {
                    gradeDuration += duration;
                }
            }
        }

        // Update status value only if temporary status is none.
        if (tmpStatusStack.Count == 0)
        {
            UpdateStatusByGrade();
        }
    }

    /// <returns>Guid used to remove added temp value.</returns>
    public Guid ApplyTempStatus(float tmp_value)
    {
        Guid guid = Guid.NewGuid();
        tmpStatusStack[guid] = tmp_value;
        value = tmp_value;
        return guid;
    }

    /// <param name="guid">Use the same guid published from ApplyTempStatus.</param>
    public void RemoveTempStatus(Guid guid)
    {
        if (!tmpStatusStack.ContainsKey(guid))
        {
            return;
        }
        tmpStatusStack.Remove(guid);

        // If all temporary status were removed, resume updating status by grade.
        if (tmpStatusStack.Count == 0)
        {
            UpdateStatusByGrade();
        }

        // If temporary status still remains, apply the last value to current status.
        else
        {
            float tmp_value = tmpStatusStack.Last().Value;
            value = tmp_value;
        }
    }

    void UpdateStatusByGrade()
    {
        float multiplier = 1.0f;
        switch (grade)
        {
            case -3: multiplier = 1.0f / 2.0f; break;
            case -2: multiplier = 1.0f / 1.5f; break;
            case -1: multiplier = 1.0f / 1.2f; break;
            case 1: multiplier = 1.2f; break;
            case 2: multiplier = 1.5f; break;
            case 3: multiplier = 2.0f; break;
        }
        value = defaultValue * multiplier;
    }
}