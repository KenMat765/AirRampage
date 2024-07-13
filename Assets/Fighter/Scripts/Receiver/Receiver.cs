using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using DG.Tweening;

// FighterConditionの各変数と、外部との橋渡しをするクラス
public abstract class Receiver : NetworkBehaviour
{
    public FighterCondition fighterCondition { get; protected set; }

    [Tooltip("Stops accepting any attacks when false")]
    public bool acceptAttack;

    // Collider needs to be disabled, in order not to be detected by other fighter as homing target.
    Collider col;


    protected virtual void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        fighterCondition.OnDeathCallback += OnDeath;
        fighterCondition.OnRevivalCallback += OnRevival;
        col = GetComponent<Collider>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        fighterCondition.OnDeathCallback -= OnDeath;
        fighterCondition.OnRevivalCallback -= OnRevival;
    }



    // On weapon hit callback ////////////////////////////////////////////////////////////////////////////////////////////

    // Should be called only at Owner
    public virtual void OnWeaponHit(int fighterNo)
    {
        if (IsOwner) { }
        else
            OnWeaponHitServerRpc(fighterNo);
    }

    [ServerRpc(RequireOwnership = false)]
    void OnWeaponHitServerRpc(int fighterNo)
    {
        if (IsOwner)
            OnWeaponHit(fighterNo);
        else
            OnWeaponHitClientRpc(fighterNo);
    }

    [ClientRpc]
    void OnWeaponHitClientRpc(int fighterNo) { if (IsOwner) OnWeaponHit(fighterNo); }

    Sequence hitSeq;
    protected void ShakeBody()
    {
        if (hitSeq.IsActive())
        {
            return;
        }
        Vector3 rot_strength = new Vector3(0, 0, 60);
        int rot_vibrato = 10;
        hitSeq = DOTween.Sequence();
        hitSeq.Join(transform.DOShakeRotation(0.5f, rot_strength, rot_vibrato));
        hitSeq.Play();
    }



    // Damages & Debuffs ////////////////////////////////////////////////////////////////////////////////////////////
    public void HPDown(float power)
    {
        if (IsOwner)
        {
            if (!acceptAttack) return;
            float damage = power / fighterCondition.defence.value;
            fighterCondition.HPDecreaser(damage);
        }
        else
            HPDownServerRpc(power);
    }

    public void SpeedDown(int delta_grade, float duration, float probability)
    {
        if (IsOwner)
        {
            if (!acceptAttack) return;
            float random = Random.value;
            if (random <= probability) fighterCondition.speed.Grade(delta_grade, duration);
        }
        else
            SpeedDownServerRpc(delta_grade, duration, probability);
    }

    public void PowerDown(int delta_grade, float duration, float probability)
    {
        if (IsOwner)
        {
            if (!acceptAttack) return;
            float random = Random.value;
            if (random <= probability) fighterCondition.power.Grade(delta_grade, duration);
        }
        else
            PowerDownServerRpc(delta_grade, duration, probability);
    }

    public void DefenceDown(int delta_grade, float duration, float probability)
    {
        if (IsOwner)
        {
            if (!acceptAttack) return;
            float random = Random.value;
            if (random <= probability) fighterCondition.defence.Grade(delta_grade, duration);
        }
        else
            DefenceDownServerRpc(delta_grade, duration, probability);
    }

    [ServerRpc(RequireOwnership = false)]
    void HPDownServerRpc(float power)
    {
        if (IsOwner)
            HPDown(power);
        else
            HPDownClientRpc(power);
    }

    [ServerRpc(RequireOwnership = false)]
    void SpeedDownServerRpc(int grade, float duration, float probability)
    {
        if (IsOwner)
            SpeedDown(grade, duration, probability);
        else
            SpeedDownClientRpc(grade, duration, probability);
    }

    [ServerRpc(RequireOwnership = false)]
    void PowerDownServerRpc(int grade, float duration, float probability)
    {
        if (IsOwner)
            PowerDown(grade, duration, probability);
        else
            PowerDownClientRpc(grade, duration, probability);
    }

    [ServerRpc(RequireOwnership = false)]
    void DefenceDownServerRpc(int grade, float duration, float probability)
    {
        if (IsOwner)
            DefenceDown(grade, duration, probability);
        else
            DefenceDownClientRpc(grade, duration, probability);
    }

    [ClientRpc]
    void HPDownClientRpc(float power) { if (IsOwner) HPDown(power); }

    [ClientRpc]
    void SpeedDownClientRpc(int grade, float duration, float probability) { if (IsOwner) SpeedDown(grade, duration, probability); }

    [ClientRpc]
    void PowerDownClientRpc(int grade, float duration, float probability) { if (IsOwner) PowerDown(grade, duration, probability); }

    [ClientRpc]
    void DefenceDownClientRpc(int grade, float duration, float probability) { if (IsOwner) DefenceDown(grade, duration, probability); }



    // Shooter Detection /////////////////////////////////////////////////////////////////////////////////////////////
    public int lastShooterNo { get; private set; }
    public string lastCauseOfDeath { get; private set; }

    // Should be called at Owner.
    public void LastShooterDetector(int fighterNo, string causeOfDeath)
    {
        if (IsOwner)
        {
            lastShooterNo = fighterNo;
            lastCauseOfDeath = causeOfDeath;
        }
        else
            LastShooterDetectorServerRpc(fighterNo, causeOfDeath);
    }

    [ServerRpc(RequireOwnership = false)]
    void LastShooterDetectorServerRpc(int fighterNo, string causeOfDeath)
    {
        if (IsOwner)
            LastShooterDetector(fighterNo, causeOfDeath);
        else
            LastShooterDetectorClientRpc(fighterNo, causeOfDeath);
    }

    [ClientRpc]
    void LastShooterDetectorClientRpc(int fighterNo, string causeOfDeath) { if (IsOwner) LastShooterDetector(fighterNo, causeOfDeath); }



    // Death & Revival /////////////////////////////////////////////////////////////////////////////////////////////
    protected virtual void OnDeath(int destroyerNo, string causeOfDeath)
    {
        col.enabled = false;
    }

    protected virtual void OnRevival()
    {
        col.enabled = true;
    }



    // Shield ///////////////////////////////////////////////////////////////////////////////////////////////////////

    // As ShieldHitDetector cannot call RPCs, declear here.
    public ShieldHitDetector hitDetector { get; set; }  // Set from Shield class.

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
