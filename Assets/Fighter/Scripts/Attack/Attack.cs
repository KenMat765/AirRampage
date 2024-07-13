using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public abstract class Attack : NetworkBehaviour
{
    public FighterCondition fighterCondition { get; protected set; }

    [Tooltip("Disable attack when false")]
    public bool attackable;

    protected virtual void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        fighterCondition.OnDeathCallback += OnDeath;
        fighterCondition.OnRevivalCallback += OnRevival;
        PoolNormalBullets(2);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        fighterCondition.OnDeathCallback -= OnDeath;
        fighterCondition.OnRevivalCallback -= OnRevival;
    }



    // Death & Revival ///////////////////////////////////////////////////////////////
    protected virtual void OnDeath(int destroyerNo, string causeOfDeath) { }
    protected virtual void OnRevival() { }



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
            // Clean up list when there are no enemy fighters around.
            lockonTargetNos.Clear();
        }
    }



    // Normal Blast /////////////////////////////////////////////////////////////////////////////////////////////////
    [Header("Normal Blast")]
    [SerializeField] protected GameObject originalNormalBullet;
    [SerializeField] protected ParticleSystem blastImpact;
    [SerializeField] protected AudioSource blastSound;

    // Properties of normal bullet.
    public float bulletPower = 1;   // power of normal bullet (≠ FighterCondition.power: power of fighter itself)
    public float bulletSpeed = 150;
    public float bulletLifespan = 1;
    public float blastInterval;
    HomingType homingType = HomingType.PreHoming;

    protected List<Weapon> normalWeapons = new List<Weapon>();
    protected float blastTimer { get; set; }

    // This if DEATH_NORMAL_BLAST for fighters, but change this to SPECIFIC_DEATH_CANNON for cannons.
    protected virtual string causeOfDeath { get; set; } = FighterCondition.DEATH_NORMAL_BLAST;

    // Instantiate a specified number of bullets.
    protected void PoolNormalBullets(int quantity)
    {
        Transform orig_trans = originalNormalBullet.transform;
        for (int k = 0; k < quantity; k++)
        {
            GameObject bullet = Instantiate(originalNormalBullet, orig_trans.position, orig_trans.rotation, transform);
            Weapon weapon = bullet.GetComponent<Weapon>();
            normalWeapons.Add(weapon);
            weapon.WeaponSetter(gameObject, fighterCondition, false, causeOfDeath);
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
    protected void NormalRapid(int rapid_count, GameObject target = null)
    {
        float interval = blastInterval / rapid_count;
        IEnumerator normalRapid()
        {
            NormalBlast(target);
            for (int k = 1; k < rapid_count; k++)
            {
                yield return new WaitForSeconds(interval);
                NormalBlast(target);
            }
        }
        StartCoroutine(normalRapid());

        if (IsOwner)
        {
            if (IsHost)
                NormalRapidClientRpc(rapid_count, -1);
            else
                NormalRapidServerRpc(rapid_count, -1);
        }
    }

    [ServerRpc]
    void NormalRapidServerRpc(int rapidCount, int targetNo = -1)
    {
        NormalRapidClientRpc(rapidCount, targetNo);
    }

    [ClientRpc]
    void NormalRapidClientRpc(int rapidCount, int targetNo = -1)
    {
        if (IsOwner) return;
        GameObject target = null;
        if (targetNo != -1) target = ParticipantManager.I.fighterInfos[targetNo].body;
        NormalRapid(rapidCount, target);
    }
}