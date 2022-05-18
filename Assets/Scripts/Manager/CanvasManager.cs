using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 一番最初のシーンに配置すること！！
public class CanvasManager : Singleton<CanvasManager>
{
    protected override bool dont_destroy_on_load { get; set; } = true;
    public static float canvas_width;
    public static float canvas_height;
    public static Vector3 canvas_scale;

    protected override void Awake()
    {
        base.Awake();
        RectTransform canvas_rect = GameObject.Find("Canvas").GetComponent<RectTransform>();

        // SceneManager.activeSceneChanged += (Scene before, Scene after) => canvas_rect = GameObject.Find("Canvas").GetComponent<RectTransform>();
        
        canvas_width = canvas_rect.rect.width;
        canvas_height = canvas_rect.rect.height;
        canvas_scale = canvas_rect.localScale;
    }
}
