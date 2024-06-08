using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;

// 機体を動かすクラス
public abstract class Movement : NetworkBehaviour
{
    public FighterCondition fighterCondition { get; set; }

    // Used for death animation.
    Rigidbody rigidBody;
    AudioSource explosionSound1, explosionSound2;
    ParticleSystem explosion1, explosion2, explosionTrail;

    // Is called at every clients.
    public virtual void OnDeath()
    {
        ready4action = false;
        StartCoroutine(DeathAnimation());
    }

    IEnumerator DeathAnimation()
    {
        // Start falling, and play first explision effect.
        rigidBody.drag = 0;
        rigidBody.useGravity = true;
        explosion1.Play();
        explosionSound1.Play();
        explosionTrail.Play();

        // 1.6f : effect play time of explosion dead.
        yield return new WaitForSeconds(2.0f);

        // Stop falling, and play second explision effect.
        rigidBody.drag = float.PositiveInfinity;
        rigidBody.useGravity = false;
        explosion2.Play();
        explosionSound2.Play();
        explosionTrail.Stop();
        fighterCondition.body.SetActive(false);

        // 1.6f : effect play time of explosion dead.
        yield return new WaitForSeconds(1.8f);

        // Return to start position.
        transform.position = start_pos;
        transform.rotation = start_rot;
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


    Vector3 start_pos;
    Quaternion start_rot;

    protected const float maxTiltX = 55;  //縦
    protected const float maxTiltZ = 80;  //左右

    protected bool controllable = false;
    public virtual void Controllable(bool controllable) => this.controllable = controllable;



    protected virtual void Awake()
    {
        fighterCondition = GetComponent<FighterCondition>();
        start_pos = transform.position;
        start_rot = transform.rotation;
        rigidBody = GetComponent<Rigidbody>();
        Transform explosion_trans = transform.Find("Explosion");
        Transform explosion_trans1 = explosion_trans.Find("ExplosionDead1");
        Transform explosion_trans2 = explosion_trans.Find("ExplosionDead2");
        explosionSound1 = explosion_trans1.GetComponent<AudioSource>();
        explosionSound2 = explosion_trans2.GetComponent<AudioSource>();
        explosion1 = explosion_trans1.GetComponent<ParticleSystem>();
        explosion2 = explosion_trans2.GetComponent<ParticleSystem>();
        explosionTrail = explosion_trans.Find("ExplosionTrail").GetComponent<ParticleSystem>();
    }

    protected virtual void FixedUpdate()
    {
        MoveForward();

        if (fighterCondition.isDead) return;
        if (!controllable) return;

        Rotate();
        FourActionExe();
    }

    protected void MoveForward()
    {
        float speed = fighterCondition.speed;
        transform.position = Vector3.MoveTowards(
            transform.position,
            transform.position + (transform.forward * speed * Time.deltaTime * uTurndirection),
            speed);
    }

    protected abstract void Rotate();



    // 4アクション ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected Animator anim;
    protected float uturnTime, flipTime, rollTime;

    // KariCameraで使うためにpublic(後でprotectedに直す)
    public int uTurndirection { get; set; } = 1;

    bool ready4action = true;

    protected void Uturn()
    {
        if (ready4action) StartCoroutine(uTurn());
        if (BattleInfo.isMulti && IsOwner) UturnServerRpc(OwnerClientId);
    }
    protected virtual IEnumerator uTurn()
    {
        ready4action = false;
        anim.SetInteger("FighterAnim", 2 * uTurndirection);

        yield return new WaitForSeconds(0.33f);

        uTurndirection *= -1;
        anim.SetInteger("FighterAnim", uTurndirection);

        yield return new WaitForSeconds(uturnTime - 0.33f);

        ready4action = true;
    }

    protected void Flip()
    {
        if (ready4action) StartCoroutine(flip());
        if (BattleInfo.isMulti && IsOwner) FlipServerRpc(OwnerClientId);
    }
    protected virtual IEnumerator flip()
    {
        ready4action = false;
        anim.SetInteger("FighterAnim", 3 * uTurndirection);
        float speed_temp = fighterCondition.speed;
        fighterCondition.PauseGradingSpeed(0);

        yield return new WaitForSeconds(1.2f);

        fighterCondition.ResumeGradingSpeed();

        yield return new WaitForSeconds(0.3f);

        anim.SetInteger("FighterAnim", uTurndirection);

        yield return new WaitForSeconds(flipTime - 1.5f + 3.0f);

        ready4action = true;
    }

    bool rollReady = true;
    protected float roll_distance = 12;

    protected void LeftRoll(float delay)
    {
        if (ready4action && rollReady) StartCoroutine(leftroll(delay));
        if (BattleInfo.isMulti && IsOwner) LeftRoleServerRpc(OwnerClientId, delay);
    }
    protected virtual IEnumerator leftroll(float delay)
    {
        ready4action = false;
        rollReady = false;
        anim.SetInteger("FighterAnim", 5 * uTurndirection);
        transform.DOBlendableMoveBy(transform.right * roll_distance * uTurndirection * -1, rollTime)
            .SetEase(Ease.OutQuint);

        yield return new WaitForSeconds(rollTime);

        anim.SetInteger("FighterAnim", uTurndirection);
        ready4action = true;

        yield return new WaitForSeconds(delay);

        rollReady = true;
    }

    protected void RightRoll(float delay)
    {
        if (ready4action && rollReady) StartCoroutine(rightroll(delay));
        if (BattleInfo.isMulti && IsOwner) RightRoleServerRpc(OwnerClientId, delay);
    }
    protected virtual IEnumerator rightroll(float delay)
    {
        ready4action = false;
        rollReady = false;
        anim.SetInteger("FighterAnim", 4 * uTurndirection);
        transform.DOBlendableMoveBy(transform.right * roll_distance * uTurndirection, rollTime)
            .SetEase(Ease.OutQuint);

        yield return new WaitForSeconds(rollTime);

        anim.SetInteger("FighterAnim", uTurndirection);
        ready4action = true;

        yield return new WaitForSeconds(delay);

        rollReady = true;
    }

    protected virtual void FourActionExe() { }

    [ServerRpc]
    void FlipServerRpc(ulong senderId) => FlipClientRpc(senderId);
    [ServerRpc]
    void UturnServerRpc(ulong senderId) => UturnClientRpc(senderId);
    [ServerRpc]
    void LeftRoleServerRpc(ulong senderId, float delay) => LeftRoleClientRpc(senderId, delay);
    [ServerRpc]
    void RightRoleServerRpc(ulong senderId, float delay) => RightRoleClientRpc(senderId, delay);
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



    // AIのみ使用 /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected float rotationSpeed { get; set; }
    protected const float distanceBorder = 25f;
    protected float sqr_distanceBorder { get { return distanceBorder * distanceBorder; } }
    protected const float searchRadius_orig = 150;
    [SerializeField] protected Vector3 finalDestination, nextDestination;
    protected Vector3 relative_to_final => finalDestination - transform.position;
    protected Vector3 relative_to_next => nextDestination - transform.position;
    protected bool arrived_at_final_destination;
    protected bool bypassing;

    // Initialize latestDestinations only at AIMovement & ZakoMovement, because PlayerMovement dosen't use this.
    protected const short max_cashe = 10;
    protected Vector3[] latestDestinations;

    // Only for Terminal Conquest.
    protected bool destination_is_terminal;

    bool arrived_at_next_destination;
    int index_counter = 0;


    protected void SetFinalDestination(Vector3 destination, bool destination_is_terminal = false)
    {
        arrived_at_final_destination = false;
        finalDestination = destination;
        this.destination_is_terminal = destination_is_terminal;
        System.Array.Fill(latestDestinations, Vector3.zero);
        SetNextDestination();
    }

    void SetNextDestination()
    {
        arrived_at_next_destination = false;

        Vector3 my_position = transform.position;

        // When there is a terrain between current position and final destination.
        RaycastHit hit;
        if (Physics.Raycast(my_position, relative_to_final, out hit, Vector3.Magnitude(relative_to_final), FighterCondition.obstacles_mask) && !(destination_is_terminal && hit.transform.position == finalDestination))
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
                float searchRadius = searchRadius_orig * trial;

                // Search sub targets around.
                subTargetsAround_weak = Physics.OverlapSphere(my_position, searchRadius, SubTarget.mask, QueryTriggerInteraction.Collide)
                    .Select(s => s.transform.position)
                    .Where(t => !latestDestinations.Contains(t))
                    .ToList();
                subTargetsAround = subTargetsAround_weak
                    .Where(t => !Physics.Raycast(my_position, t - my_position, Vector3.Magnitude(t - my_position), FighterCondition.obstacles_mask))
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
                // If no sub targets were found, relax condiion.
                subTargetsAround = subTargetsAround_weak;
            }


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
        float sqr_distanceBorder = destination_is_terminal ? this.sqr_distanceBorder * 3 * 3 : this.sqr_distanceBorder;

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
}
