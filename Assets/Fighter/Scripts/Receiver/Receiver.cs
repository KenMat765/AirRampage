using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using DG.Tweening;

// FighterConditionの各変数と、外部との橋渡しをするクラス
public class Receiver : NetworkBehaviour
{
    public FighterCondition fighterCondition { get; set; }
    public bool acceptDamage { get; set; }

    // Collider needs to be disabled, in order not to be detected by other fighter as homing target.
    Collider col;

    // Death by normal blast.
    public const string DEATH_NORMAL_BLAST = "NormalBlast";

    // Specific cause of death (Death other than enemy attacks)
    public const string SPECIFIC_DEATH_CRYSTAL = "Crystal Kill";
    public const string SPECIFIC_DEATH_COLLISION = "Collision Crash";
    public static string[] specificDeath = { SPECIFIC_DEATH_CRYSTAL, SPECIFIC_DEATH_COLLISION };


    void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        col = GetComponent<Collider>();
    }



    // Death & Revival /////////////////////////////////////////////////////////////////////////////////////////////
    public virtual void OnDeath(int destroyerNo, string causeOfDeath)
    {
        col.enabled = false;

        // If specific cause of death. (Not killed by enemy)
        if (specificDeath.Contains(causeOfDeath))
        {
            return;
        }

        // Give combo to destroyer.
        FighterCondition destroyer_condition = ParticipantManager.I.fighterInfos[destroyerNo].fighterCondition;
        destroyer_condition.Combo(fighterCondition.my_cp);
    }

    public virtual void OnRevival()
    {
        col.enabled = true;
    }

    // Tell uGUIManager to report death of this fighter. (Called from Player and AI fighters)
    protected void ReportDeath(int destroyerNo, string causeOfDeath)
    {
        string my_name = fighterCondition.fighterName.Value.ToString();
        Team my_team = fighterCondition.fighterTeam.Value;

        // Specific cause of death.
        if (specificDeath.Contains(causeOfDeath))
        {
            uGUIMannager.I.BookRepo(causeOfDeath, my_name, my_team, causeOfDeath);
            return;
        }

        // Death by enemy attacks.
        string destroyer_name = ParticipantManager.I.fighterInfos[destroyerNo].fighterCondition.fighterName.Value.ToString();
        if (causeOfDeath == DEATH_NORMAL_BLAST)
        {
            uGUIMannager.I.BookRepo(destroyer_name, my_name, my_team, causeOfDeath);
        }
        else
        {
            uGUIMannager.I.BookRepo(destroyer_name, my_name, my_team, causeOfDeath);
        }
    }



    // Shooter Detection /////////////////////////////////////////////////////////////////////////////////////////////
    public int lastShooterNo { get; private set; }
    public string lastSkillName { get; private set; }

    // Call : Server + Owner.
    public void LastShooterDetector(int fighterNo, string skillName)
    {
        lastShooterNo = fighterNo;
        lastSkillName = skillName;
    }

    [ServerRpc(RequireOwnership = false)]
    public void LastShooterDetectorServerRpc(int fighterNo, string skillName)
    {
        // Call in server too.
        LastShooterDetector(fighterNo, skillName);
        if (!IsOwner) LastShooterDetectorClientRpc(fighterNo, skillName);
    }

    [ClientRpc]
    public void LastShooterDetectorClientRpc(int fighterNo, string skillName)
    {
        // Call in owner.
        if (IsOwner) LastShooterDetector(fighterNo, skillName);
    }



    // Damages & Debuffs ////////////////////////////////////////////////////////////////////////////////////////////
    Sequence hitSeq;

    // Other things to do when weapon hit. (Called only at Owner)
    public virtual void OnWeaponHit(int fighterNo)
    {
        if (!IsOwner) return;

        if (!hitSeq.IsActive())
        {
            Vector3 rot_strength = new Vector3(0, 0, 60);
            int rot_vibrato = 10;
            hitSeq = DOTween.Sequence();
            hitSeq.Join(transform.DOShakeRotation(0.5f, rot_strength, rot_vibrato));
            hitSeq.Play();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnWeaponHitServerRpc(int fighterNo)
    {
        if (IsOwner) OnWeaponHit(fighterNo);
        else OnWeaponHitClientRpc(fighterNo);
    }

    [ClientRpc]
    void OnWeaponHitClientRpc(int fighterNo)
    {
        if (IsOwner) OnWeaponHit(fighterNo);
    }


    // HPDown is always called from weapon.
    public void HPDown(float power)
    {
        if (!acceptDamage) return;
        float damage = power / fighterCondition.defence;
        fighterCondition.HPDecreaser(damage);
    }

    // Speed, Power, Defence is NOT always called from weapon.
    public void SpeedDown(int speedGrade, float speedDuration, float speedProbability)
    {
        float random = Random.value;
        if (random <= speedProbability) fighterCondition.SpeedGrader(speedGrade, speedDuration);
    }

    public void PowerDown(int powerGrade, float powerDuration, float powerProbability)
    {
        float random = Random.value;
        if (random <= powerProbability) fighterCondition.PowerGrader(powerGrade, powerDuration);
    }

    public void DefenceDown(int defenceGrade, float defenceDuration, float defenceProbability)
    {
        float random = Random.value;
        if (random <= defenceProbability) fighterCondition.DefenceGrader(defenceGrade, defenceDuration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HPDownServerRpc(float power)
    {
        if (IsOwner) HPDown(power);
        else HPDownClientRpc(power);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpeedDownServerRpc(int speedGrade, float speedDuration, float speedProbability)
    {
        if (IsOwner) SpeedDown(speedGrade, speedDuration, speedProbability);
        else SpeedDownClientRpc(speedGrade, speedDuration, speedProbability);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PowerDownServerRpc(int powerGrade, float powerDuration, float powerProbability)
    {
        if (IsOwner) PowerDown(powerGrade, powerDuration, powerProbability);
        else PowerDownClientRpc(powerGrade, powerDuration, powerProbability);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DefenceDownServerRpc(int defenceGrade, float defenceDuration, float defenceProbability)
    {
        if (IsOwner) DefenceDown(defenceGrade, defenceDuration, defenceProbability);
        else DefenceDownClientRpc(defenceGrade, defenceDuration, defenceProbability);
    }

    [ClientRpc]
    public void HPDownClientRpc(float power) { if (IsOwner) HPDown(power); }

    [ClientRpc]
    public void SpeedDownClientRpc(int speedGrade, float speedDuration, float speedProbability) { if (IsOwner) SpeedDown(speedGrade, speedDuration, speedProbability); }

    [ClientRpc]
    public void PowerDownClientRpc(int powerGrade, float powerDuration, float powerProbability) { if (IsOwner) PowerDown(powerGrade, powerDuration, powerProbability); }

    [ClientRpc]
    public void DefenceDownClientRpc(int defenceGrade, float defenceDuration, float defenceProbability) { if (IsOwner) DefenceDown(defenceGrade, defenceDuration, defenceProbability); }



    // Shield ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // Set hitDetector from Shield class.
    // As ShieldHitDetector cannot call RPCs, declear RPC here.
    public ShieldHitDetector hitDetector { get; set; }

    [ServerRpc(RequireOwnership = false)]
    public void ShieldDurabilityDecreaseServerRpc(float power)
    {
        if (IsOwner) hitDetector.DecreaseDurability(power);
        else ShieldDurabilityDecreaseClientRpc(power);
    }

    [ClientRpc]
    public void ShieldDurabilityDecreaseClientRpc(float power)
    {
        if (IsOwner) hitDetector.DecreaseDurability(power);
    }
}
