using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplicationSetup()
    {
        FPSSetup();
        LoadPlayerInfo();
    }

    static void FPSSetup()
    {
        Application.targetFrameRate = 60;
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
