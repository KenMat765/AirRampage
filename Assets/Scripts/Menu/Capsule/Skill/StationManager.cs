using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StationManager : MonoBehaviour
{
    Capsule stationCapsule;
    [SerializeField] Button skillPortButton, abilityBaseButton;

    void Start()
    {
        stationCapsule = GetComponentInParent<Capsule>();
        stationCapsule.finish_open_action = () => { skillPortButton.interactable = true; abilityBaseButton.interactable = true; };
        stationCapsule.start_close_action = () => { skillPortButton.interactable = false; abilityBaseButton.interactable = false; };
        skillPortButton.interactable = false;
        abilityBaseButton.interactable = false;
    }

    public void ActivateSkillPort()
    {
        skillPortButton.interactable = false;
        abilityBaseButton.interactable = false;
        SceneManager2.I.LoadSceneAsync2(GameScenes.skillport, FadeType.gradually);
    }

    public void ActivateAbilityBase()
    {
        skillPortButton.interactable = false;
        abilityBaseButton.interactable = false;
    }
}
