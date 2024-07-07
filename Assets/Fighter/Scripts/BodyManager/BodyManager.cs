using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyManager : MonoBehaviour
{
    // Reffered from uGUIManager to show HP Bar on top of visible fighters
    public bool visible { get; set; } = false;

    Transform trans;
    int obstacleMask = 0;
    float visibleDistance = 500;
    bool insideCamera = false;


    void Awake()
    {
        // Cache transform.
        trans = transform;

        // Set obstacles layer mask.
        obstacleMask += 1 << LayerMask.NameToLayer("Terrain");
        obstacleMask += 1 << LayerMask.NameToLayer("Terminal");
        obstacleMask += 1 << LayerMask.NameToLayer("RedTerminal");
        obstacleMask += 1 << LayerMask.NameToLayer("BlueTerminal");
        obstacleMask += 1 << LayerMask.NameToLayer("Structure");
    }

    void FixedUpdate()
    {
        // Always invisible if not inside camera.
        if (!insideCamera) return;

        // Get direction to main camera.
        Camera main_camera = Camera.main;
        if (main_camera == null)
        {
            Debug.Log("Camera null");
            return;
        }
        Vector3 camera_direction = main_camera.transform.position - trans.position;

        // Check distance between this fighter and camera.
        float camera_distance = camera_direction.magnitude;
        if (camera_distance > visibleDistance)
        {
            visible = false;
            return;
        }

        // Check for obstacles between camera and this fighter.
        bool has_obstacle = Physics.Raycast(trans.position, camera_direction, camera_distance, obstacleMask);
        if (has_obstacle)
        {
            visible = false;
        }
        else
        {
            visible = true;
        }
    }

    void OnBecameVisible()
    {
        insideCamera = true;
    }

    void OnBecameInvisible()
    {
        insideCamera = false;
        visible = false;
    }
}
