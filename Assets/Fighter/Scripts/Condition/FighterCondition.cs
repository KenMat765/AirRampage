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
    protected virtual void Awake()
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
                Death(killerNo, causeOfDeath);
                return;
            }
            speed.Timer();
            power.Timer();
            defence.Timer();
        }
    }



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



    // Fighter Identities //////////////////////////////////////////////////////////////////////////////////////
    public NetworkVariable<int> fighterNo { get; set; } = new NetworkVariable<int>(-1);
    public NetworkVariable<FixedString32Bytes> fighterName { get; set; } = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<Team> fighterTeam { get; set; } = new NetworkVariable<Team>();



    // Status //////////////////////////////////////////////////////////////////////////////////////////////////
    // Current status is only reffered by owner.
    [Header("Current Status")]
    public float Hp;
    public FighterStatus speed { get; private set; }
    public FighterStatus power { get; private set; }
    public FighterStatus defence { get; private set; }

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



    // Death & Revival //////////////////////////////////////////////////////////////////////////////////////////////
    [Header("Death and Revival")]
    public float revivalTime;
    public float reviveTimer { get; private set; }
    public bool isDead { get; private set; }

    // Action<myNo, killerNo, killedTeam, causeOfDeath>
    public Action<int, int, Team, string> OnDeathCallback { get; set; }
    public Action OnRevivalCallback { get; set; }

    // Causes of death.
    public const string DEATH_NORMAL_BLAST = "Normal Blast";
    public const string SPECIFIC_DEATH_CANNON = "Cannon Blast";         // Specific death (Death other than enemy attacks)
    public const string SPECIFIC_DEATH_CRYSTAL = "Crystal Kill";        // Specific death (Death other than enemy attacks)
    public const string SPECIFIC_DEATH_COLLISION = "Collision Crash";   // Specific death (Death other than enemy attacks)
    static string[] specificDeath = { SPECIFIC_DEATH_CANNON, SPECIFIC_DEATH_CRYSTAL, SPECIFIC_DEATH_COLLISION };
    public static bool IsSpecificDeath(string causeOfDeath) { return specificDeath.Contains(causeOfDeath); }

    // These should only be referenced via arguments of OnDeath, since the values are not determined until death.
    int killerNo = -1;
    string causeOfDeath = "";
    public void SetKiller(int killer_no, string cause_of_death)
    {
        killerNo = killer_no;
        causeOfDeath = cause_of_death;
    }

    // Processes run at the time of death. (Should be called on every clients)
    protected virtual void OnDeath(int killer_no, string cause_of_death)
    {
        if (isDead) return;

        isDead = true;
        OnDeathCallback?.Invoke(fighterNo.Value, killer_no, fighterTeam.Value, cause_of_death);

        if (!IsSpecificDeath(cause_of_death)) // Died from enemy attacks.
        {
            // Invoke killer's Attack.OnKill
            Attack killer_attack = ParticipantManager.I.fighterInfos[killer_no].attack;
            killer_attack.OnKill(fighterNo.Value);
        }
    }

    // Method to call OnDeath() at all clients.
    /// <param name="killer_no">Put minus if killer is not fighter.</param>
    public void Death(int killer_no, string cause_of_death)
    {
        // Call for yourself.
        OnDeath(killer_no, cause_of_death);

        // Call for clones at other clients.
        if (IsHost)
            DeathClientRpc(OwnerClientId, killer_no, cause_of_death);
        else
            DeathServerRpc(OwnerClientId, killer_no, cause_of_death);
    }

    [ServerRpc]
    void DeathServerRpc(ulong senderId, int killer_no, string cause_of_death)
    {
        DeathClientRpc(senderId, killer_no, cause_of_death);
    }

    [ClientRpc]
    void DeathClientRpc(ulong senderId, int killer_no, string cause_of_death)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        OnDeath(killer_no, cause_of_death);
    }

    // Processes run at the time of revival. (Should be called on every clients)
    protected virtual void OnRevival()
    {
        isDead = false;
        OnRevivalCallback?.Invoke();
        if (IsOwner)
        {
            Hp = defaultHp;
            speed.Reset();
            power.Reset();
            defence.Reset();
            killerNo = -1;
            causeOfDeath = "";
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
}
