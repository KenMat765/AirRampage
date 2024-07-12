using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Cysharp.Threading.Tasks;
using System;

public abstract class Movement : NetworkBehaviour
{
    public FighterCondition fighterCondition { get; set; }

    protected virtual void Awake()
    {
        fighterCondition = GetComponent<FighterCondition>();
        latestDestinations = new Vector3[MAX_CACHE];
        col = GetComponent<Collider>();
        col.enabled = false;

        // Components for death animation.
        rigidBody = GetComponent<Rigidbody>();
        Transform explosion_trans = fighterCondition.body.transform.Find("Explosion");
        Transform explosion_trans1 = explosion_trans.Find("ExplosionDead1");
        Transform explosion_trans2 = explosion_trans.Find("ExplosionDead2");
        explosion2Trans = explosion_trans2;
        explosionSound1 = explosion_trans1.GetComponent<AudioSource>();
        explosionSound2 = explosion_trans2.GetComponent<AudioSource>();
        explosion1 = explosion_trans1.GetComponent<ParticleSystem>();
        explosion2 = explosion_trans2.GetComponent<ParticleSystem>();
        explosionTrail = explosion_trans.Find("ExplosionTrail").GetComponent<ParticleSystem>();
    }

    protected virtual void FixedUpdate()
    {
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
    public int uTurndirection { get; set; } = 1;  // Used to reverse the direction of the aircraft after U-turn. (1 or -1)
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
    public virtual void OnDeath()
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
        fighterCondition.body.SetActive(false);

        // Wait a few seconds until the explosion1 effect (=1.6sec) ends.
        yield return new WaitForSeconds(1.8f);

        // Put back explosion2 under fighterbody.
        explosion2Trans.parent = fighterCondition.body.transform;
        explosion2Trans.localPosition = Vector3.zero;
    }

    // Must be called on every clients.
    public virtual async void OnRevival()
    {
        GameObject body = fighterCondition.body;
        body.transform.localPosition = Vector3.zero;
        body.SetActive(true);
        await UniTask.Delay(TimeSpan.FromSeconds(3));
        ready4action = true;
    }



    // For AI fighters & zakos //////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected float rotationSpeed { get; set; }

    protected const float ARRIVE_DISTANCE = 30f;
    protected const float SPHERE_CAST_RADIUS = 5;

    public Vector3 finalDestination { get; private set; }
    public Vector3 nextDestination { get; private set; }
    protected bool arrived_at_final_destination;
    protected bool arrived_at_next_destination;
    protected bool bypassing;

    protected Vector3[] latestDestinations;
    const short MAX_CACHE = 10;
    int cache_idx = 0;


    protected void SetFinalDestination(Vector3 destination)
    {
        arrived_at_final_destination = false;
        finalDestination = destination;
        Array.Fill(latestDestinations, Vector3.zero);
        SetNextDestination();
    }

    protected void SetNextDestination()
    {
        arrived_at_next_destination = false;

        Vector3 my_position = transform.position;
        Vector3 relative_to_final = finalDestination - my_position;
        float distance_to_final = Vector3.Magnitude(relative_to_final);

        RaycastHit hit;
        Ray ray = new Ray(my_position, relative_to_final);
        bool obstacles_in_way = Physics.SphereCast(ray, SPHERE_CAST_RADIUS, out hit, distance_to_final, FighterCondition.obstacles_mask);
        if (obstacles_in_way)
        {
            bypassing = true;

            // Search for sub-targets around until trial reaches max_trial.
            int max_trial = 3;
            float search_radius = 150;
            List<Vector3> subTargetsAround = new List<Vector3>();       // Check for obstacles in way.
            List<Vector3> subTargetsAround_weak = new List<Vector3>();  // Does not check for obstacles.
            for (int trial = 1; trial <= max_trial; trial++)
            {
                // Expand search radius on each trial.
                float searchRadius = search_radius * trial;

                // Search sub-targets around.
                subTargetsAround_weak = Physics.OverlapSphere(my_position, searchRadius, SubTarget.mask, QueryTriggerInteraction.Collide)
                    .Select(s => s.transform.position)
                    .Where(t => !latestDestinations.Contains(t))
                    .ToList();
                subTargetsAround = subTargetsAround_weak
                    .Where(t =>
                    {
                        Ray ray_to_sub = new Ray(my_position, t - my_position);
                        float distance_to_sub = Vector3.Magnitude(t - my_position);
                        return !Physics.SphereCast(ray_to_sub, SPHERE_CAST_RADIUS, distance_to_sub, FighterCondition.obstacles_mask);
                    })
                    .ToList();

                // Break when sub-targets were found.
                if (subTargetsAround.Count > 0)
                {
                    break;
                }
            }

            // If no sub-targets were found, relax condition.
            if (subTargetsAround.Count < 1)
            {
                subTargetsAround = subTargetsAround_weak;
            }

            // Select sub-target which direction is closest to final destination.
            float min_degree = 360;
            foreach (Vector3 subTargetAround in subTargetsAround)
            {
                Vector3 relative_to_subAround = subTargetAround - my_position;
                float degree_to_final = Mathf.Abs(Vector3.SignedAngle(relative_to_final, relative_to_subAround, Vector3.up));
                if (degree_to_final < min_degree)
                {
                    min_degree = degree_to_final;
                    nextDestination = subTargetAround;
                }
            }

            // Update latest destinations.
            latestDestinations[cache_idx] = nextDestination;
            cache_idx = (cache_idx + 1) % MAX_CACHE;
        }
        else
        {
            bypassing = false;
            nextDestination = finalDestination;
        }
    }

    protected void ArrivalCheck()
    {
        if (arrived_at_next_destination) return;

        Vector3 relative_to_next = nextDestination - transform.position;
        if (Vector3.SqrMagnitude(relative_to_next) < Mathf.Pow(ARRIVE_DISTANCE, 2))
        {
            arrived_at_next_destination = true;
            if (bypassing)
            {
                SetNextDestination();
            }
            else
            {
                arrived_at_final_destination = true;
            }
        }
    }
}
