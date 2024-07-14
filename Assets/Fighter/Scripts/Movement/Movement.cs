using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cysharp.Threading.Tasks;
using System;

public abstract class Movement : NetworkBehaviour
{
    public FighterCondition fighterCondition { get; protected set; }
    Transform bodyTrans;

    protected virtual void Awake()
    {
        fighterCondition = GetComponent<FighterCondition>();
        fighterCondition.OnDeathCallback += OnDeath;
        fighterCondition.OnRevivalCallback += OnRevival;
        bodyTrans = fighterCondition.transform.Find("fighterbody");
        col = GetComponent<Collider>();
        col.enabled = false;

        // Components for death animation.
        rigidBody = GetComponent<Rigidbody>();
        Transform explosion_trans = bodyTrans.Find("Explosion");
        Transform explosion_trans1 = explosion_trans.Find("ExplosionDead1");
        Transform explosion_trans2 = explosion_trans.Find("ExplosionDead2");
        explosion2Trans = explosion_trans2;
        explosionSound1 = explosion_trans1.GetComponent<AudioSource>();
        explosionSound2 = explosion_trans2.GetComponent<AudioSource>();
        explosion1 = explosion_trans1.GetComponent<ParticleSystem>();
        explosion2 = explosion_trans2.GetComponent<ParticleSystem>();
        explosionTrail = explosion_trans.Find("ExplosionTrail").GetComponent<ParticleSystem>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        fighterCondition.OnDeathCallback -= OnDeath;
        fighterCondition.OnRevivalCallback -= OnRevival;
    }

    protected virtual void FixedUpdate()
    {
        if (!IsOwner) return;
        if (fighterCondition.isDead) return;

        MoveForward();

        if (!controllable) return;

        Rotate();
        FourActionExe();
    }



    // Movement ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected const float MAX_TILT_X = 55;
    protected const float MAX_TILT_Z = 60;
    Collider col;

    protected virtual void MoveForward()
    {
        float speed = fighterCondition.speed.value;
        rigidBody.velocity = transform.forward * speed * uTurndirection; // Move by rigidbody (Slip-through does not occur)
    }

    protected abstract void Rotate();

    // Enables rotation & 4actions when true.
    protected bool controllable { get; private set; } = false;
    public virtual void Controllable(bool controllable)
    {
        this.controllable = controllable;
        col.enabled = controllable; // If controllable, also enable collider to detect collision to obstacles.
    }



    // 4-Actions ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected Animator anim;
    protected float uturnTime, somersaultTime, rollTime;
    public int uTurndirection { get; protected set; } = 1;  // Used to reverse the direction of the aircraft after U-turn. (1 or -1)
    protected bool ready4action = true;
    protected float rollDistance = 15;  // Lateral movement distance during rolling.

    protected void Uturn()
    {
        if (ready4action) StartCoroutine(uTurn());
        if (IsOwner) UturnServerRpc(OwnerClientId);
    }
    protected virtual IEnumerator uTurn() { return null; }

    protected void Somersault()
    {
        if (ready4action) StartCoroutine(somersault());
        if (IsOwner) SomersaultServerRpc(OwnerClientId);
    }
    protected virtual IEnumerator somersault() { return null; }

    protected void LeftRoll(float freeze_time)
    {
        if (ready4action) StartCoroutine(leftroll(freeze_time));
        if (IsOwner) LeftRollServerRpc(OwnerClientId, freeze_time);
    }
    protected virtual IEnumerator leftroll(float freeze_time) { return null; }

    protected void RightRoll(float freeze_time)
    {
        if (ready4action) StartCoroutine(rightroll(freeze_time));
        if (IsOwner) RightRollServerRpc(OwnerClientId, freeze_time);
    }
    protected virtual IEnumerator rightroll(float freeze_time) { return null; }

    protected virtual void FourActionExe() { }

    [ServerRpc]
    void SomersaultServerRpc(ulong senderId) => SomersaultClientRpc(senderId);
    [ServerRpc]
    void UturnServerRpc(ulong senderId) => UturnClientRpc(senderId);
    [ServerRpc]
    void LeftRollServerRpc(ulong senderId, float delay) => LeftRoleClientRpc(senderId, delay);
    [ServerRpc]
    void RightRollServerRpc(ulong senderId, float delay) => RightRoleClientRpc(senderId, delay);
    [ClientRpc]
    void SomersaultClientRpc(ulong senderId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        Somersault();
    }
    [ClientRpc]
    void UturnClientRpc(ulong senderId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        Uturn();
    }
    [ClientRpc]
    void LeftRoleClientRpc(ulong senderId, float delay)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        LeftRoll(delay);
    }
    [ClientRpc]
    void RightRoleClientRpc(ulong senderId, float delay)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        RightRoll(delay);
    }



    // Collision Detection ////////////////////////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] LayerMask obstacleMask;  // Layer which kills instantly when collided (terrain & structures)
    void OnCollisionEnter(Collision col)
    {
        if (!IsOwner) return;
        if (fighterCondition.isDead) return;

        int col_layer = 1 << col.gameObject.layer;

        // Kill this fighter instantly when collided to obstacle.
        if ((obstacleMask & col_layer) != 0)
        {
            fighterCondition.Death(-1, FighterCondition.SPECIFIC_DEATH_COLLISION);
        }
    }



    // Death & Revival ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    Rigidbody rigidBody;
    Transform explosion2Trans;
    AudioSource explosionSound1, explosionSound2;
    ParticleSystem explosion1, explosion2, explosionTrail;

    // Must be called at every clients.
    protected virtual void OnDeath(int killer_no, string cause_of_death)
    {
        ready4action = false;
        StartCoroutine(DeathAnimation());
    }

    protected virtual IEnumerator DeathAnimation()
    {
        // Start falling, and play first explision effect.
        rigidBody.useGravity = true;
        explosion1.Play();
        explosionSound1.Play();
        explosionTrail.Play();

        // Wait a few seconds until the explosion1 effect (=1.6sec) ends.
        yield return new WaitForSeconds(2.0f);

        // Stop falling, and play second explision effect.
        rigidBody.useGravity = false;
        rigidBody.velocity = Vector3.zero;
        // Put out explosion2 from fighterbody before deactivating fighterbody, otherwise the effect will not be visible.
        explosion2Trans.parent = transform;
        explosion2.Play();
        explosionSound2.Play();
        explosionTrail.Stop();
        bodyTrans.gameObject.SetActive(false);

        // Wait a few seconds until the explosion1 effect (=1.6sec) ends.
        yield return new WaitForSeconds(1.8f);

        // Put back explosion2 under fighterbody.
        explosion2Trans.parent = bodyTrans;
        explosion2Trans.localPosition = Vector3.zero;
    }

    // Must be called on every clients.
    protected virtual async void OnRevival()
    {
        bodyTrans.localPosition = Vector3.zero;
        bodyTrans.gameObject.SetActive(true);
        await UniTask.Delay(TimeSpan.FromSeconds(3));
        ready4action = true;
    }
}
