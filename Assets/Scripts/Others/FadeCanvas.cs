using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FadeCanvas : Singleton<FadeCanvas>
{
    protected override bool dont_destroy_on_load { get; set; } = true;

    float fade_duration = 0.2f;
    [SerializeField] float blinkDuration = 0;

    [SerializeField] Image panel;
    [SerializeField] Image logo;
    [SerializeField] Text nowLoading;
    Sequence start_seq;
    Sequence blink_seq;



    void Start()
    {
        panel.color = Color.black;
        panel.fillAmount = 0;
        logo.color = new Color(1, 1, 1, 0);
        nowLoading.color = new Color(1, 1, 1, 0);
    }



    // シーン開始
    public float FadeIn(FadeType fadeType)
    {
        switch (fadeType)
        {
            case FadeType.gradually:
                panel.color = Color.black;
                panel.fillAmount = 1;
                panel.DOFade(0, fade_duration);
                break;

            case FadeType.bottom:
                panel.fillMethod = Image.FillMethod.Vertical;
                panel.fillOrigin = (int)Image.OriginVertical.Bottom;
                panel.fillAmount = 1;
                panel.DOFillAmount(0, fade_duration);
                break;

            case FadeType.left:
                panel.fillMethod = Image.FillMethod.Horizontal;
                panel.fillOrigin = (int)Image.OriginHorizontal.Left;
                panel.fillAmount = 1;
                panel.DOFillAmount(0, fade_duration);
                break;
        }
        return fade_duration;
    }

    // シーン終了
    public float FadeOut(FadeType fadeType)
    {
        float duration = fade_duration;
        switch (fadeType)
        {
            case FadeType.gradually:
                float duration_multiplier = 5;
                panel.color = Color.clear;
                panel.fillAmount = 1;
                panel.DOFade(1, fade_duration * duration_multiplier);
                duration *= duration_multiplier;
                break;

            case FadeType.bottom:
                panel.fillMethod = Image.FillMethod.Vertical;
                panel.fillOrigin = (int)Image.OriginVertical.Bottom;
                panel.fillAmount = 0;
                panel.DOFillAmount(1, fade_duration);
                break;

            case FadeType.left:
                panel.fillMethod = Image.FillMethod.Horizontal;
                panel.fillOrigin = (int)Image.OriginHorizontal.Left;
                panel.fillAmount = 0;
                panel.DOFillAmount(1, fade_duration);
                break;
        }
        return duration;
    }

    public float StartBlink()
    {
        const float start_duration = 0.2f;
        const float start_alpha = 0.5f;
        const float end_alpha = 0.7f;
        const float loop_interval = 0.5f;

        blink_seq = DOTween.Sequence();
        blink_seq.Join(logo.DOFade(end_alpha, loop_interval));
        blink_seq.Join(nowLoading.DOFade(end_alpha, loop_interval));
        blink_seq.SetLoops(-1, LoopType.Yoyo);

        start_seq = DOTween.Sequence();
        start_seq.Join(logo.DOFade(start_alpha, start_duration));
        start_seq.Join(nowLoading.DOFade(start_alpha, start_duration));

        start_seq.Play()
            .OnComplete(() => blink_seq.Play());

        return blinkDuration;
    }

    public void StopBlink()
    {
        if (blink_seq.IsActive() && blink_seq.IsPlaying()) blink_seq.Kill();
        logo.color = new Color(1, 1, 1, 0);
        nowLoading.color = new Color(1, 1, 1, 0);
    }
}

public enum FadeType
{
    gradually,
    bottom,
    left
}