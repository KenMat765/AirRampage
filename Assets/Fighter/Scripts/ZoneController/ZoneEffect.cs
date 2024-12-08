using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ZoneEffect : MonoBehaviour
{
    Material material;
    ParticleSystem particle;

    [SerializeField] float animDuration;

    Tween animTween;
    const string ALPHA_PROPERTY = "_Alpha";


    void Awake()
    {
        material = GetComponent<Renderer>().material;
        particle = GetComponentInChildren<ParticleSystem>();
    }


    /// <param name="immediate">trueにするとアニメーションなしでエフェクトが再生</param>
    public void PlayEffect(bool immediate = false)
    {
        if (animTween != null && animTween.IsPlaying())
            animTween.Kill(true);

        float duration = immediate ? 0 : animDuration;
        animTween = material.DOFloat(1, ALPHA_PROPERTY, duration);
        particle.Play();
    }

    /// <param name="immediate">trueにするとアニメーションなしでエフェクトが停止</param>
    public void StopEffect(bool immediate = false)
    {
        if (animTween != null && animTween.IsPlaying())
            animTween.Kill(true);

        if (immediate)
        {
            animTween = material.DOFloat(0, ALPHA_PROPERTY, 0);
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            animTween = material.DOFloat(0, ALPHA_PROPERTY, animDuration);
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
