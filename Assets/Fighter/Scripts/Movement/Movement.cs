using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Cysharp.Threading.Tasks;
using System;

// 機体を動かすクラス
public abstract class Movement : NetworkBehaviour
{
    public FighterCondition fighterCondition { get; set; }

    protected virtual void Awake()
    {
        fighterCondition = GetComponent<FighterCondition>();
        col = GetComponent<Collider>();
        col.enabled = false;

        // For death animation.
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
    protected const float maxTiltX = 55;  //縦
    protected const float maxTiltZ = 60;  //左右
    Collider col;

    protected void MoveForward()
    {
        float speed = fighterCondition.speed;

        // Move by transform (Slip-through occurs)
        // transform.position = Vector3.MoveTowards(
        //     transform.position,
        //     transform.position + (transform.forward * speed * Time.deltaTime * uTurndirection),
        //     speed);

        // Move by rigidbody (Slip-through does not occur)
        rigidBody.velocity = transform.forward * speed * uTurndirection;
    }

    protected abstract void Rotate();

    // Enables rotation & 4actions when true.
    protected bool controllable { get; private set; } = false;
    public virtual void Controllable(bool controllable)
    {
        this.controllable = controllable;
        // If controllable, enable collider to detect collision to obstacles.
        col.enabled = controllable;
    }



    // 4-Actions ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected Animator anim;
    protected float uturnTime, flipTime, rollTime;

    // KariCameraで使うためにpublic(後でprotectedに直す)
    public int uTurndirection { get; set; } = 1;

    protected bool ready4action = true;

    protected void Uturn()
    {
        if (ready4action) StartCoroutine(uTurn());
        if (IsOwner) UturnServerRpc(OwnerClientId);
    }
    protected virtual IEnumerator uTurn() { return null; }

    protected void Flip()
    {
        if (ready4action) StartCoroutine(flip());
        if (IsOwner) FlipServerRpc(OwnerClientId);
    }
    protected virtual IEnumerator flip() { return null; }

    protected float rollDistance = 15;

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
    void FlipServerRpc(ulong senderId) => FlipClientRpc(senderId);
    [ServerRpc]
    void UturnServerRpc(ulong senderId) => UturnClientRpc(senderId);
    [ServerRpc]
    void LeftRollServerRpc(ulong senderId, float delay) => LeftRoleClientRpc(senderId, delay);
    [ServerRpc]
    void RightRollServerRpc(ulong senderId, float delay) => RightRoleClientRpc(senderId, delay);
    [ClientRpc]
    void FlipClientRpc(ulong senderId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        Flip();
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

        // Get layer of collided object.
        int col_layer = 1 << col.gameObject.layer;

        // Crash when collided to obstacle.
        if ((obstacleMask & col_layer) != 0)
        {
            // For Debug.
            if (fighterCondition.fighterNo.Value < 8)
            {
                Debug.Log("<color=red>Crashed!!</color>", gameObject);
                Debug.Log("<color=red>Hit object</color>", col.gameObject);
                // Time.timeScale = 0;
            }
            fighterCondition.Death(-1, FighterCondition.SPECIFIC_DEATH_COLLISION);
        }
    }



    // Death & Revival ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Used for death animation.
    Rigidbody rigidBody;
    Transform explosion2Trans;
    AudioSource explosionSound1, explosionSound2;
    ParticleSystem explosion1, explosion2, explosionTrail;

    // Is called at every clients.
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

        // 1.6f : effect play time of explosion dead.
        yield return new WaitForSeconds(2.0f);

        // Stop falling, and play second explision effect.
        rigidBody.useGravity = false;
        rigidBody.velocity = Vector3.zero;
        // Put out explosion2 from fighterbody before deactivating fighterbody.
        explosion2Trans.parent = transform;
        explosion2.Play();
        explosionSound2.Play();
        explosionTrail.Stop();
        fighterCondition.body.SetActive(false);

        // 1.6f : effect play time of explosion dead.
        yield return new WaitForSeconds(1.8f);

        // Put back explosion2.
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

    // Judged arrived when distance to destination is smaller than this value.
    // Also used to judge whether shooter is near or far from this fighter.
    protected const float DISTANCE_BORDER = 30f;
    protected float SQR_DISTANCE_BORDER { get { return DISTANCE_BORDER * DISTANCE_BORDER; } }
    protected const float SEARCH_RADIUS_ORG = 150;  // Used for searching sub-targets around.
    protected const float CAST_RADIUS = 5;           // Used for detecting obstacles in way.
    protected Vector3 finalDestination, nextDestination;
    public Vector3 relative_to_final => finalDestination - transform.position;
    public Vector3 relative_to_next => nextDestination - transform.position;
    protected bool arrived_at_final_destination, arrived_at_next_destination;
    protected bool bypassing;
    int index_counter = 0;

    // Initialize latestDestinations only at AIMovement & ZakoMovement, because PlayerMovement dosen't use this.
    protected const short max_cashe = 10;
    protected Vector3[] latestDestinations;

    // Only for Terminal Conquest.
    protected bool destination_is_terminal;


    protected void SetFinalDestination(Vector3 destination, bool destination_is_terminal = false)
    {
        arrived_at_final_destination = false;
        finalDestination = destination;
        this.destination_is_terminal = destination_is_terminal;
        Array.Fill(latestDestinations, Vector3.zero);
        SetNextDestination();
    }

    protected void SetNextDestination()
    {
        arrived_at_next_destination = false;

        Vector3 my_position = transform.position;

        // When there is a obstacle between current position and final destination.
        Ray ray = new Ray(my_position, relative_to_final);
        RaycastHit hit;
        float max_distance = Vector3.Magnitude(relative_to_final);
        if (Physics.SphereCast(ray, CAST_RADIUS, out hit, max_distance, FighterCondition.obstacles_mask) && !(destination_is_terminal && hit.transform.position == finalDestination))
        {
            // Set as bypassing.
            bypassing = true;

            // Search for sub targets around until trial reaches max_trial.
            List<Vector3> subTargetsAround = new List<Vector3>();       // Check for obstacles in way.
            List<Vector3> subTargetsAround_weak = new List<Vector3>();  // Does not check for obstacles.
            const int max_trial = 3;
            for (int trial = 1; trial <= max_trial; trial++)
            {
                // Expand search radius on each trial.
                float searchRadius = SEARCH_RADIUS_ORG * trial;

                // Search sub targets around.
                subTargetsAround_weak = Physics.OverlapSphere(my_position, searchRadius, SubTarget.mask, QueryTriggerInteraction.Collide)
                    .Select(s => s.transform.position)
                    .Where(t => !latestDestinations.Contains(t))
                    .ToList();
                subTargetsAround = subTargetsAround_weak
                    .Where(t =>
                    {
                        Ray ray2sub = new Ray(my_position, t - my_position);
                        float max_distance2sub = Vector3.Magnitude(t - my_position);
                        return !Physics.SphereCast(ray2sub, CAST_RADIUS, max_distance2sub, FighterCondition.obstacles_mask);
                    })
                    .ToList();

                // break when sub target was found.
                if (subTargetsAround.Count > 0)
                {
                    break;
                }
            }

            // If there were no sub targets around, warn it.
            if (subTargetsAround.Count < 1)
            {
#if UNITY_EDITOR
                Debug.LogWarning("周囲にサブターゲットがありません. subTargetsAround_weak: " + subTargetsAround_weak.Count, gameObject);
#endif
                // If no sub targets were found, relax condition.
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
            latestDestinations[index_counter] = nextDestination;
            index_counter = (index_counter + 1) % max_cashe;
        }

        // When there are no obstacles between current position and final destination.
        else
        {
            bypassing = false;
            nextDestination = finalDestination;
        }
    }

    protected void ArrivalCheck()
    {
        // Expand distance border when destination is terminal.
        float sqr_distanceBorder = destination_is_terminal ? this.SQR_DISTANCE_BORDER * 3 * 3 : this.SQR_DISTANCE_BORDER;

        // When arrived at next destination.
        if (Vector3.SqrMagnitude(relative_to_next) < sqr_distanceBorder && !arrived_at_next_destination)
        {
            arrived_at_next_destination = true;

            // When arrived at midpoint.
            if (bypassing)
            {
                SetNextDestination();
            }

            // When arrived at final destination.
            else
            {
                arrived_at_final_destination = true;
            }
        }
    }

    protected bool ObstacleIsInFront(float max_distance)
    {
        Transform trans = transform;
        Ray ray = new Ray(trans.position, trans.forward * uTurndirection);
        return Physics.SphereCast(ray, CAST_RADIUS, max_distance, FighterCondition.obstacles_mask);
    }
}
