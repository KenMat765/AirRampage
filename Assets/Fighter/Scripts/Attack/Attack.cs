using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public abstract class Attack : NetworkBehaviour
{
    public FighterCondition fighterCondition { get; set; }
    public bool attackable { get; set; } = false;   // If able to attack or not.

    protected virtual void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        PoolNormalBullets(2);
    }



    // Death & Revival ///////////////////////////////////////////////////////////////
    public virtual void OnDeath() { }
    public virtual void OnRevival() { }



    // Lock On ///////////////////////////////////////////////////////////////////////////////////////////////////////
    public List<int> lockonTargetNos { get; private set; } = new List<int>(); // targets: body or shield
    public int lockonCount { get { return lockonTargetNos.Count; } }

    [Header("Lockon")]
    public float lockonAngle;       // change by Ability
    public float lockonDistance;    // change by Ability

    // Search for the fighter number of locked-on targets.
    protected void SetLockonTargetNos()
    {
        Transform my_transform = transform;
        Vector3 my_position = my_transform.position;
        Vector3 bullet_position = originalNormalBullet.transform.position;

        // Detect Fighter-Root to detect target regardless of opponents shield activation. (Collider of body is disabled when activating shield)
        Collider[] colliders = Physics.OverlapSphere(bullet_position, lockonDistance, fighterCondition.fighters_mask);

        // Detect targets, and set them to lockonTargetNos.
        if (colliders.Length > 0)
        {
            var possibleTargets = colliders.Select(t => t.gameObject);

            // Get fighter number of targets.
            lockonTargetNos = possibleTargets.Where(p =>

                // Check if target is inside lockon range.
                Vector3.Angle(my_transform.forward, p.transform.position - my_position) < lockonAngle &&

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
            lockonTargetNos.Clear();
        }
    }



    // Only For Terminal Conquest ///////////////////////////////////////////////////////////////////////////////////
    public List<int> attackableTerminals { get; private set; } = new List<int>();
    protected void SearchAttackableTerminals()
    {
        Vector3 my_position = transform.position;
        Vector3 bullet_position = originalNormalBullet.transform.position;

        Collider[] colliders = Physics.OverlapSphere(bullet_position, lockonDistance, fighterCondition.terminals_mask);

        // Detect targets, and set them to aroundTerminals.
        if (colliders.Length > 0)
        {
            var possibleTargets = colliders.Select(t => t.gameObject);

            // Get terminal number of targets.
            attackableTerminals = possibleTargets.Where(p =>

                // Check if target is inside lockon range.
                Vector3.Angle(transform.forward, p.transform.position - my_position) < lockonAngle &&

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
    [SerializeField] protected GameObject originalNormalBullet; // original prefab of the normal bullet
    [SerializeField] protected ParticleSystem blastImpact;      // bullet firing effect
    [SerializeField] protected AudioSource blastSound;          // bullet firing sound

    // Properties of normal bullet.
    public float bulletPower = 1;                               // power of normal bullet (≠ FighterCondition.power)
    public float bulletSpeed = 150;                             // speed of normal bullet (≠ FighterCondition.speed)
    public float bulletLifespan = 1;                            // lifespan of normal bullet
    public abstract float blastInterval { get; set; }           // interval until the next blast (changes by Ability)
    HomingType homingType = HomingType.PreHoming;               // homing type of normal bullet (PreHoming: face towards the enemy upon firing)

    protected List<Weapon> normalWeapons = new List<Weapon>();  // list of Weapon components attached to each bullets
    protected float blastTimer { get; set; }                    // timer used for firing at specific interval.

    // This if DEATH_NORMAL_BLAST for fighters, but change this to SPECIFIC_DEATH_CANNON for cannons.
    protected abstract string causeOfDeath { get; set; }

    // Instantiate a specified number of bullets.
    protected void PoolNormalBullets(int quantity)
    {
        Transform orig_trans = originalNormalBullet.transform;
        for (int k = 0; k < quantity; k++)
        {
            GameObject bullet = Instantiate(originalNormalBullet, orig_trans.position, orig_trans.rotation, transform);
            Weapon weapon = bullet.GetComponent<Weapon>();
            normalWeapons.Add(weapon);
            weapon.WeaponSetter(gameObject, this, false, causeOfDeath);
            weapon.WeaponParameterSetter(bulletPower, bulletSpeed, bulletLifespan, homingType);
        }
    }

    // Get the ID of bullet that are currently not in use.
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
        float interval = blastInterval / rapidCount;
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



    // Skills ////////////////////////////////////////////////////////////////////////////////////////////////////////
    public Skill[] skills { get; set; } = new Skill[GameInfo.MAX_SKILL_COUNT];  // Set in ParticipantManager.Awake

    // Stop charging and disable the use of skills.
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

    // Terminate all currently active skills.
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

    // Activation RPCs are declared here because Skill component cannnot call RPCs. (they are attached AFTER fighters are spawned)

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