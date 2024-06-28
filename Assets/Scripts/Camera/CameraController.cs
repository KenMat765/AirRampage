using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class CameraController : Singleton<CameraController>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    public Camera cam { get; private set; }
    public ViewType viewType;

    [SerializeField] GameObject cam_fps, cam_tps_near, cam_tps_far;

    float shiftSpeed;
    [SerializeField] float defaultShiftSpeed;
    [SerializeField] float rotationSpeed;

    [Header("Fix Rotation")]
    public bool xAxis;
    public bool yAxis;
    public bool zAxis;

    Transform fighterTrans;

    Vector3 currentRelativePos = Vector3.zero;
    Vector3 targetRelativePos = Vector3.zero;
    bool control_rot = true;

    Animator animator;
    float default_view;

    async void Start()
    {
        shiftSpeed = defaultShiftSpeed;

        // Wait until fighterTrans is set from ParticipantManager.
        await UniTask.WaitUntil(() => fighterTrans != null);

        default_view = cam.fieldOfView;
    }

    void Update()
    {
        if (viewType == ViewType.FPS) return;

        if (fighterTrans == null) return;

        // Position controll
        currentRelativePos = Vector3.MoveTowards(currentRelativePos, targetRelativePos, shiftSpeed * Time.deltaTime);
        Vector3 target_pos = fighterTrans.position + currentRelativePos;
        transform.position = target_pos;

        // Rotation controll
        if (control_rot)
        {
            float x_euler = xAxis ? 0 : fighterTrans.rotation.eulerAngles.x;
            float y_euler = yAxis ? 0 : fighterTrans.rotation.eulerAngles.y;
            float z_euler = zAxis ? 0 : fighterTrans.rotation.eulerAngles.z;
            Quaternion target_rot = Quaternion.Euler(x_euler, y_euler, z_euler);
            Quaternion slerp_rot = Quaternion.Slerp(transform.rotation, target_rot, rotationSpeed * Time.deltaTime);
            transform.rotation = slerp_rot;
        }
    }

    public void SetupPlayerCamera(Transform fighter_trans, ViewType view_type)
    {
        fighterTrans = fighter_trans;
        ChangeViewType(view_type);
    }

    void ChangeViewType(ViewType new_viewType)
    {
        viewType = new_viewType;
        switch (viewType)
        {
            case ViewType.FPS:
                cam = cam_fps.GetComponent<Camera>();
                cam_fps.SetActive(true);
                cam_tps_near.SetActive(false);
                cam_tps_far.SetActive(false);
                transform.SetParent(fighterTrans.Find("fighterbody"));
                transform.localPosition = Vector3.zero;
                transform.localScale = Vector3.one;
                break;

            case ViewType.TPS_NEAR:
                cam = cam_tps_near.GetComponent<Camera>();
                cam_fps.SetActive(false);
                cam_tps_near.SetActive(true);
                cam_tps_far.SetActive(false);
                transform.SetParent(null);
                transform.position = fighterTrans.position;
                animator = cam_tps_near.GetComponent<Animator>();
                transform.localScale = fighterTrans.localScale;
                break;

            case ViewType.TPS_FAR:
                cam = cam_tps_far.GetComponent<Camera>();
                cam_fps.SetActive(false);
                cam_tps_near.SetActive(false);
                cam_tps_far.SetActive(true);
                transform.SetParent(null);
                transform.position = fighterTrans.position;
                animator = cam_tps_far.GetComponent<Animator>();
                transform.localScale = fighterTrans.localScale;
                break;
        }
        CameraManager.SetupCameraInScene();
    }

    // Flip時に上を向かせる
    public Tween LookUp(float euler_angle, float lookup_half_time)
    {
        if (viewType == ViewType.FPS) return null;

        control_rot = false;
        Tween tween = transform.DOLocalRotate(new Vector3(euler_angle, 0, 0), lookup_half_time, RotateMode.LocalAxisAdd)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => control_rot = true);
        return tween;
    }

    // ブースト時に視野角を変化
    public Tween ChangeView(float destination_view, float duration)
    {
        if (viewType == ViewType.FPS) return null;
        return DOTween.To(() => cam.fieldOfView, (x) => cam.fieldOfView = x, destination_view, duration);
    }

    // Cameraを初期状態に戻す
    public Tween ResetView(float duration = 0)
    {
        if (viewType == ViewType.FPS) return null;
        return DOTween.To(() => cam.fieldOfView, (x) => cam.fieldOfView = x, default_view, duration);
    }

    // Animator起動
    public void TurnCamera(int direction)
    {
        if (viewType == ViewType.FPS) return;
        animator.SetInteger("Direction", direction);
    }

    public void ShiftCameraPos(Vector3 relative_pos, float? shift_speed = null)
    {
        if (viewType == ViewType.FPS) return;
        shiftSpeed = shift_speed.HasValue ? shift_speed.Value : defaultShiftSpeed;
        targetRelativePos = relative_pos;
    }

    public void ResetCameraPos(float? shift_speed = null)
    {
        shiftSpeed = shift_speed.HasValue ? shift_speed.Value : defaultShiftSpeed;
        targetRelativePos = Vector3.zero;
    }
}

public enum ViewType
{
    FPS,
    TPS_NEAR,
    TPS_FAR
}
