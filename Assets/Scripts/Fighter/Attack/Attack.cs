using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public abstract class Attack : NetworkBehaviour
{
    // 各武器の生成時に値を追加していく
    // ゲーム中に動的に生成される武器も存在するので、Attackからまとめて取得することはしない
    protected virtual void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        UpdateTrans();
        PoolNormalBullets(2);
    }

    protected virtual void Start()
    {
        SetLayerInteggers();
    }
    
    protected virtual void FixedUpdate()
    {
        UpdateTrans();
        SetHomingTargetNos();
    }



    public FighterCondition fighterCondition {get; set;}
    public virtual void OnDeath() {}
    public virtual void OnRevival() {}



    Vector3 MyPos, BPos;
    Quaternion MyRot, BRot;
    void UpdateTrans()
    {
        MyPos = transform.position;
        MyRot = transform.rotation;
        BPos = originalNormalBullet.transform.position;
        BRot = originalNormalBullet.transform.rotation;
    }



    // ParticipantManagerのAwake()でセットされる ////////////////////////////////////////////////////////////////////////
    public Skill[] skills {get; set;} = new Skill[GameInfo.max_skill_count];



    // Lock On ///////////////////////////////////////////////////////////////////////////////////////////////////////
    public List<int> homingTargetNos;    // homingTargets = ボディ or シールド。 プロパティにするとおかしくなる(?)。
    public int homingCount { get { return homingTargetNos.Count; } }
    public abstract float homingAngle {get; set;}    // Abilityで変化
    public abstract float homingDist {get; set;}    // Abilityで変化
    LayerMask enemy_mask;    // enemy_layer = Fighter-Root

    void SetLayerInteggers()
    {
        // Set Fighter-Root's layer to enemy_mask.
        if(fighterCondition.fighterTeam.Value == Team.Red) enemy_mask = 1 << 18;
        else if(fighterCondition.fighterTeam.Value == Team.Blue) enemy_mask = 1 << 17;
        else Debug.LogError("Team not set at FighterCondition");
    }

    void SetHomingTargetNos()
    {
        // Detect Fighter-Root GameObject in order to detect target regardless of oponents shield activation.
        Collider[] colliders = Physics.OverlapSphere(BPos, homingDist, enemy_mask);

        // Detect targets, and set them to homingTargetNos.
        if(colliders.Length > 0)
        {
            LayerMask terrain = LayerMask.GetMask("Terrain");
            var possibleTargets = colliders.Select(t => t.gameObject);

            // Get fighter number of targets.
            homingTargetNos = possibleTargets.Where(p => 

                // Check if target is inside homing range.
                Vector3.Angle(transform.forward, p.transform.position - MyPos) < homingAngle && 

                // Check if there are no obstacles (terrain) between self and target.
                !Physics.Raycast(MyPos, p.transform.position - MyPos, Vector3.Magnitude(p.transform.position - MyPos), terrain))

                // Get fighter number of target from its name.
                .Select(r => int.Parse(r.name)).ToList();
        }
        else
        {
            // Clean up list.
            homingTargetNos.Clear();
        }
    }



    // Normal Blast /////////////////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] GameObject originalNormalBullet;
    List<GameObject> normalBullets;
    List<Weapon> normalWeapons;
    protected abstract float setInterval {get; set;}
    protected float blastTimer {get; set;}
    HomingType homingType = HomingType.PreHoming; 

    // Variables that may vary by Ability.
    protected abstract int rapidCount {get; set;}
    float power = 1, speed = 30, lifespan = 1;

    void PoolNormalBullets(int quantity)
    {
        normalBullets = new List<GameObject>();
        normalWeapons = new List<Weapon>();
        for(int k = 0; k < quantity; k++)
        {
            GameObject bullet = Instantiate(originalNormalBullet, BPos, BRot, transform);
            normalBullets.Add(bullet);
            Weapon weapon = bullet.GetComponent<Weapon>();
            normalWeapons.Add(weapon);
            weapon.WeaponSetter(gameObject, this, "NormalBlast");
            weapon.WeaponParameterSetter(power, speed, lifespan, homingType);
        }
    }

    int GetNormalBulletIndex()
    {
        foreach(GameObject normalBullet in normalBullets) {if(normalBullet.activeSelf == false) {return normalBullets.IndexOf(normalBullet);}}
        GameObject newBullet = Instantiate(originalNormalBullet, BPos, BRot, transform);   //全て使用中だったら新たに作成
        normalBullets.Add(newBullet);
        Weapon weapon = newBullet.GetComponent<Weapon>();
        normalWeapons.Add(weapon);
        weapon.WeaponSetter(gameObject, this, "NormalBlast");
        weapon.WeaponParameterSetter(power, speed, lifespan, homingType);
        return normalBullets.IndexOf(newBullet);
    }

    protected void NormalBlast(GameObject target)
    {
        Weapon bullet = normalWeapons[GetNormalBulletIndex()];
        bullet.Activate(target);
    }

    protected void NormalRapid(GameObject target, int bulletCount)
    {
        const float interval = 0.05f;
        IEnumerator normalRapid(GameObject target, int bulletCount)
        {
            NormalBlast(target);
            for (int k = 1; k < bulletCount; k++)
            {
                yield return new WaitForSeconds(interval);
                NormalBlast(target);
            }
        }
        StartCoroutine(normalRapid(target, bulletCount));
    }

    [ServerRpc]
    /// <Param name="targetNo">Send -1 if there is no target.</Param>
    protected void NormalRapidServerRpc(ulong senderId, int targetNo, int rapidCount)
    {
        NormalRapidClientRpc(senderId, targetNo, rapidCount);
    }

    [ClientRpc]
    protected void NormalRapidClientRpc(ulong senderId, int targetNo, int bulletCount)
    {
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        GameObject target = null;
        if(targetNo != -1) target = ParticipantManager.I.fighterInfos[targetNo].body;
        NormalRapid(target, bulletCount);
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
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
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
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        skills[skillNo].EndProccess();
    }
}