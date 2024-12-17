using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ZoneEffect : MonoBehaviour
{
    ParticleSystem aura_particle;
    ParticleSystem end_particle;
    Transform aura;

    Tween animTween;
    [SerializeField] float animDuration;


    void Awake()
    {
        aura = transform.Find("Aura");
        aura_particle = aura.GetComponent<ParticleSystem>();
        end_particle = transform.Find("EndEffect").GetComponent<ParticleSystem>();
        animTween = aura.DOScale(0, 0);
    }


    public void PlayEffect()
    {
        if (animTween.IsActive() && animTween.IsPlaying())
            animTween.Kill();
        animTween = aura.DOScale(1, animDuration)
                        .SetEase(Ease.OutElastic);

        aura_particle.Play(true);
    }

    /// <param name="immediate">trueにするとアニメーションなしでエフェクトが停止</param>
    public void StopEffect(bool immediate = false)
    {
        if (animTween.IsActive() && animTween.IsPlaying())
            animTween.Kill();
        animTween = aura.DOScale(0, animDuration)
                        .SetEase(Ease.OutBack);

        ParticleSystemStopBehavior stopBehavior = immediate ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;
        aura_particle.Stop(true, stopBehavior);

        if (!immediate)
            end_particle.Play(true);
    }
}
