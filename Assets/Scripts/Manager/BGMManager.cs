using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : Singleton<BGMManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    [SerializeField] AudioSource bgmAudio;
    void Start()
    {
        bgmAudio.volume = PlayerInfo.I.bgm;
    }
    public void SetBGMVolume(float volume)
    {
        bgmAudio.volume = volume;
    }
}
