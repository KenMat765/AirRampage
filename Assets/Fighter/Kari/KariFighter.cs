using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KariFighter : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] bool moveByTransform = false;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Move Forward.
        Vector3 target_pos = transform.position + (transform.forward * speed * Time.deltaTime);
        if (moveByTransform)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target_pos,
                speed);
        }
        else
        {
            rb.velocity = transform.forward * speed;
        }

        // Rotation.
        float maxRotSpeed = 40;
        float maxTiltX = 55;  //縦
        float maxTiltZ = 60;  //左右
        float relativeRotY = 0, targetRotZ = 0;
        Quaternion targetRot = default(Quaternion);

        if (Input.anyKey)
        {
            // Control X rotation only when not colliding to slopes.
            if (!collidingSlope)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    targetRotX = maxTiltX;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    targetRotX = -maxTiltX;
                }
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                relativeRotY = maxRotSpeed;
                targetRotZ = maxTiltZ;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                relativeRotY = maxRotSpeed * -1;
                targetRotZ = maxTiltZ * -1;
            }
            targetRot = Quaternion.Euler(targetRotX * -1, transform.rotation.eulerAngles.y + relativeRotY, targetRotZ * -1);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.05f);
        }
        else
        {
            FixTilt();
        }
    }

    void FixTilt()
    {
        const float fix_time = 0.05f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0), fix_time);
    }



    // Obstacle (= Terrain + Structure) collide detection.
    [SerializeField] LayerMask obstacleLayer;
    [SerializeField] float slopeThresh;
    [SerializeField] bool collidingSlope = false;
    [SerializeField] float targetRotX;   // deg
    [SerializeField] int colCount;

    void OnCollisionEnter(Collision col)
    {
        int col_layer = 1 << col.gameObject.layer;
        if ((obstacleLayer & col_layer) != 0)
        {
            colCount++;
        }
    }

    void OnCollisionExit(Collision col)
    {
        int col_layer = 1 << col.gameObject.layer;
        if ((obstacleLayer & col_layer) != 0)
        {
            colCount--;
        }
    }

    // void OnCollisionStay(Collision col)
    // {
    //     // Debug.Log("<color=yellow>OnCollisionStay</color>");
    //     // return;

    //     int col_layer = 1 << col.gameObject.layer;
    //     if ((obstacleLayer & col_layer) != 0)
    //     {
    //         Vector3 normal = col.contacts[0].normal;
    //         float slope = GetSlopeFromNormal(normal);
    //         Debug.Log($"{slope} degrees");
    //         if (slope > slopeThresh)
    //         {
    //             Debug.Log("<color=red>DEAD</color>");
    //         }
    //         else
    //         {
    //             Debug.Log("<color=yellow>Colliding Slope</color>");
    //             collidingSlope = true;
    //             targetRotX = slope;
    //         }
    //     }
    // }

    float GetSlopeFromNormal(Vector3 normal)
    {
        // 法線から勾配を計算（角度に変換）
        float slope = Mathf.Acos(normal.y) * Mathf.Rad2Deg;
        return slope;
    }
}
