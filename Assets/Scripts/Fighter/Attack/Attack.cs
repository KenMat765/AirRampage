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

    // NetworkVariables necessary for linking normal blast between clones.
    protected NetworkVariable<bool> isBlasting = new NetworkVariable<bool>(false);

    // Only used for normal bullets, as attack skills send fighter number through RPC everytime the owner activates it.
    // Set -1 if no targets were detected.
    protected NetworkVariable<int> targetNo = new NetworkVariable<int>(-1);

    [ServerRpc]
    protected void SetIsBlastingServerRpc(bool value) => isBlasting.Value = value;

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

        // If multiplayer, as homingTargets is referenced in skills, every clones need to set homingTargets
        // Though, only the server needs to convert homingTargets[0] to targetNo.
        if(BattleInfo.isMulti && IsHost)
        {
            // If there are homing targets, get the first targets fighter number from its name, and set it to targetNo.
            if (homingTargets.Count > 0) targetNo.Value = int.Parse(homingTargets[0].name);
            // If there are no homing targets, set targetNo to -1, in order to declare that there are no targets.
            else targetNo.Value = -1;
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
            // If multiplayer, convert targetNo to fighter-body-object, and set it to target.
            if(BattleInfo.isMulti)
            {
                if(targetNo.Value != -1) target = ParticipantManager.I.fighterInfos[targetNo.Value].body;
            }
            // If soloplayer, set homingTargets[0] directly to target.
            else
            {
                if(homingTargets.Count > 0) target = homingTargets[0];
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