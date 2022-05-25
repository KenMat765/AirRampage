using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// FighterConditionの各変数と、外部との橋渡しをするクラス
public class Receiver : NetworkBehaviour
{
    void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        GameObject explosion_obj = transform.Find("Explosion").gameObject;
        explosionEffect = explosion_obj.GetComponent<ParticleSystem>();
        explosionSound = explosion_obj.GetComponent<AudioSource>();
    }



    public FighterCondition fighterCondition {get; set;}
    public virtual void OnDeath(int destroyerNo, string destroyerSkillName) {}
    public virtual void OnRevival() {}



    // Damage ///////////////////////////////////////////////////////////////////////////////////////////////////////
    ParticleSystem explosionEffect;
    AudioSource explosionSound;
    public void ExplosionEffectPlayer()
    {
        explosionEffect.Play();
        explosionSound.Play();
    }


    public int lastShooterNo {get; private set;}
    public string lastSkillName {get; private set;}

    public virtual void LastShooterDetector(int fighterNo, string skillName)
    {
        lastShooterNo = fighterNo;
        lastSkillName = skillName;
    }

    [ServerRpc(RequireOwnership = false)]
    public void LastShooterDetectorServerRpc(int fighterNo, string skillName)
    {
        if(IsOwner) LastShooterDetector(fighterNo, skillName);
        else LastShooterDetectorClientRpc(fighterNo, skillName);
    }

    [ClientRpc]
    void LastShooterDetectorClientRpc(int fighterNo, string skillName)
    {
        if(IsOwner) LastShooterDetector(fighterNo, skillName);
    }


    // HPDown is always called from weapon.
    public void HPDown(float power)
    {
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
        if(IsOwner) HPDown(power);
        else HPDownClientRpc(power);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpeedDownServerRpc(int speedGrade, float speedDuration, float speedProbability)
    {
        if(IsOwner) SpeedDown(speedGrade, speedDuration, speedProbability);
        else SpeedDownClientRpc(speedGrade, speedDuration, speedProbability);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PowerDownServerRpc(int powerGrade, float powerDuration, float powerProbability)
    {
        if(IsOwner) PowerDown(powerGrade, powerDuration, powerProbability);
        else PowerDownClientRpc(powerGrade, powerDuration, powerProbability);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DefenceDownServerRpc(int defenceGrade, float defenceDuration, float defenceProbability)
    {
        if(IsOwner) DefenceDown(defenceGrade, defenceDuration, defenceProbability);
        else DefenceDownClientRpc(defenceGrade, defenceDuration, defenceProbability);
    }

    [ClientRpc]
    public void HPDownClientRpc(float power) { if(IsOwner) HPDown(power); }

    [ClientRpc]
    public void SpeedDownClientRpc(int speedGrade, float speedDuration, float speedProbability) { if(IsOwner) SpeedDown(speedGrade, speedDuration, speedProbability); }

    [ClientRpc]
    public void PowerDownClientRpc(int powerGrade, float powerDuration, float powerProbability) { if(IsOwner) PowerDown(powerGrade, powerDuration, powerProbability); }

    [ClientRpc]
    public void DefenceDownClientRpc(int defenceGrade, float defenceDuration, float defenceProbability) { if(IsOwner) DefenceDown(defenceGrade, defenceDuration, defenceProbability); }
}
