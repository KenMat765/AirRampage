using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StationManager : MonoBehaviour
{
    Capsule stationCapsule;
    [SerializeField] Button skillPortButton, abilityPortButton;

    void Start()
    {
        stationCapsule = GetComponentInParent<Capsule>();
        stationCapsule.finish_open_action = () => { skillPortButton.interactable = true; abilityPortButton.interactable = true; };
        stationCapsule.start_close_action = () => { skillPortButton.interactable = false; abilityPortButton.interactable = false; };
        skillPortButton.interactable = false;
        abilityPortButton.interactable = false;
    }

    public void ActivateSkillPort()
    {
        skillPortButton.interactable = false;
        abilityPortButton.interactable = false;
        SceneManager2.I.LoadSceneAsync2(GameScenes.SKILLPORT, FadeType.gradually);
    }

    public void ActivateAbilityPort()
    {
        skillPortButton.interactable = false;
        abilityPortButton.interactable = false;
        SceneManager2.I.LoadSceneAsync2(GameScenes.ABILITYPORT, FadeType.gradually);
    }
}
