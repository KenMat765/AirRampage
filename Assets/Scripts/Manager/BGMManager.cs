using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : Singleton<BGMManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    [SerializeField] AudioSource bgmAudio;
    [SerializeField, Range(0f, 1f)] float maxVolume = 1.0f;

    void Start()
    {
        SetBGMVolume(PlayerInfo.I.bgmRatio);
    }

    public void SetBGMVolume(float volume_ratio)
    {
        float volume = maxVolume * volume_ratio;
        bgmAudio.volume = volume;
    }
}
