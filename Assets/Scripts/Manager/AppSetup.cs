using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplicationSetup()
    {
        LoadPlayerInfo();
        SetFPS(PlayerInfo.I.fps);
        SetVolume(PlayerInfo.I.volume);
        SetBGM(PlayerInfo.I.bgmRatio);
    }

    public static void SetFPS(int fps)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }

    public static void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public static void SetBGM(float bgm)
    {
        BGMManager.I.SetBGMVolume(bgm);
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
