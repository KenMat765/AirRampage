using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KariFighter : MonoBehaviour
{
    [SerializeField] float speed;

    void Start()
    {

    }

    void Update()
    {
        // Move Forward.
        transform.position = Vector3.MoveTowards(
            transform.position,
            transform.position + (transform.forward * speed * Time.deltaTime),
            speed);

        // Rotation.
        float maxRotSpeed = 40;
        float maxTiltX = 55;  //縦
        float maxTiltZ = 60;  //左右
        float targetRotX = 0, relativeRotY = 0, targetRotZ = 0;
        Quaternion targetRot = default(Quaternion);

        if (Input.anyKey)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                targetRotX += maxTiltX;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                targetRotX -= maxTiltX;
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
}
