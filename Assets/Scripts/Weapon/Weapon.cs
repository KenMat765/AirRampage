using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public abstract class Weapon : Utilities
{
    public bool weapon_ready
    {
        get
        {
            return !gameObject.activeSelf && !hitEffect.activeSelf;
        }
    }


    // 武器によって固定の要素 ///////////////////////////////////////////////////////////////////////////////////////////
    [BoxGroup("Penetrate"), SerializeField, OnValueChanged("WhenPenetrateRange")]
    bool penetrate;
    void WhenPenetrateRange()
    {
        if (penetrate && IsRange())
        {
            attackRange = AttackRange.Single;
            Debug.LogWarning("貫通型の範囲攻撃は不可能なので、攻撃範囲はSingleに変更されました");
        }
    }


    enum AttackRange { Single, Range }

    [BoxGroup("AttackRange"), SerializeField, OnValueChanged("WhenPenetrateRange")]
    AttackRange attackRange;
    bool IsRange() { return attackRange == AttackRange.Range; }

    [BoxGroup("AttackRange"), SerializeField, ShowIf("IsRange")]
    float rangeRadius;


    [BoxGroup("BlastImpact"), SerializeField, InfoBox("ParticleSystem/StopAction = Disabled (Only Parent)"), InfoBox("AudioSource(Blast Sound)/PlayOnAwake = On")]
    GameObject blastImpact;


    [BoxGroup("HitEffect"), SerializeField, InfoBox("AudioSource/PlayOnAwake = OFF (toggled on in Awake)"), ValidateInput("HasHitEffectWhenRange", "範囲攻撃に Hit Effect は必須です")]
    GameObject hitEffect;
    List<GameObject> hitEffects;    // penetrate用

    [BoxGroup("HitEffect"), SerializeField, ShowIf("HasHitEffect")]
    float hitDuration;
    bool HasHitEffectWhenRange(GameObject obj)
    {
        if (IsRange() && !HasHitEffect()) return false;
        else return true;
    }
    bool HasHitEffect() { return hitEffect != null; }
    Vector3 hit_startPos;
    Quaternion hit_startRot;


    [BoxGroup("DefaultTarget"), SerializeField]
    bool hasDefaultTarget = false;

    [BoxGroup("DefaultTarget"), SerializeField, Range(0, 0.5f), ShowIf("hasDefaultTarget")]
    float defaultHomingAccuracy;
    Vector3 default_target;


    // スキルレベルによって変化する要素 /////////////////////////////////////////////////////////////////////////////////////
    protected float power { get; set; }
    protected float lifespan { get; set; }
    protected float speed { get; set; }

    protected HomingType homingType { get; set; }
    protected float homingAccuracy { get; set; }
    protected float homingAngle { get; set; }


    // Receiverに受け渡す要素 /////////////////////////////////////////////////////////////////////////////////////////////
    string causeOfDeath;
    protected float power_temp { get; private set; }

    protected bool speedDown { get; private set; }
    protected float speedProbability { get; private set; }
    protected int speedGrade { get; private set; }
    protected float speedDuration { get; private set; }

    protected bool powerDown { get; private set; }
    protected float powerProbability { get; private set; }
    protected int powerGrade { get; private set; }
    protected float powerDuration { get; private set; }

    protected bool defenceDown { get; private set; }
    protected float defenceProbability { get; private set; }
    protected int defenceGrade { get; private set; }
    protected float defenceDuration { get; private set; }


    // Skillからセット //////////////////////////////////////////////////////////////////////////////////////////////////
    GameObject owner;   // owner is figherbody, not Fighter
    bool isSkill;
    System.Func<float> StayMotion = null;
    GameObject targetObject = null;


    // Fighter properties //////////////////////////////////////////////////////////////////////////////////////////////////
    FighterCondition fighterCondition;
    Team team => fighterCondition.fighterTeam.Value;
    int fighterNo => fighterCondition.fighterNo.Value;


    int enemy_body_layer, enemy_shield_layer;
    LayerMask enemy_mask;    // enemy_mask = body + shield

    Transform parent;    // 帰る場所

    enum WeaponState { inactive, staying, moving, exploding }
    WeaponState current_state = WeaponState.inactive;

    float motion_duration = 0;
    float elapsedTime;

    Vector3 startPos;
    Quaternion startRot;

    List<GameObject> alreadyBombed;

    // Referenced from zako attack when changing team.
    public ParticleSystem parent_particle;


    // Awake() is called whether the gameObject is active or not.
    protected virtual void Awake()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;

        parent = transform.parent;

        if (team == Team.RED)
        {
            gameObject.layer = LayerMask.NameToLayer("RedBullet");
            enemy_body_layer = LayerMask.NameToLayer("BlueBody");
            enemy_shield_layer = LayerMask.NameToLayer("BlueShield");
            enemy_mask = (1 << 12) + (1 << 14);
        }
        else if (team == Team.BLUE)
        {
            gameObject.layer = LayerMask.NameToLayer("BlueBullet");
            enemy_body_layer = LayerMask.NameToLayer("RedBody");
            enemy_shield_layer = LayerMask.NameToLayer("RedShield");
            enemy_mask = (1 << 9) + (1 << 11);
        }
        else Debug.LogError("AttackにTeamが割り当てられていません。ParticipantManagerでAttackにTeamが割り当てられているか確認してください。");

        if (blastImpact != null)
        {
            blastImpact.transform.parent = owner.transform;
            blastImpact.transform.localPosition = startPos;
            blastImpact.transform.localRotation = startRot;
            blastImpact.SetActive(false);
        }

        if (hitEffect != null)
        {
            hitEffect.SetActive(false);
            hit_startPos = hitEffect.transform.localPosition;
            hit_startRot = hitEffect.transform.localRotation;
            // Set playOnAwake here, in order not to play hitSound as soon as the game starts.
            if (hitEffect.TryGetComponent<AudioSource>(out AudioSource hitSound))
            {
                hitSound.playOnAwake = true;
            }
            if (penetrate)
            {
                hitEffects = new List<GameObject>();
                hitEffects.Add(hitEffect);
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        switch (current_state)
        {
            case WeaponState.staying:
                OnStaying();
                break;

            case WeaponState.moving:
                OnMoving();
                break;

            case WeaponState.exploding:
                OnExploding();
                break;
        }
    }

    protected virtual void OnEnable()
    {
        if (StayMotion == null)
        {
            current_state = WeaponState.moving;
            OnStartMoving();
        }
        else
        {
            current_state = WeaponState.staying;
            OnStartStaying();
        }
    }

    protected virtual void OnCollisionEnter(Collision col)
    {
        if (attackRange == AttackRange.Single)
        {
            GameObject hit_obj = col.gameObject;

            // hit_obj == shield
            if (hit_obj.layer == enemy_shield_layer)
            {
                if (fighterCondition.IsOwner)
                {
                    Receiver receiver = hit_obj.GetComponentInParent<Receiver>();
                    receiver.ShieldDurabilityDecreaseServerRpc(power_temp);
                }
            }

            // hit_obj == fighter_body
            else if (hit_obj.layer == enemy_body_layer)
            {
                Receiver receiver = hit_obj.GetComponent<Receiver>();
                if (fighterCondition.IsOwner)
                {
                    receiver.HPDown(power_temp);
                    receiver.AttackerDetector(fighterNo, causeOfDeath);
                    receiver.OnWeaponHit(fighterNo);
                    if (speedDown) receiver.SpeedDown(speedGrade, speedDuration, speedProbability);
                    if (powerDown) receiver.PowerDown(powerGrade, powerDuration, powerProbability);
                    if (defenceDown) receiver.DefenceDown(defenceGrade, defenceDuration, defenceProbability);
                }
            }
        }

        // 当たったものに関係なく以下を実行
        if (hitEffect == null)
        {
            // 貫通型でないならKill
            if (!penetrate) KillWeapon();
        }
        else
        {
            if (penetrate)
            {
                // hitEffect を リジェクト ＋ 再生
                GameObject effect = GetHitEffectFromList();
                effect.transform.parent = null;
                effect.SetActive(true);
                DelayCall(this, hitDuration, () =>
                {
                    effect.SetActive(false);
                    effect.transform.parent = transform;
                    effect.transform.localPosition = hit_startPos;
                    effect.transform.localRotation = hit_startRot;
                });
            }

            // 貫通しないなら爆撃(貫通型かつ範囲攻撃はあり得ない)
            else
            {
                // hitEffect を リジェクト ＋ 再生
                hitEffect.transform.parent = null;
                hitEffect.SetActive(true);

                current_state = WeaponState.exploding;
                OnStartExploding();

                // 親弾のエフェクトを停止
                parent_particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }


    public void Activate(GameObject target)
    {
        float fighter_power = fighterCondition.power.value;
        power_temp = power * fighter_power;
        targetObject = target;
        gameObject.SetActive(true);
    }

    public void TerminateWeapon()
    {
        if (current_state == WeaponState.staying || (targetObject == owner && current_state != WeaponState.inactive))
        {
            KillWeapon();
        }
    }


    void OnStartStaying()
    {
        elapsedTime = 0;

        // StayMotion 開始
        motion_duration = StayMotion();
    }

    void OnStaying()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime > motion_duration)
        {
            current_state = WeaponState.moving;
            OnStartMoving();
            elapsedTime = 0;
        }
    }


    protected virtual void OnStartMoving()
    {
        elapsedTime = 0;

        // 準ホーミング
        if (homingType == HomingType.PreHoming)
        {
            if (targetObject != null) PreHoming();
        }

        // 弾をリジェクト ＋ 発射音、エフェクト再生
        transform.parent = null;
        if (blastImpact != null)
        {
            blastImpact.SetActive(true);
        }
        if (hasDefaultTarget) default_target = speed * lifespan * owner.transform.forward;
    }

    protected virtual void OnMoving()
    {
        // ホーミング
        switch (homingType)
        {
            case HomingType.Normal:
                if (hasDefaultTarget) HomeToDefault();
                break;

            case HomingType.PreHoming:
                if (targetObject == null && hasDefaultTarget) HomeToDefault();
                break;

            case HomingType.Homing:
                if (targetObject != null) Homing();
                else if (hasDefaultTarget) HomeToDefault();
                break;
        }

        elapsedTime += Time.deltaTime;
        if (elapsedTime > lifespan)
        {
            // Skill
            if (isSkill)
            {
                if (hitEffect == null)
                {
                    KillWeapon();
                }
                else
                {
                    current_state = WeaponState.exploding;
                    OnStartExploding();
                    PlayHitEffect();
                }
            }

            // Normal Bullet
            else
            {
                KillWeapon();
            }
        }
    }



    void OnStartExploding()
    {
        elapsedTime = 0;

        if (attackRange == AttackRange.Range && alreadyBombed == null)
        {
            alreadyBombed = new List<GameObject>();
        }
    }

    void OnExploding()
    {
        // 攻撃：Attack Range == Range
        if (attackRange == AttackRange.Range)
        {
            // hits == fighter_body or shield
            Collider[] hits = Physics.OverlapSphere(transform.position, rangeRadius, enemy_mask);

            foreach (Collider hit in hits)
            {
                GameObject hit_obj = hit.gameObject;

                // hit_obj == shield
                if (hit_obj.layer == enemy_shield_layer)
                {
                    if (!alreadyBombed.Contains(hit_obj))
                    {
                        alreadyBombed.Add(hit.gameObject);
                        alreadyBombed.Add(hit.transform.parent.gameObject);
                        if (fighterCondition.IsOwner)
                        {
                            Receiver receiver = hit_obj.GetComponentInParent<Receiver>();
                            receiver.ShieldDurabilityDecreaseServerRpc(power_temp);
                        }
                    }
                }

                // hit_obj == fighter_body
                else if (hit_obj.layer == enemy_body_layer)
                {
                    if (!alreadyBombed.Contains(hit_obj))
                    {
                        alreadyBombed.Add(hit.gameObject);
                        Receiver receiver = hit_obj.GetComponent<Receiver>();
                        if (fighterCondition.IsOwner)
                        {
                            receiver.HPDown(power_temp);
                            receiver.AttackerDetector(fighterNo, causeOfDeath);
                            receiver.OnWeaponHit(fighterNo);
                            if (speedDown) receiver.SpeedDown(speedGrade, speedDuration, speedProbability);
                            if (powerDown) receiver.PowerDown(powerGrade, powerDuration, powerProbability);
                            if (defenceDown) receiver.DefenceDown(defenceGrade, defenceDuration, defenceProbability);
                        }
                    }
                }
            }
        }

        elapsedTime += Time.deltaTime;
        if (elapsedTime > hitDuration)
        {
            hitEffect.SetActive(false);
            hitEffect.transform.parent = transform;
            hitEffect.transform.localPosition = hit_startPos;
            hitEffect.transform.localRotation = hit_startRot;

            if (attackRange == AttackRange.Range) alreadyBombed.Clear();

            KillWeapon();
        }
    }



    void PreHoming()
    {
        Vector3 relativePos = targetObject.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(relativePos);
    }

    void Homing()
    {
        Vector3 relativePos = targetObject.transform.position - transform.position;

        // Disable this for now ...
        // homingAngle圏内にいない場合、またはtargetが既に死んでいる(非アクティブ)場合は、targetを見失う
        // float currentAngle = Vector3.Angle(transform.forward, relativePos);
        // if(currentAngle > homingAngle || !targetObject.activeSelf)
        // {
        //     targetObject = null;
        //     return;
        // }

        if (relativePos != Vector3.zero)
        {
            Quaternion look_rotation = Quaternion.LookRotation(relativePos);
            transform.rotation = Quaternion.Slerp(transform.rotation, look_rotation, homingAccuracy);
        }
    }

    void HomeToDefault()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(default_target), defaultHomingAccuracy);
    }



    // hitEffectを持つ貫通型スキルで使用
    GameObject GetHitEffectFromList()
    {
        foreach (GameObject effect in hitEffects) if (!effect.activeSelf) return effect;
        GameObject new_effect = Instantiate(hitEffect, transform);
        hitEffects.Add(new_effect);
        return new_effect;
    }

    protected virtual void PlayHitEffect()
    {
        // 親弾のエフェクトを停止
        parent_particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // hit_effect を リジェクト ＋ 再生
        hitEffect.transform.parent = null;
        hitEffect.SetActive(true);
    }

    protected virtual void KillWeapon()
    {
        current_state = WeaponState.inactive;
        elapsedTime = 0;
        if (gameObject.activeSelf) gameObject.SetActive(false);
        transform.parent = parent;
        transform.localPosition = startPos;
        transform.localRotation = startRot;
    }


    // Weapon setting methods ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public void WeaponSetter
    (GameObject owner, FighterCondition fighterCondition, bool isSkill, string causeOfDeath, System.Func<float> StayMotion = null)
    {
        this.owner = owner;
        this.fighterCondition = fighterCondition;
        this.isSkill = isSkill;
        this.causeOfDeath = causeOfDeath;
        this.StayMotion = StayMotion;
    }

    public void WeaponParameterSetter(AttackLevelData levelData)
    {
        power = levelData.Power; speed = levelData.Speed; lifespan = levelData.Lifespan;
        homingType = levelData.HomingType; homingAccuracy = levelData.HomingAccuracy; homingAngle = levelData.HomingAngle;
        speedDown = levelData.SpeedDown; powerDown = levelData.PowerDown; defenceDown = levelData.DefenceDown;
        speedProbability = levelData.SpeedDownProbability; powerProbability = levelData.PowerDownProbability; defenceProbability = levelData.DefenceDownProbability;
        speedGrade = levelData.SpeedGrade; powerGrade = levelData.PowerGrade; defenceGrade = levelData.DefenceGrade;
        speedDuration = levelData.SpeedDuration; powerDuration = levelData.PowerDuration; defenceDuration = levelData.DefenceDuration;
    }
    public void WeaponParameterSetter(DisturbLevelData levelData)
    {
        power = 0; speed = levelData.Speed; lifespan = levelData.Lifespan;
        homingType = levelData.HomingType; homingAccuracy = levelData.HomingAccuracy; homingAngle = levelData.HomingAngle;
        speedDown = levelData.SpeedDown; powerDown = levelData.PowerDown; defenceDown = levelData.DefenceDown;
        speedProbability = levelData.SpeedDownProbability; powerProbability = levelData.PowerDownProbability; defenceProbability = levelData.DefenceDownProbability;
        speedGrade = levelData.SpeedGrade; powerGrade = levelData.PowerGrade; defenceGrade = levelData.DefenceGrade;
        speedDuration = levelData.SpeedDuration; powerDuration = levelData.PowerDuration; defenceDuration = levelData.DefenceDuration;
    }
    public void WeaponParameterSetter(float Power, float Speed, float Lifespan,
    HomingType HomingType = HomingType.Normal, float HomingAccuracy = 0, float HomingAngle = 0,
    bool SpeedDown = false, int SpeedGrade = 0, float SpeedDuration = 0, float SpeedProbability = 0,
    bool PowerDown = false, int PowerGrade = 0, float PowerDuration = 0, float PowerProbability = 0,
    bool DefenceDown = false, int DefenceGrade = 0, float DefenceDuration = 0, float DefenceProbability = 0)
    {
        power = Power; speed = Speed; lifespan = Lifespan;
        homingType = HomingType; homingAccuracy = HomingAccuracy; homingAngle = HomingAngle;
        speedDown = SpeedDown; powerDown = PowerDown; defenceDown = DefenceDown;
        speedProbability = SpeedProbability; powerProbability = PowerProbability; defenceProbability = DefenceProbability;
        speedGrade = SpeedGrade; powerGrade = PowerGrade; defenceGrade = DefenceGrade;
        speedDuration = SpeedDuration; powerDuration = PowerDuration; defenceDuration = DefenceDuration;
    }


    // For Debug ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(hitEffect.transform.position, rangeRadius);
    }
}