using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Netcode;
using Unity.Collections;
using NaughtyAttributes;

// Fighterの状態を保持するクラス
// このクラスのプロパティをもとに、Movement、Attack、Receiverを動かす
public abstract class FighterCondition : NetworkBehaviour
{
    protected virtual void Start()
    {
        // Only the owner refers to HP, speed, defence, and power.
        if (!IsOwner) return;
        SetLayerMasks(fighterTeam.Value);
        HPResetter();
        SpeedResetter();
        PowerResetter();
        DefenceResetter();
    }

    protected virtual void Update()
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
            if (HP <= 0)
            {
                Death(receiver.lastShooterNo, receiver.lastSkillName);
                return;
            }
            SpeedTimeUpdate();
            PowerTimeUpdate();
            DefenceTimeUpdate();
        }
    }



    [Header("Components")]
    public GameObject body;
    public Movement movement;
    public Attack attack;
    public Receiver receiver;
    public RadarIconController radarIcon;



    // Layer Masks //////////////////////////////////////////////////////////////////////////////////////////////////
    public static LayerMask obstacles_mask { get { return GameInfo.terrainMask + Terminal.defaultMask + Terminal.redMask + Terminal.blueMask; } }
    public LayerMask fighters_mask { get; private set; }
    public LayerMask terminals_mask { get; private set; }
    protected void SetLayerMasks(Team my_team)
    {
        // Set Fighter-Root's layer to enemy_mask.
        switch (my_team)
        {
            case Team.RED:
                fighters_mask = 1 << 18;
                terminals_mask = (1 << 19) + (1 << 21);
                break;

            case Team.BLUE:
                fighters_mask = 1 << 17;
                terminals_mask = (1 << 19) + (1 << 20);
                break;

            default: Debug.LogError("FighterConditionにチームが設定されていません!!"); break;
        }
    }



    // ParticipantManagerからセット ///////////////////////////////////////////////////////////////////////////////////
    public NetworkVariable<int> fighterNo = new NetworkVariable<int>();   // How many participant is this fighter.
    public NetworkVariable<FixedString32Bytes> fighterName = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<Team> fighterTeam = new NetworkVariable<Team>();



    // HP ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Only the owner of this fighter knows HP.
    [Header("Current Status")]
    public float HP;
    public bool isDead { get; private set; }

    // Only the owner of this fighter initializes HP, because HP is linked among all clones.
    void HPResetter() => HP = defaultHP;

    // Decreaser is called only from the owner.
    // Only the owner of this fighter should call this in Repair Device.
    public virtual void HPDecreaser(float deltaHP)    // HPを増加させたい場合は、deltaHP < 0 とする
    {
        HP -= deltaHP;
        HP = Mathf.Clamp(HP, 0, defaultHP);
    }

    [ServerRpc]
    public void HpDecreaser_UIServerRPC(float curHp)
    {
        float normHp = curHp.Normalize(0, defaultHP);
        HpDecreaser_UIClientRPC(normHp);
    }

    [ClientRpc]
    public void HpDecreaser_UIClientRPC(float normHp)
    {
        uGUIMannager.I.HPDecreaser_UI(fighterNo.Value, normHp);
    }



    // Speed, Power, Defence ////////////////////////////////////////////////////////////////////////////////////////
    // Only reffered by owner. (Movement)
    public float speed;
    // Reffered in every clients. (Weapon.Activate)
    public float power;
    // Only reffered by owner. (Receiver.Damage)
    public float defence;

    // Only reffered by owner.
    public int speed_grade { get; private set; }
    public int power_grade { get; private set; }
    public int defence_grade { get; private set; }

    // Only reffered by owner.
    float speed_duration;
    float power_duration;
    float defence_duration;

    // Only reffered by owner.
    float speed_timer;
    float power_timer;
    float defence_timer;

    // Only reffered by owner.
    bool pausing_speed;
    bool pausing_power;
    bool pausing_defence;

    [Header("Default Status")]
    public float defaultHP;
    public float defaultSpeed = 5f;
    public float defaultPower = 1f;
    public float defaultDefence = 1f;

    const float speed_change_duration = 0.35f;


    // Only the owner of this fighter needs to updates timers.
    void SpeedTimeUpdate()
    {
        if (speed_grade != 0)
        {
            speed_timer += Time.deltaTime;
            if (speed_timer > speed_duration)
            {
                SpeedResetter();
            }
        }
    }
    void PowerTimeUpdate()
    {
        if (power_grade != 0)
        {
            power_timer += Time.deltaTime;
            if (power_timer > power_duration)
            {
                PowerResetter();
            }
        }
    }
    void DefenceTimeUpdate()
    {
        if (defence_grade != 0)
        {
            defence_timer += Time.deltaTime;
            if (defence_timer > defence_duration)
            {
                DefenceResetter();
            }
        }
    }

    // Only the owner of this fighter needs to initializes variables.
    void SpeedResetter()
    {
        speed = defaultSpeed;
        pausing_speed = false;
        speed_grade = 0;
        speed_duration = 0;
        speed_timer = 0;
    }
    void PowerResetter()
    {
        power = defaultPower;
        pausing_power = false;
        power_grade = 0;
        power_duration = 0;
        power_timer = 0;
    }
    void DefenceResetter()
    {
        defence = defaultDefence;
        pausing_defence = false;
        defence_grade = 0;
        defence_duration = 0;
        defence_timer = 0;
    }


    // These methods are just for convenience.
    void SpeedUpdater()
    {
        // pausing_speed の時は速度変更を行わない
        if (pausing_speed) return;

        switch (speed_grade)
        {
            case -3: SpeedMultiplier(1 / 3f, speed_change_duration); break;
            case -2: SpeedMultiplier(1 / 2f, speed_change_duration); break;
            case -1: SpeedMultiplier(1 / 1.5f, speed_change_duration); break;
            case 0: SpeedMultiplier(1, speed_change_duration); break;
            case 1: SpeedMultiplier(1.2f, speed_change_duration); break;
            case 2: SpeedMultiplier(1.5f, speed_change_duration); break;
            case 3: SpeedMultiplier(2, speed_change_duration); break;
        }
    }
    void PowerUpdater()
    {
        // pausing_power の時は速度変更を行わない
        if (pausing_power) return;

        switch (power_grade)
        {
            case -3: PowerMultiplier(1 / 2f); break;
            case -2: PowerMultiplier(1 / 1.5f); break;
            case -1: PowerMultiplier(1 / 1.2f); break;
            case 0: PowerMultiplier(1); break;
            case 1: PowerMultiplier(1.2f); break;
            case 2: PowerMultiplier(1.5f); break;
            case 3: PowerMultiplier(2); break;
        }
    }
    void DefenceUpdater()
    {
        // pausing_defence の時は速度変更を行わない
        if (pausing_defence) return;

        switch (defence_grade)
        {
            case -3: DefenceMultiplier(1 / 2f); break;
            case -2: DefenceMultiplier(1 / 1.5f); break;
            case -1: DefenceMultiplier(1 / 1.2f); break;
            case 0: DefenceMultiplier(1); break;
            case 1: DefenceMultiplier(1.2f); break;
            case 2: DefenceMultiplier(1.5f); break;
            case 3: DefenceMultiplier(2); break;
        }
    }
    void SpeedMultiplier(float magnif, float duration = 0)
    {
        float end_speed = defaultSpeed * magnif;
        DOTween.To(() => speed, (x) => speed = x, end_speed, duration);
    }
    void PowerMultiplier(float magnif) => power = defaultPower * magnif;
    void DefenceMultiplier(float magnif) => defence = defaultDefence * magnif;


    // Only the owner of this fighter needs to call Graders.
    public void SpeedGrader(int delta_grade, float new_duration)
    {
        // Cache previous grade.
        int pre_grade = speed_grade;

        // Update grade.
        speed_grade += delta_grade;
        speed_grade = Mathf.Clamp(speed_grade, -3, 3);

        // Update duration.
        if (speed_grade == 0)
        {
            speed_timer = 0;
            speed_duration = 0;
        }
        else
        {
            if (pre_grade == 0)
            {
                speed_timer = 0;
                speed_duration = new_duration;
            }
            else
            {
                bool sign_flipped = pre_grade * speed_grade < 0;
                if (sign_flipped)
                {
                    speed_timer = 0;
                    speed_duration = new_duration;
                }
                else
                {
                    speed_duration += new_duration;
                }
            }
        }

        // Update status.
        SpeedUpdater();
    }
    public void PowerGrader(int delta_grade, float new_duration)
    {
        // Cache previous grade.
        int pre_grade = power_grade;

        // Update grade.
        power_grade += delta_grade;
        power_grade = Mathf.Clamp(power_grade, -3, 3);

        // Update duration.
        if (power_grade == 0)
        {
            power_timer = 0;
            power_duration = 0;
        }
        else
        {
            if (pre_grade == 0)
            {
                power_timer = 0;
                power_duration = new_duration;
            }
            else
            {
                bool sign_flipped = pre_grade * power_grade < 0;
                if (sign_flipped)
                {
                    power_timer = 0;
                    power_duration = new_duration;
                }
                else
                {
                    power_duration += new_duration;
                }
            }
        }

        // Update status.
        PowerUpdater();
    }
    public void DefenceGrader(int delta_grade, float new_duration)
    {
        // Cache previous grade.
        int pre_grade = defence_grade;

        // Update grade.
        defence_grade += delta_grade;
        defence_grade = Mathf.Clamp(defence_grade, -3, 3);

        // Update duration.
        if (defence_grade == 0)
        {
            defence_timer = 0;
            defence_duration = 0;
        }
        else
        {
            if (pre_grade == 0)
            {
                defence_timer = 0;
                defence_duration = new_duration;
            }
            else
            {
                bool sign_flipped = pre_grade * defence_grade < 0;
                if (sign_flipped)
                {
                    defence_timer = 0;
                    defence_duration = new_duration;
                }
                else
                {
                    defence_duration += new_duration;
                }
            }
        }

        // Update status.
        DefenceUpdater();
    }

    // Only the owner of the fighter needs to pause/resume grading.
    public void PauseGradingSpeed(float speed_temp, float duration = 0)
    {
        if (!IsOwner) return;
        pausing_speed = true;
        DOTween.To(() => speed, (x) => speed = x, speed_temp, duration);
    }
    public void ResumeGradingSpeed()
    {
        if (!IsOwner) return;
        pausing_speed = false;
        SpeedUpdater();
    }
    public void PauseGradingPower(float power_temp)
    {
        if (!IsOwner) return;
        pausing_power = true;
        power = power_temp;
    }
    public void ResumeGradingPower()
    {
        if (!IsOwner) return;
        pausing_power = false;
        PowerUpdater();
    }
    public void PauseGradingDefence(float defence_temp)
    {
        if (!IsOwner) return;
        pausing_defence = true;
        defence = defence_temp;
    }
    public void ResumeGradingDefence()
    {
        if (!IsOwner) return;
        pausing_defence = false;
        DefenceUpdater();
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
    protected float dec_cp_per_sec = 5;
    protected float cp_maintain = 0f;   // 0.0 (maintain none) ~ 1.0 (maintain all)
    protected float default_combo_timer;
    protected float default_zone_timer = 5;

    protected void CPStart()
    {
        cp = 0;
        combo = 0;
        default_combo_timer = has_comboKeep ? 4f : 2.5f;
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

    public virtual void Combo(float inc_cp)
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
                PowerGrader(boost_grade, boost_duration);
            }
            if (has_comboBoostD)
            {
                DefenceGrader(boost_grade, boost_duration);
            }
            if (has_comboBoostS)
            {
                SpeedGrader(boost_grade, boost_duration);
            }
        }

        // Do not increase cp when Zone.
        if (isZone) return;

        // Increase cp.
        if (combo >= combo_thresh)
        {
            // 3:x1.1, 4:x1.2, ... , 12:x2.0, 13:x2.0
            float cp_magnif = 1 + 0.1f * (combo - combo_thresh + 1);
            cp_magnif = Mathf.Clamp(cp_magnif, 1.0f, 2.0f);
            inc_cp *= cp_magnif;
        }

        // Bonus of ability.
        if (has_deepAbsorb)
        {
            inc_cp *= 1.5f;
        }

        cp += inc_cp;
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
    public abstract float revivalTime { get; set; }
    public float reviveTimer { get; private set; }

    // Processes run at the time of death. (Should be called on every clients)
    protected virtual void OnDeath(int destroyerNo, string causeOfDeath)
    {
        isDead = true;

        movement.OnDeath();
        attack.OnDeath();
        receiver.OnDeath(destroyerNo, causeOfDeath);
        radarIcon.Visualize(false);

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
            HPResetter();
            SpeedResetter();
            PowerResetter();
            DefenceResetter();
            CPResetter();
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



    // Special Abilities //////////////////////////////////////////////////////////////////////////////////////////////
    public bool has_shadowStep { get; set; } = false;
    public bool has_boostStep { get; set; } = false;
    public bool has_skillBoost { get; set; } = false;
    public bool has_skillKeep { get; set; } = false;
    public bool has_technician_1 { get; set; } = false;
    public bool has_technician_2 { get; set; } = false;
    public bool has_quickRepair { get; set; } = false;
    public bool has_comboKeep { get; set; } = false;
    public bool has_deepAbsorb { get; set; } = false;
    public bool has_comboBoostA { get; set; } = false;
    public bool has_comboBoostD { get; set; } = false;
    public bool has_comboBoostS { get; set; } = false;
}
