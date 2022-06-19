using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 
// 
// 
public class AppSetup : MonoBehaviour
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

        // 
        // 
        // 
        iii = playerInfo;

        if (playerInfo != null)
        {
            PlayerInfo.I = playerInfo;
        }
    }

    // 
    // 
    // 
    static PlayerInfo iii;
    void Start()
    {
        LoadPlayerInfo();
        if (iii != null)
        {
            // 
            // 
            // 
            InfoCanvas.I.OpenFrameAndEnterText(iii.deck_skill_ids[0, 0].ToString(), InfoCanvas.EnterMode.inMoment);
            Debug.Log("Loaded");
        }
        else
        {
            // 
            // 
            // 
            InfoCanvas.I.OpenFrameAndEnterText("Not Loaded", InfoCanvas.EnterMode.inMoment);
            Debug.Log("Not Loaded");
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            LoadPlayerInfo();
            if (iii != null)
            {
                // 
                // 
                // 
                InfoCanvas.I.OpenFrameAndEnterText(iii.deck_skill_ids[0, 0].ToString(), InfoCanvas.EnterMode.inMoment, false);
                Debug.Log("Loaded");
            }
            else
            {
                // 
                // 
                // 
                InfoCanvas.I.OpenFrameAndEnterText("Not Loaded", InfoCanvas.EnterMode.inMoment);
                InfoCanvas.I.EnterText(PlayerInfo.I.deck_skill_ids[0, 0].ToString(), InfoCanvas.EnterMode.inMoment, false);
                Debug.Log("Not Loaded");
            }
        }
    }
}
