using System.Collections;
using System.Collections.Generic;
using System.Linq;
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


    void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        col = GetComponent<Collider>();
    }



    // Death & Revival /////////////////////////////////////////////////////////////////////////////////////////////
    public virtual void OnDeath(int destroyerNo, string causeOfDeath)
    {
        col.enabled = false;
    }

    public virtual void OnRevival()
    {
        col.enabled = true;
    }



    // Shooter Detection /////////////////////////////////////////////////////////////////////////////////////////////
    public int lastShooterNo { get; private set; }
    public string lastSkillName { get; private set; }

    // Call : Server + Owner.
    public void LastShooterDetector(int fighterNo, string causeOfDeath)
    {
        lastShooterNo = fighterNo;
        lastSkillName = causeOfDeath;
    }

    [ServerRpc(RequireOwnership = false)]
    public void LastShooterDetectorServerRpc(int fighterNo, string causeOfDeath)
    {
        // Call in server too.
        LastShooterDetector(fighterNo, causeOfDeath);
        if (!IsOwner) LastShooterDetectorClientRpc(fighterNo, causeOfDeath);
    }

    [ClientRpc]
    public void LastShooterDetectorClientRpc(int fighterNo, string causeOfDeath)
    {
        // Call in owner.
        if (IsOwner) LastShooterDetector(fighterNo, causeOfDeath);
    }



    // Damages & Debuffs ////////////////////////////////////////////////////////////////////////////////////////////
    Sequence hitSeq;

    // Other things to do when weapon hit. (Called only at Owner)
    public virtual void OnWeaponHit(int fighterNo)
    {
        if (!IsOwner) return;

        // Shake fighter body.
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
