using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// FighterConditionの各変数と、外部との橋渡しをするクラス
public class Receiver : NetworkBehaviour, IFighter
{
    void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        GameObject explosion_obj = transform.Find("Explosion").gameObject;
        explosionEffect = explosion_obj.GetComponent<ParticleSystem>();
        explosionSound = explosion_obj.GetComponent<AudioSource>();
    }



    public FighterCondition fighterCondition {get; set;}
    public virtual void OnDeath() {}
    public virtual void OnRevival() {}



    // Damage ///////////////////////////////////////////////////////////////////////////////////////////////////////
    ParticleSystem explosionEffect;
    AudioSource explosionSound;

    public virtual void Damage(Weapon weapon)
    {
        explosionEffect.Play();
        explosionSound.Play();

        if(BattleInfo.isMulti && !IsOwner) return;

        float damage = weapon.power_temp / fighterCondition.defence;
        fighterCondition.HPDecreaser(damage);

        if(weapon.speedDown)
        {
            float random = Random.value;
            if(random <= weapon.speedProbability) fighterCondition.SpeedGrader(weapon.speedGrade, weapon.speedDuration);
        }
        if(weapon.powerDown)
        {
            float random = Random.value;
            if(random <= weapon.powerProbability) fighterCondition.PowerGrader(weapon.powerGrade, weapon.powerDuration);
        }
        if(weapon.defenceDown)
        {
            float random = Random.value;
            if(random <= weapon.defenceProbability) fighterCondition.DefenceGrader(weapon.defenceGrade, weapon.defenceDuration);
        }
    }
}
