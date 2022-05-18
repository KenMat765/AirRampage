using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public abstract class Attack : NetworkBehaviour, IFighter
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
        SetHomingTargets();
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
    public List<GameObject> homingTargets;    // homingTargets = ボディ or シールド。 プロパティにするとおかしくなる(?)。
    public abstract float homingAngle {get; set;}    // Abilityで変化
    public abstract float homingDist {get; set;}    // Abilityで変化

    LayerMask enemy_mask;    // enemy_layer = ボディ + シールド

    void SetLayerInteggers()
    {
        if(fighterCondition.team == Team.Red) enemy_mask = (1 << 12) + (1 << 14);
        else if(fighterCondition.team == Team.Blue) enemy_mask = (1 << 9) + (1 << 11);
        else Debug.LogError("AttackにTeamが割り当てられていません。ParticipantManagerでAttackにTeamが割り当てられているか確認してください。");
    }

    void SetHomingTargets()
    {
        Collider[] colliders = Physics.OverlapSphere(BPos, homingDist, enemy_mask);
        if(colliders.Length > 0)
        {
            LayerMask terrain = LayerMask.GetMask("Default");
            var possibleTargets = colliders.Select(t => t.gameObject);
            homingTargets = possibleTargets.Where(p => 
                Vector3.Angle(transform.forward, p.transform.position - MyPos) < homingAngle && 
                !Physics.Raycast(MyPos, p.transform.position - MyPos, Vector3.Magnitude(p.transform.position - MyPos), terrain)).ToList();
        }
        else
        {
            homingTargets.Clear();
        }
    }



    // Normal Blast /////////////////////////////////////////////////////////////////////////////////////////////////
    protected abstract float normalInterval {get; set;}

    [SerializeField] GameObject originalNormalBullet;
    List<GameObject> normalBullets;
    List<Weapon> normalWeapons;
    float normalElapsedTime;
    float power = 1, speed = 30, lifespan = 1;
    HomingType homingType = HomingType.PreHoming; 

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

    protected void NormalBlast()
    {
        normalElapsedTime += Time.deltaTime;
        if(normalElapsedTime > normalInterval)
        {
            Weapon bullet = normalWeapons[GetNormalBulletIndex()];
            GameObject target = null;
            if(homingTargets.Count > 0)
            {
                target = homingTargets[0];
            }
            bullet.Activate(target);
            normalElapsedTime = 0;
        }
    }


    // Declared here because Skill cannnot call RPCs. (they are attached after fighters are spawned)
    [ServerRpc]
    public void SkillActivatorServerRpc(ulong senderId, int skillNo, string infoCode = null)
    {
        SkillActivatorClientRpc(senderId, skillNo, infoCode);
    }

    [ClientRpc]
    public void SkillActivatorClientRpc(ulong senderId, int skillNo, string infoCode = null)
    {
        if(NetworkManager.Singleton.LocalClientId == senderId) return;
        skills[skillNo].Activator(infoCode);
    }
}