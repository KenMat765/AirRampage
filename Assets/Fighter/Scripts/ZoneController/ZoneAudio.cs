using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class ZoneAudio : MonoBehaviour
{
    [SerializeField] AudioSource enterSound;
    [SerializeField] AudioSource staySound;
    [SerializeField] AudioSource exitSound;

    [SerializeField] float fadeDuration;

    // Stay音はフェードインさせるので、初期音量をキャッシュしておく
    float stayVolume;

    public void PlayEnterSound() => enterSound.Play();
    public void PlayStaySound() => staySound.Play();
    public void PlayExitSound() => exitSound.Play();

    public void StopEnterSound() => enterSound.Stop();
    public void StopStaySound() => staySound.Stop();
    public void StopExitSound() => exitSound.Stop();

    public void FadeInStaySound()
    {
        staySound.DOFade(stayVolume, fadeDuration)
            .OnStart(PlayStaySound);
    }
    public void FadeOutStaySound()
    {
        staySound.DOFade(0, fadeDuration)
            .OnComplete(StopStaySound);
    }


    void Awake()
    {
        stayVolume = staySound.volume;
    }


    /// <summary>ゾーン突入音を鳴らした後、指定した秒数後にゾーン最中の音を鳴らす</summary>
    public async void PlayEnterAndStaySound(float interval)
    {
        PlayEnterSound();
        await UniTask.Delay(TimeSpan.FromSeconds(interval));
        FadeInStaySound();
    }
}
