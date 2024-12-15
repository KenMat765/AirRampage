using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.Audio;


// 新規グループ・パラメータを追加したら、ここに"同じ名前で"追加する
public enum AudioGroup { BGM, SE, Zone }
public enum AudioParam { Volume, Lowpass_CutoffFreq }


public class AudioMixerManager : Singleton<AudioMixerManager>
{
    protected override bool dont_destroy_on_load { get; set; } = true;

    AudioMixer main_audioMixer;
    public AudioMixer GetMainAudioMixer() => main_audioMixer;

    protected override void Awake()
    {
        base.Awake();
        main_audioMixer = Resources.Load<AudioMixer>("MainAudioMixer");
    }


    // ExposedParam名 = Group名 + "_" + Param名
    string ParamName(AudioGroup group, AudioParam param) => group.ToString() + "_" + param.ToString();

    public float GetParam(AudioGroup group, AudioParam param)
    {
        main_audioMixer.GetFloat(ParamName(group, param), out float value);
        return value;
    }

    public void SetParam(AudioGroup group, AudioParam param, float value)
    {
        main_audioMixer.SetFloat(ParamName(group, param), value);
    }


    /// <summary>ローパスフィルタにこの値をセットすれば、実質フィルターをOFFにすることができる</summary>
    public const float FILTER_MAX_FREQ = 22000.0f;
}
