using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using DG.Tweening;

// FighterConditionの各変数と、外部との橋渡しをするクラス
public abstract class Receiver : NetworkBehaviour
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



    // On weapon hit callback ////////////////////////////////////////////////////////////////////////////////////////////

    // Should be called only at Owner
    public virtual void OnWeaponHit(int fighterNo) { }

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

    // HPDown is always called from weapon.
    public void HPDown(float power)
    {
        if (!acceptDamage) return;
        float damage = power / fighterCondition.defence;
        fighterCondition.HPDecreaser(damage);
    }

    // Speed, Power, Defence is NOT always called from weapon.
    public void SpeedDown(int grade, float duration, float probability)
    {
        float random = Random.value;
        if (random <= probability) fighterCondition.SpeedGrader(grade, duration);
    }

    public void PowerDown(int grade, float duration, float probability)
    {
        float random = Random.value;
        if (random <= probability) fighterCondition.PowerGrader(grade, duration);
    }

    public void DefenceDown(int grade, float duration, float probability)
    {
        float random = Random.value;
        if (random <= probability) fighterCondition.DefenceGrader(grade, duration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HPDownServerRpc(float power)
    {
        if (IsOwner) HPDown(power);
        else HPDownClientRpc(power);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpeedDownServerRpc(int grade, float duration, float probability)
    {
        if (IsOwner) SpeedDown(grade, duration, probability);
        else SpeedDownClientRpc(grade, duration, probability);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PowerDownServerRpc(int grade, float duration, float probability)
    {
        if (IsOwner) PowerDown(grade, duration, probability);
        else PowerDownClientRpc(grade, duration, probability);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DefenceDownServerRpc(int grade, float duration, float probability)
    {
        if (IsOwner) DefenceDown(grade, duration, probability);
        else DefenceDownClientRpc(grade, duration, probability);
    }

    [ClientRpc]
    public void HPDownClientRpc(float power) { if (IsOwner) HPDown(power); }

    [ClientRpc]
    public void SpeedDownClientRpc(int grade, float duration, float probability) { if (IsOwner) SpeedDown(grade, duration, probability); }

    [ClientRpc]
    public void PowerDownClientRpc(int grade, float duration, float probability) { if (IsOwner) PowerDown(grade, duration, probability); }

    [ClientRpc]
    public void DefenceDownClientRpc(int grade, float duration, float probability) { if (IsOwner) DefenceDown(grade, duration, probability); }



    // Shooter Detection /////////////////////////////////////////////////////////////////////////////////////////////
    public int lastShooterNo { get; private set; }
    public string lastCauseOfDeath { get; private set; }

    // Call : Server + Owner.
    public void LastShooterDetector(int fighterNo, string causeOfDeath)
    {
        lastShooterNo = fighterNo;
        lastCauseOfDeath = causeOfDeath;
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



    // Death & Revival /////////////////////////////////////////////////////////////////////////////////////////////
    public virtual void OnDeath(int destroyerNo, string causeOfDeath)
    {
        col.enabled = false;
    }

    public virtual void OnRevival()
    {
        col.enabled = true;
    }



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
