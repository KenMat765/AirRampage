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
    public List<int> homingTargetNos;
    public abstract float homingAngle {get; set;}    // Abilityで変化
    public abstract float homingDist {get; set;}    // Abilityで変化
    LayerMask enemy_mask;    // enemy_layer = ボディ + シールド

    // NetworkVariables necessary for linking normal blast between clones.
    protected NetworkVariable<bool> isBlasting = new NetworkVariable<bool>(false);

    // Only used for normal bullets, as attack skills send fighter number through RPC everytime the owner activates it.
    // Set -1 if no targets were detected.
    protected NetworkVariable<int> targetNoForNormalBlast = new NetworkVariable<int>(-1);

    [ServerRpc]
    protected void SetIsBlastingServerRpc(bool value) => isBlasting.Value = value;

    void SetLayerInteggers()
    {
        // Set Fighter-Root's layer to enemy_mask.
        if(fighterCondition.team == Team.Red) enemy_mask = 1 << 18;
        else if(fighterCondition.team == Team.Blue) enemy_mask = 1 << 17;
        else Debug.LogError("Team not set at FighterCondition");
    }

    void SetHomingTargets()
    {
        // Detect Fighter-Root GameObject in order to detect target regardless of oponents shield activation.
        Collider[] colliders = Physics.OverlapSphere(BPos, homingDist, enemy_mask);

        // Detect targets, and set them to homingTargetNos.
        if(colliders.Length > 0)
        {
            LayerMask terrain = LayerMask.GetMask("Default");
            var possibleTargets = colliders.Select(t => t.gameObject);
            // Get fighter number of targets.
            homingTargetNos = possibleTargets.Where(p => 
                // Check if target is inside homing range.
                Vector3.Angle(transform.forward, p.transform.position - MyPos) < homingAngle && 
                // Check if there are no obstacles (terrain) between self and target.
                !Physics.Raycast(MyPos, p.transform.position - MyPos, Vector3.Magnitude(p.transform.position - MyPos), terrain))
                // Get fighter number of target from its name.
                .Select(r => int.Parse(r.name))
                .ToList();

            // If multiplayer, set the first targets fighter number to targetNo, in order to link homing target in normal blast.
            // Only the server should set targetNo.
            if (BattleInfo.isMulti && IsHost)
            {
                // If there are homing targets, get the first targets fighter number.
                if (homingTargetNos.Count > 0) targetNoForNormalBlast.Value = homingTargetNos[0];
                // If there are no homing targets, set targetNo to -1, in order to declare that there are no targets.
                else targetNoForNormalBlast.Value = -1;
            }
        }
        else
        {
            // Clean up list.
            homingTargetNos.Clear();
        }

        // Convert numbers to fighter-body if there are any homing-targets.
        // As homingTargets is referenced in skills, every clones need to set homingTargets
        if(homingTargetNos.Count > 0)
        {
            homingTargets = homingTargetNos.Select(n => ParticipantManager.I.fighterInfos[n].body).ToList();
        }
        else
        {
            // Clean up list.
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
            // If multiplayer, convert targetNo to fighter-body-object, and set it to target.
            if(BattleInfo.isMulti)
            {
                if(targetNoForNormalBlast.Value != -1) target = ParticipantManager.I.fighterInfos[targetNoForNormalBlast.Value].body;
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