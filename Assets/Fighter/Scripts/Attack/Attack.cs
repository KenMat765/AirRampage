using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public abstract class Attack : NetworkBehaviour
{
    // If able to attack or not.
    public bool attackable { get; set; } = false;


    // 各武器の生成時に値を追加していく
    // ゲーム中に動的に生成される武器も存在するので、Attackからまとめて取得することはしない
    protected virtual void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        PoolNormalBullets(2);
    }


    public FighterCondition fighterCondition { get; set; }
    public virtual void OnDeath() { }
    public virtual void OnRevival() { }



    // Skills ////////////////////////////////////////////////////////////////////////
    // These are set in Awake() of ParticipantManager
    public Skill[] skills { get; set; } = new Skill[GameInfo.MAX_SKILL_COUNT];
    public void LockAllSkills(bool lock_skill)
    {
        foreach (Skill skill in skills)
        {
            if (skill != null)
            {
                if (skill.isUsing)
                {
                    skill.ForceTermination(true);
                }
                skill.isLocked = lock_skill;
            }
        }
    }
    public void TerminateAllSkills()
    {
        foreach (Skill skill in skills)
        {
            if (skill != null)
            {
                bool maintain_charge = fighterCondition.has_skillKeep;
                skill.ForceTermination(maintain_charge);
            }
        }
    }



    // Lock On ///////////////////////////////////////////////////////////////////////////////////////////////////////
    public List<int> homingTargetNos { get; private set; } = new List<int>();    // homingTargets = body or shield
    public int homingCount { get { return homingTargetNos.Count; } }

    [Header("Homing")]
    public float homingAngle;    // Abilityで変化
    public float homingDist;    // Abilityで変化

    protected void SetHomingTargetNos()
    {
        Vector3 my_position = transform.position;
        Vector3 bullet_position = originalNormalBullet.transform.position;

        // Detect Fighter-Root to detect target regardless of opponents shield activation. (Collider of body is disabled when activating shield)
        Collider[] colliders = Physics.OverlapSphere(bullet_position, homingDist, fighterCondition.fighters_mask);

        // Detect targets, and set them to homingTargetNos.
        if (colliders.Length > 0)
        {
            var possibleTargets = colliders.Select(t => t.gameObject);

            // Get fighter number of targets.
            homingTargetNos = possibleTargets.Where(p =>

                // Check if target is inside homing range.
                Vector3.Angle(transform.forward, p.transform.position - my_position) < homingAngle &&

                // Check if there are no obstacles (terrain + terminals) between self and target.
                !Physics.Raycast(my_position, p.transform.position - my_position, Vector3.Magnitude(p.transform.position - my_position), FighterCondition.obstacles_mask))

                // Get fighter number of target from its name.
                .Select(r => int.Parse(r.name))

                // Filter dead fighters.
                .Where(no => !ParticipantManager.I.fighterInfos[no].fighterCondition.isDead).ToList();
        }
        else
        {
            // Clean up list.
            homingTargetNos.Clear();
        }
    }



    // Only For Terminal Conquest ///////////////////////////////////////////////////////////////////////////////////
    public List<int> attackableTerminals { get; private set; } = new List<int>();

    protected void SearchAttackableTerminals()
    {
        Vector3 my_position = transform.position;
        Vector3 bullet_position = originalNormalBullet.transform.position;

        Collider[] colliders = Physics.OverlapSphere(bullet_position, homingDist, fighterCondition.terminals_mask);

        // Detect targets, and set them to aroundTerminals.
        if (colliders.Length > 0)
        {
            var possibleTargets = colliders.Select(t => t.gameObject);

            // Get terminal number of targets.
            attackableTerminals = possibleTargets.Where(p =>

                // Check if target is inside homing range.
                Vector3.Angle(transform.forward, p.transform.position - my_position) < homingAngle &&

                // Check if there are no obstacles (terrain) between self and target.
                !Physics.Raycast(my_position, p.transform.position - my_position, Vector3.Magnitude(p.transform.position - my_position), GameInfo.terrainMask))

                // Get terminal number of target from its name.
                .Select(r => int.Parse(r.name)).ToList();
        }
        else
        {
            // Clean up list.
            attackableTerminals.Clear();
        }
    }



    // Normal Blast /////////////////////////////////////////////////////////////////////////////////////////////////
    [Header("Normal Blast")]
    [SerializeField] protected GameObject originalNormalBullet;
    [SerializeField] protected ParticleSystem blastImpact;
    [SerializeField] protected AudioSource blastSound;
    protected List<Weapon> normalWeapons = new List<Weapon>();
    protected abstract int rapidCount { get; set; }
    protected float blastTimer { get; set; }
    HomingType homingType = HomingType.PreHoming;

    // Variables that may vary by Ability.
    public abstract float setInterval { get; set; }
    public float power = 1, speed = 150, lifespan = 1;

    // This if DEATH_NORMAL_BLAST for fighters, but change this to SPECIFIC_DEATH_CANNON for cannons.
    protected virtual string causeOfDeath { get; set; } = FighterCondition.DEATH_NORMAL_BLAST;

    protected void PoolNormalBullets(int quantity)
    {
        Transform orig_trans = originalNormalBullet.transform;
        for (int k = 0; k < quantity; k++)
        {
            GameObject bullet = Instantiate(originalNormalBullet, orig_trans.position, orig_trans.rotation, transform);
            Weapon weapon = bullet.GetComponent<Weapon>();
            normalWeapons.Add(weapon);
            weapon.WeaponSetter(gameObject, this, false, causeOfDeath);
            weapon.WeaponParameterSetter(power, speed, lifespan, homingType);
        }
    }

    protected int GetNormalBulletIndex()
    {
        // Get ready weapon.
        foreach (Weapon normalWeapon in normalWeapons)
        {
            if (normalWeapon.weapon_ready)
            {
                return normalWeapons.IndexOf(normalWeapon);
            }
        }

        // Create new weapon if all weapons were not ready.
        PoolNormalBullets(1);
        return normalWeapons.Count - 1;
    }

    ///<param name="target"> Put null when there are no targets. </param>
    protected virtual void NormalBlast(GameObject target = null)
    {
        if (fighterCondition.isDead) return;
        if (!attackable) return;

        Weapon bullet = normalWeapons[GetNormalBulletIndex()];
        blastImpact.Play();
        blastSound.Play();
        bullet.Activate(target);
    }

    ///<param name="target"> Put null when there are no targets. </param>
    protected void NormalRapid(int rapidCount, GameObject target = null)
    {
        // const float interval = 0.05f;
        float interval = setInterval / this.rapidCount;
        IEnumerator normalRapid()
        {
            NormalBlast(target);
            for (int k = 1; k < rapidCount; k++)
            {
                yield return new WaitForSeconds(interval);
                NormalBlast(target);
            }
        }
        StartCoroutine(normalRapid());
    }

    [ServerRpc]
    /// <Param name="targetNo">Send -1 if there are no targets.</Param>
    protected void NormalRapidServerRpc(ulong senderId, int rapidCount, int targetNo = -1)
    {
        NormalRapidClientRpc(senderId, rapidCount, targetNo);
    }

    [ClientRpc]
    /// <Param name="targetNo">Send -1 if there are no targets.</Param>
    protected void NormalRapidClientRpc(ulong senderId, int rapidCount, int targetNo = -1)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        GameObject target = null;
        if (targetNo != -1) target = ParticipantManager.I.fighterInfos[targetNo].body;
        NormalRapid(rapidCount, target);
    }

    // Declared here because Skill cannnot call RPCs. (they are attached AFTER fighters are spawned)
    [ServerRpc]
    /// <Param name="targetNos">Used for attack & disturb skills to send target fighter numbers.</Param>
    public void SkillActivatorServerRpc(ulong senderId, int skillNo, int[] targetNos = null)
    {
        SkillActivatorClientRpc(senderId, skillNo, targetNos);
    }

    [ClientRpc]
    public void SkillActivatorClientRpc(ulong senderId, int skillNo, int[] targetNos = null)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        skills[skillNo].Activator(targetNos);
    }

    [ServerRpc]
    public void SkillEndProccessServerRpc(ulong senderId, int skillNo)
    {
        SkillEndProccessClientRpc(senderId, skillNo);
    }

    [ClientRpc]
    public void SkillEndProccessClientRpc(ulong senderId, int skillNo)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        skills[skillNo].EndProccess();
    }
}