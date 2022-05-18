using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Netcode;
using NaughtyAttributes;

// Fighterの状態を保持するクラス
// このクラスのプロパティをもとに、Movement、Attack、Receiverを動かす
public abstract class FighterCondition : NetworkBehaviour
{
    protected virtual void Awake()
    {
        GameObject explosionDead_obj = transform.Find("ExplosionDead").gameObject;
        deadEffect = explosionDead_obj.GetComponent<ParticleSystem>();
        deadSound = explosionDead_obj.GetComponent<AudioSource>();

        // Only the owner of this fighter initializes variables,
        // because HP & power is linked among clones, and speed & defence is not reffered by clones.
        if(BattleInfo.isMulti && !IsOwner) return;

        HPResetter();
        SpeedResetter();
        PowerResetter();
        DefenceResetter();
    }

    void Update()
    {
        // Only the owner of this fighter calls Update().
        if(BattleInfo.isMulti && !IsOwner) return;

        DeathJudger();

        if(isDead) return;

        SpeedTimeUpdate();
        PowerTimeUpdate();
        DefenceTimeUpdate();
    }



    [Header("Components")]
    public GameObject body;
    public Movement movement;
    public Attack attack;
    public Receiver receiver;
    public BodyManager bodyManager;

    [ContextMenu("Get Components")]
    void GetComponentsRequired()
    {
        body = transform.Find("fighterbody").gameObject;
        movement = GetComponent<Movement>();
        attack = body.GetComponent<Attack>();
        receiver = body.GetComponent<Receiver>();
        bodyManager = body.GetComponent<BodyManager>();
    }



    // ParticipantManagerからセット ///////////////////////////////////////////////////////////////////////////////////
    public NetworkVariable<int> fighterNo = new NetworkVariable<int>();   // How many participant is this fighter (0 ~ 7)
    public Team team {get; set;}



    // HP ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    // HP link is neccesarry, because each clones must update its own HP Bar UI.
    public NetworkVariable<float> HP = new NetworkVariable<float>();
    public abstract float default_HP {get; set;}
    public bool isDead {get; private set;}

    // Only the owner of this fighter initializes HP, because HP is linked among all clones.
    void HPResetter() => HP.Value = default_HP;

    // Decreaser is called only from the owner.
    // Only the owner of this fighter should call this in Repair Device.
    public virtual void HPDecreaser(float deltaHP)    // HPを増加させたい場合は、deltaHP < 0 とする
    {
        HP.Value -= deltaHP;
        HP.Value = Mathf.Clamp(HP.Value, 0, default_HP);
    }



    // Speed, Power, Defence ////////////////////////////////////////////////////////////////////////////////////////
    // Only reffered by owner. (Movement)
    public float speed {get; private set;}
    // Reffered in every clients. (Weapon.Activate)
    public NetworkVariable<float> power {get; private set;} = new NetworkVariable<float>();
    // Only reffered by owner. (Receiver.Damage)
    public float defence {get; private set;}

    // Only reffered by owner.
    public int speed_grade {get; private set;}
    public int power_grade {get; private set;}
    public int defence_grade {get; private set;}

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

    [Header("Default Params")]
    public float defaultSpeed = 5f;
    public float defaultPower = 1f;
    public float defaultDefence = 1f;

    const float speed_change_duration = 0.35f;


    // Only the owner of this fighter needs to updates timers.
    void SpeedTimeUpdate()
    {
        if(speed_grade != 0)
        {
            speed_timer += Time.deltaTime;
            if(speed_timer > speed_duration)
            {
                SpeedGrader(-speed_grade, 0);
                speed_duration = 0;
            }
        }
    }
    void PowerTimeUpdate()
    {
        if(power_grade != 0)
        {
            power_timer += Time.deltaTime;
            if(power_timer > power_duration)
            {
                PowerGrader(-power_grade, 0);
                power_duration = 0;
            }
        }
    }
    void DefenceTimeUpdate()
    {
        if(defence_grade != 0)
        {
            defence_timer += Time.deltaTime;
            if(defence_timer > defence_duration)
            {
                DefenceGrader(-defence_grade, 0);
                defence_duration = 0;
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
        power.Value = defaultPower;
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
        if(pausing_speed) return;

        switch(speed_grade)
        {
            case -3 : SpeedMultiplier(1/3f, speed_change_duration); break;
            case -2 : SpeedMultiplier(1/2f, speed_change_duration); break;
            case -1 : SpeedMultiplier(1/1.5f, speed_change_duration); break;
            case  0 : SpeedMultiplier(1, speed_change_duration); break;
            case  1 : SpeedMultiplier(1.2f, speed_change_duration); break;
            case  2 : SpeedMultiplier(1.5f, speed_change_duration); break;
            case  3 : SpeedMultiplier(2, speed_change_duration); break;
        }
    }
    void PowerUpdater()
    {
        // pausing_power の時は速度変更を行わない
        if(pausing_power) return;

        switch(power_grade)
        {
            case -3 : PowerMultiplier(1/2f); break;
            case -2 : PowerMultiplier(1/1.5f); break;
            case -1 : PowerMultiplier(1/1.2f); break;
            case  0 : PowerMultiplier(1); break;
            case  1 : PowerMultiplier(1.2f); break;
            case  2 : PowerMultiplier(1.5f); break;
            case  3 : PowerMultiplier(2); break;
        }
    }
    void DefenceUpdater()
    {
        // pausing_defence の時は速度変更を行わない
        if(pausing_defence) return;

        switch(defence_grade)
        {
            case -3 : DefenceMultiplier(1/2f); break;
            case -2 : DefenceMultiplier(1/1.5f); break;
            case -1 : DefenceMultiplier(1/1.2f); break;
            case  0 : DefenceMultiplier(1); break;
            case  1 : DefenceMultiplier(1.2f); break;
            case  2 : DefenceMultiplier(1.5f); break;
            case  3 : DefenceMultiplier(2); break;
        }
    }
    void SpeedMultiplier(float magnif, float duration = 0)
    {
        float end_speed = defaultSpeed * magnif;
        DOTween.To(() => speed, (x) => speed = x, end_speed, duration);
    }
    void PowerMultiplier(float magnif) => power.Value = defaultPower * magnif;
    void DefenceMultiplier(float magnif) => defence = defaultDefence * magnif;


    // Owner of this fighter sets default values.
    public void DefaultSpeedSetter(float default_speed) => this.defaultSpeed = default_speed;
    public void DefaultPowerSetter(float default_power) => this.defaultPower = default_power;
    public void DefaultDefenceSetter(float default_defence) => this.defaultDefence = default_defence;


    // Only the owner of this fighter needs to call Graders.
    public void SpeedGrader(int delta_grade, float new_duration)
    {
        speed_grade += delta_grade;
        speed_grade = Mathf.Clamp(speed_grade, -3, 3);

        // 持続時間は長い方を採択
        if(new_duration > speed_duration) speed_duration = new_duration;
        speed_timer = 0;

        SpeedUpdater();
    }
    public void PowerGrader(int delta_grade, float new_duration)
    {
        power_grade += delta_grade;
        power_grade = Mathf.Clamp(power_grade, -3, 3);

        // 持続時間は長い方を採択
        if(new_duration > power_duration) power_duration = new_duration;
        power_timer = 0;

        PowerUpdater();
    }
    public void DefenceGrader(int delta_grade, float new_duration)
    {
        defence_grade += delta_grade;
        defence_grade = Mathf.Clamp(defence_grade, -3, 3);

        // 持続時間は長い方を採択
        if(new_duration > defence_duration) defence_duration = new_duration;
        defence_timer = 0;

        DefenceUpdater();
    }

    // Only the owner of the fighter needs to pause/resume grading.
    public void PauseGradingSpeed(float speed_temp, float duration = 0)
    {
        if(BattleInfo.isMulti && !IsOwner) return;
        pausing_speed = true;
        DOTween.To(() => speed, (x) => speed = x, speed_temp, duration);
    }
    public void ResumeGradingSpeed()
    {
        if(BattleInfo.isMulti && !IsOwner) return;
        pausing_speed = false;
        SpeedUpdater();
    }
    public void PauseGradingPower(float power_temp)
    {
        if(BattleInfo.isMulti && !IsOwner) return;
        pausing_power = true;
        power.Value = power_temp;
    }
    public void ResumeGradingPower()
    {
        if(BattleInfo.isMulti && !IsOwner) return;
        pausing_power = false;
        PowerUpdater();
    }
    public void PauseGradingDefence(float defence_temp)
    {
        if(BattleInfo.isMulti && !IsOwner) return;
        pausing_defence = true;
        defence = defence_temp;
    }
    public void ResumeGradingDefence()
    {
        if(BattleInfo.isMulti && !IsOwner) return;
        pausing_defence = false;
        DefenceUpdater();
    }



    // Death & Revival //////////////////////////////////////////////////////////////////////////////////////////////
    public abstract float revivalTime {get; set;}
    public float revive_timer {get; private set;}
    ParticleSystem deadEffect;
    AudioSource deadSound;
    
    // Should be called on every clients.
    protected virtual void Death()
    {
        deadEffect.Play();
        deadSound.Play();

        movement.OnDeath();
        attack.OnDeath();
        receiver.OnDeath();
        bodyManager.OnDeath();
    }

    // Should be called on every clients.
    protected virtual void Revival()
    {
        movement.OnRevival();
        attack.OnRevival();
        receiver.OnRevival();
        bodyManager.OnRevival();

        // Only the owner of this fighter needs to initializes variables.
        if(BattleInfo.isMulti && !IsOwner) return;

        HPResetter();
        SpeedResetter();
        PowerResetter();
        DefenceResetter();
    }

    [ServerRpc]
    void DeathServerRpc(ulong senderId)
    {
        DeathClientRpc(senderId);
    }

    [ClientRpc]
    void DeathClientRpc(ulong senderId)
    {
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        Death();
    }

    [ServerRpc]
    void RevivalServerRpc(ulong senderId)
    {
        RevivalClientRpc(senderId);
    }

    [ClientRpc]
    void RevivalClientRpc(ulong senderId)
    {
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        Revival();
    }

    // Only the owner should call this.
    void DeathJudger()
    {
        // HP is linked among all clients.
        if(HP.Value <= 0)
        {
            if(!isDead)
            {
                isDead = true;
                Death();
                if(BattleInfo.isMulti)
                {
                    if(IsHost) DeathClientRpc(OwnerClientId);
                    else DeathServerRpc(OwnerClientId);
                }
            }
            else
            {
                revive_timer += Time.deltaTime;
                if(revive_timer > revivalTime)
                {
                    revive_timer = 0;
                    isDead = false;
                    Revival();
                    if (BattleInfo.isMulti)
                    {
                        if (IsHost) RevivalClientRpc(OwnerClientId);
                        else RevivalServerRpc(OwnerClientId);
                    }
                }
            }
        }
    }
}
