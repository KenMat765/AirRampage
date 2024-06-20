using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsCapsule : MonoBehaviour
{
    Capsule settingsCapsule;
    [SerializeField] Toggle postprocessToggle;
    [SerializeField] Toggle fps30Toggle, fps60Toggle;
    [SerializeField] Slider bgmSlider, volumeSlider;

    void Start()
    {
        settingsCapsule = GetComponentInParent<Capsule>();
        settingsCapsule.finish_open_action = () =>
        {
            postprocessToggle.interactable = true;
            if (PlayerInfo.I.fps == 30)
            {
                fps30Toggle.interactable = false;
                fps60Toggle.interactable = true;
            }
            else
            {
                fps30Toggle.interactable = true;
                fps60Toggle.interactable = false;
            }
            bgmSlider.interactable = true;
            volumeSlider.interactable = true;
        };
        settingsCapsule.start_close_action = () =>
        {
            postprocessToggle.interactable = false;
            fps30Toggle.interactable = false;
            fps60Toggle.interactable = false;
            bgmSlider.interactable = false;
            volumeSlider.interactable = false;
            SaveManager.SaveData<PlayerInfo>(PlayerInfo.I);
        };

        // Set all toggles & sliders uninteractable
        postprocessToggle.interactable = false;
        fps30Toggle.interactable = false;
        fps60Toggle.interactable = false;
        bgmSlider.interactable = false;
        volumeSlider.interactable = false;

        // Initialize toggles & sliders according to PlayerInfo
        postprocessToggle.isOn = PlayerInfo.I.postprocess;
        if (PlayerInfo.I.fps == 30)
        {
            fps30Toggle.isOn = true;
            fps60Toggle.isOn = false;
        }
        else
        {
            fps30Toggle.isOn = false;
            fps60Toggle.isOn = true;
        }
        bgmSlider.value = PlayerInfo.I.bgmRatio;
        volumeSlider.value = PlayerInfo.I.volume;
    }

    public void OnPostprocessToggle()
    {
        PlayerInfo.I.postprocess = postprocessToggle.isOn;
    }

    public void On30FPSToggle()
    {
        if (fps30Toggle.isOn)
        {
            int fps = 30;
            PlayerInfo.I.fps = fps;
            AppSetup.SetFPS(fps);
            fps30Toggle.interactable = false;
            fps60Toggle.interactable = true;
            fps60Toggle.isOn = false;
        }
    }

    public void On60FPSToggle()
    {
        if (fps60Toggle.isOn)
        {
            int fps = 60;
            PlayerInfo.I.fps = fps;
            AppSetup.SetFPS(fps);
            fps60Toggle.interactable = false;
            fps30Toggle.interactable = true;
            fps30Toggle.isOn = false;
        }
    }

    public void OnVolumeSlider()
    {
        float volume = volumeSlider.value;
        PlayerInfo.I.volume = volume;
        AppSetup.SetVolume(volume);
    }

    public void OnBGMSlider()
    {
        float bgm = bgmSlider.value;
        PlayerInfo.I.bgmRatio = bgm;
        AppSetup.SetBGM(bgm);
    }
}
