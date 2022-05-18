using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

// fighterbodyとの相対位置・回転を固定する
public class TransformFixation : MonoBehaviour
{
    [BoxGroup("Fixation"), SerializeField] bool xRot, yRot, zRot;
    [SerializeField] bool uturn_dependent;

    Transform fighter_trans;
    Movement movement;
    Vector3 default_localPosition;
    Vector3 default_angle;
    Vector3 default_localAngle;
    
    void Start()
    {
        fighter_trans = transform.parent;
        movement = fighter_trans.GetComponent<Movement>();
        default_localPosition = transform.localPosition;
        default_angle = transform.rotation.eulerAngles;
        default_localAngle = transform.localRotation.eulerAngles;
    }

    void Update()
    {
        // fighterbodyとの相対位置を固定
        Vector3 pos = fighter_trans.position + (Vector3.right * default_localPosition.x) + (Vector3.up * default_localPosition.y) + (Vector3.forward * default_localPosition.z);

        float x_angle, y_angle, z_angle;

        if(xRot) x_angle = default_angle.x;
        else x_angle = fighter_trans.rotation.eulerAngles.x + default_localAngle.x;

        if(yRot)
        {
            if(uturn_dependent)
            {
                if(movement.uTurndirection == 1) y_angle = default_angle.y;
                else y_angle = default_angle.y + 180;
            }
            else y_angle = default_angle.y;
        }
        else
        {
            if(uturn_dependent)
            {
                if(movement.uTurndirection == 1) y_angle = fighter_trans.rotation.eulerAngles.y + default_localAngle.y;
                else y_angle = fighter_trans.rotation.eulerAngles.y + default_localAngle.y + 180;
            }
            else y_angle = fighter_trans.rotation.eulerAngles.y + default_localAngle.y;
        }

        if(zRot) z_angle = default_angle.z;
        else z_angle = fighter_trans.rotation.eulerAngles.z + default_localAngle.z;

        Quaternion rot = Quaternion.Euler(x_angle, y_angle, z_angle);

        transform.SetPositionAndRotation(pos, rot);
    }
}
