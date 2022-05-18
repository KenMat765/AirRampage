using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    const float default_view = 60;
    Transform root_trans;
    Camera cam;
    Animator animator;
    bool fix_rotation = true;



    void Start()
    {
        root_trans = transform.parent;
        cam = GetComponent<Camera>();
        cam.fieldOfView = default_view;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        FixCameraRotation();
    }



    // z軸方向回転を抑える
    void FixCameraRotation()
    {
        if(fix_rotation)
        {
            Quaternion rootRot = transform.root.rotation;
            root_trans.rotation = Quaternion.Euler(rootRot.eulerAngles.x, rootRot.eulerAngles.y, 0);
        }
    }



    // Flip時に上を向かせる
    public void CameraLookUp(float euler_angle, float lookup_half_time)
    {
        fix_rotation = false;
        root_trans.DOLocalRotate(new Vector3(euler_angle, 0, 0), lookup_half_time, RotateMode.LocalAxisAdd)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => fix_rotation = true);
    }
    


    // ブースト時に視野角を変化
    public void ViewChanger(float destination_view, float duration)
    {
        DOTween.To(() => cam.fieldOfView, (x) => cam.fieldOfView = x, destination_view, duration);
    }



    // Cameraを初期状態に戻す
    public void ResetView(float duration = 0)
    {
        DOTween.To(() => cam.fieldOfView, (x) => cam.fieldOfView = x, default_view, duration);
    }



    // Animator起動
    public void CameraTurn(int direction)
    {
        animator.SetInteger("Direction", direction);
    }
}
