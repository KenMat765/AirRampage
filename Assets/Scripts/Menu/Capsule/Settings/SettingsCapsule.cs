using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Originals.UIExtensions;

public class SettingsCapsule : MonoBehaviour
{
    Capsule settingsCapsule;

    [SerializeField] Transform generalRect;
    [SerializeField] Transform graphicsRect;
    [SerializeField] Transform audioRect;

    // General
    TMP_InputField nameInputField;
    Toggle cameraFToggle, cameraT1Toggle, cameraT2Toggle;
    RadioButton cameraRadio;
    Toggle invertYAxisToggle;

    // Graphics
    Toggle fps30Toggle, fps60Toggle;
    RadioButton fpsRadio;
    Toggle postprocessToggle;

    // Audio
    Slider bgmSlider;
    Slider seSlider;


    void Start()
    {
        settingsCapsule = GetComponentInParent<Capsule>();
        settingsCapsule.finish_open_action = () =>
        {
            SetInteractableAll(true);
        };
        settingsCapsule.start_close_action = () =>
        {
            SetInteractableAll(false);
            SaveManager.SaveData<PlayerInfo>(PlayerInfo.I);
        };


        // Get Components.
        // General
        nameInputField = generalRect.Find("Name/Name_InputField").GetComponent<TMP_InputField>();
        cameraFToggle = generalRect.Find("Camera/FToggle").GetComponent<Toggle>();
        cameraT1Toggle = generalRect.Find("Camera/T1Toggle").GetComponent<Toggle>();
        cameraT2Toggle = generalRect.Find("Camera/T2Toggle").GetComponent<Toggle>();
        cameraRadio = new RadioButton(cameraFToggle, cameraT1Toggle, cameraT2Toggle);
        invertYAxisToggle = generalRect.Find("InvertYAxis/Toggle").GetComponent<Toggle>();
        // Graphics
        fps30Toggle = graphicsRect.Find("FPS/30Toggle").GetComponent<Toggle>();
        fps60Toggle = graphicsRect.Find("FPS/60Toggle").GetComponent<Toggle>();
        fpsRadio = new RadioButton(fps30Toggle, fps60Toggle);
        postprocessToggle = graphicsRect.Find("Postprocess/Toggle").GetComponent<Toggle>();
        // Audio
        bgmSlider = audioRect.Find("BGM/BGM_Slider").GetComponent<Slider>();
        seSlider = audioRect.Find("SE/SE_Slider").GetComponent<Slider>();


        AdjustToPlayerInfo();
        SetInteractableAll(false);
        SwitchCategory(SettingsCategory.GENERAL);
    }



    // ===== Utilities ===== //
    void AdjustToPlayerInfo()
    {
        // General
        nameInputField.text = PlayerInfo.I.myName;
        switch (PlayerInfo.I.viewType)
        {
            case ViewType.FPS: cameraRadio.SwtichToggle(cameraFToggle, true); break;
            case ViewType.TPS_NEAR: cameraRadio.SwtichToggle(cameraT1Toggle, true); break;
            case ViewType.TPS_FAR: cameraRadio.SwtichToggle(cameraT2Toggle, true); break;
        }
        invertYAxisToggle.isOn = PlayerInfo.I.invertYAxis;

        // Graphics
        if (PlayerInfo.I.fps == 30)
            fpsRadio.SwtichToggle(fps30Toggle, true);
        else
            fpsRadio.SwtichToggle(fps60Toggle, true);
        postprocessToggle.isOn = PlayerInfo.I.postprocess;

        // Audio
        bgmSlider.value = PlayerInfo.I.bgmRatio;
        seSlider.value = PlayerInfo.I.seRatio;
    }

    void SetInteractableAll(bool interactable)
    {
        nameInputField.interactable = interactable;
        cameraRadio.Interactable(interactable);
        invertYAxisToggle.interactable = interactable;
        fpsRadio.Interactable(interactable);
        postprocessToggle.interactable = interactable;
        bgmSlider.interactable = interactable;
        seSlider.interactable = interactable;
    }



    // ===== Categories ===== //
    public enum SettingsCategory
    {
        GENERAL, GRAPHICS, AUDIO, NONE
    }

    public void OnCategorySelected(int category_id)
    {
        SettingsCategory category = (SettingsCategory)category_id;
        SwitchCategory(category);
    }

    void SwitchCategory(SettingsCategory category)
    {
        switch (category)
        {
            case SettingsCategory.GENERAL:
                generalRect.gameObject.SetActive(true);
                graphicsRect.gameObject.SetActive(false);
                audioRect.gameObject.SetActive(false);
                break;

            case SettingsCategory.GRAPHICS:
                generalRect.gameObject.SetActive(false);
                graphicsRect.gameObject.SetActive(true);
                audioRect.gameObject.SetActive(false);
                break;

            case SettingsCategory.AUDIO:
                generalRect.gameObject.SetActive(false);
                graphicsRect.gameObject.SetActive(false);
                audioRect.gameObject.SetActive(true);
                break;

            case SettingsCategory.NONE:
                generalRect.gameObject.SetActive(false);
                graphicsRect.gameObject.SetActive(false);
                audioRect.gameObject.SetActive(false);
                break;
        }
    }



    // ===== General ===== //
    public void OnNameInputEnd()
    {
        PlayerInfo.I.myName = nameInputField.text;
    }

    public void OnCameraToggle(int viewType_id)
    {
        ViewType viewType = (ViewType)viewType_id;
        switch (viewType)
        {
            case ViewType.FPS:
                if (cameraFToggle.isOn)
                {
                    cameraRadio.SwtichToggle(cameraFToggle);
                    PlayerInfo.I.viewType = ViewType.FPS;
                }
                break;

            case ViewType.TPS_NEAR:
                if (cameraT1Toggle.isOn)
                {
                    cameraRadio.SwtichToggle(cameraT1Toggle);
                    PlayerInfo.I.viewType = ViewType.TPS_NEAR;
                }
                break;

            case ViewType.TPS_FAR:
                if (cameraT2Toggle.isOn)
                {
                    cameraRadio.SwtichToggle(cameraT2Toggle);
                    PlayerInfo.I.viewType = ViewType.TPS_FAR;
                }
                break;
        }
    }

    public void OnInvertYAxisToggle()
    {
        PlayerInfo.I.invertYAxis = invertYAxisToggle.isOn;
    }



    // ===== Graphics ===== //
    public void OnFPSToggle(int fps_id)
    {
        int fps = 30;
        switch (fps_id)
        {
            case 0:
                if (fps30Toggle.isOn)
                {
                    fps = 30;
                    fpsRadio.SwtichToggle(fps30Toggle);
                }
                break;

            case 1:
                if (fps60Toggle.isOn)
                {
                    fps = 60;
                    fpsRadio.SwtichToggle(fps60Toggle);
                }
                break;

            default:
                Debug.LogError("FPS toggle argument out of range: Please select from 0~1", gameObject);
                return;
        }
        PlayerInfo.I.fps = fps;
        AppSetup.SetFPS(fps);
    }

    public void OnPostprocessToggle()
    {
        PlayerInfo.I.postprocess = postprocessToggle.isOn;
    }



    // ===== Audio ===== //
    public void OnSESlider()
    {
        float se = seSlider.value;
        PlayerInfo.I.seRatio = se;
        AppSetup.SetSE(se);
    }

    public void OnBGMSlider()
    {
        float bgm = bgmSlider.value;
        PlayerInfo.I.bgmRatio = bgm;
        AppSetup.SetBGM(bgm);
    }
}
