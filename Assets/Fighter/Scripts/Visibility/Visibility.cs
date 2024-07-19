using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visibility : MonoBehaviour
{
    // Reffered from uGUIManager to show HP Bar on top of visible fighters
    public bool isVisible { get; private set; } = false;

    [SerializeField] float visibleDistance = 500;
    [SerializeField] LayerMask obstacleLayer;

    Transform trans;
    bool isInsideCamera = false;


    void Awake()
    {
        // Cache transform.
        trans = transform;
    }

    void FixedUpdate()
    {
        // Always invisible if not inside camera.
        if (!isInsideCamera) return;

        // Get direction to main camera.
        Camera main_camera = Camera.main;
        if (main_camera == null) return;
        Vector3 camera_direction = main_camera.transform.position - trans.position;

        // Check distance between this fighter and camera.
        float camera_distance = camera_direction.magnitude;
        if (camera_distance > visibleDistance)
        {
            isVisible = false;
            return;
        }

        // Check for obstacles between camera and this fighter.
        bool has_obstacle = Physics.Raycast(trans.position, camera_direction, camera_distance, obstacleLayer.value);
        if (has_obstacle)
        {
            isVisible = false;
        }
        else
        {
            isVisible = true;
        }
    }

    void OnBecameVisible()
    {
        isInsideCamera = true;
    }

    void OnBecameInvisible()
    {
        isInsideCamera = false;
        isVisible = false;
    }

}
