using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ZoneEffect : MonoBehaviour
{
    ParticleSystem[] particles;
    Transform aura;

    Tween animTween;
    [SerializeField] float animDuration;


    void Awake()
    {
        particles = GetComponentsInChildren<ParticleSystem>();
        aura = transform.Find("Aura");
        animTween = aura.DOScale(0, 0);
    }


    public void PlayEffect()
    {
        if (animTween.IsActive() && animTween.IsPlaying())
            animTween.Kill();
        animTween = aura.DOScale(1, animDuration)
                        .SetEase(Ease.OutElastic);

        foreach (ParticleSystem particle in particles)
        {
            particle.Play();
        }
    }

    /// <param name="immediate">trueにするとアニメーションなしでエフェクトが停止</param>
    public void StopEffect(bool immediate = false)
    {
        if (animTween.IsActive() && animTween.IsPlaying())
            animTween.Kill();
        animTween = aura.DOScale(0, animDuration)
                        .SetEase(Ease.OutBack);

        ParticleSystemStopBehavior stopBehavior = immediate ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;
        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, stopBehavior);
        }
    }
}
