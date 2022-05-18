using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class CameraManager
{
    static Camera mainCamera_in_scene;
    static Camera fadeCamera;

    [RuntimeInitializeOnLoadMethod]
    static void FirstSetup()
    {
        fadeCamera = GameObject.Find("FadeCamera").GetComponent<Camera>();
        SetMainCamera();
        SetCameraStackInMain();
    }

    public static void SetupCameraInScene()
    {
        RemoveAllStackCameras();
        SetMainCamera();
        SetCameraStackInMain();
    }
    
    static void SetMainCamera()
    {
        mainCamera_in_scene = Camera.main;
    }

    static void SetCameraStackInMain()
    {
        Camera[] overlayCameras = GameObject.FindGameObjectsWithTag("OverlayCamera").Select(g => g.GetComponent<Camera>()).ToArray();
        var cameraData = mainCamera_in_scene.GetUniversalAdditionalCameraData();
        foreach(Camera overlayCamera in overlayCameras) cameraData.cameraStack.Add(overlayCamera);
        cameraData.cameraStack.Add(fadeCamera);
    }

    static void RemoveAllStackCameras()
    {
        if(mainCamera_in_scene == null) return;

        var cameraData = mainCamera_in_scene.GetUniversalAdditionalCameraData();
        if(cameraData.cameraStack.Count > 0) cameraData.cameraStack.Clear();
    }
}
