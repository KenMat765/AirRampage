using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AppSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplicationSetup()
    {
        LoadPlayerInfo();
        SetFPS(PlayerInfo.I.fps);
        SetSE(PlayerInfo.I.seRatio);
        SetBGM(PlayerInfo.I.bgmRatio);
    }

    public static void SetFPS(int fps)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }

    public static void SetSE(float se)
    {
        float se_db = 20 * Mathf.Log10(se);
        se_db = Mathf.Clamp(se_db, AudioMixerManager.VOLUME_MIN_DB, 0);
        AudioMixerManager.I.SetParam(AudioGroup.SE, AudioParam.Volume, se_db);
        AudioMixerManager.I.SetParam(AudioGroup.Zone, AudioParam.Volume, se_db);
    }

    public static void SetBGM(float bgm)
    {
        float bgm_db = 20 * Mathf.Log10(bgm);
        bgm_db = Mathf.Clamp(bgm_db, AudioMixerManager.VOLUME_MIN_DB, 0);
        AudioMixerManager.I.SetParam(AudioGroup.BGM, AudioParam.Volume, bgm_db);
    }

    static void LoadPlayerInfo()
    {
        PlayerInfo playerInfo = SaveManager.LoadData<PlayerInfo>("PlayerInfo");
        if (playerInfo != null)
        {
            PlayerInfo.I = playerInfo;
        }
        else
        {
            PlayerInfo.I = new PlayerInfo();
        }
    }
}
