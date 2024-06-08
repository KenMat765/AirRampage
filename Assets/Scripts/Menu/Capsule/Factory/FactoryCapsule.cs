using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FactoryCapsule : MonoBehaviour
{
    Capsule factoryCapsule;
    [SerializeField] Button skillFactoryButton, abilityFactoryButton;

    void Start()
    {
        factoryCapsule = GetComponentInParent<Capsule>();
        factoryCapsule.finish_open_action = () => { skillFactoryButton.interactable = true; abilityFactoryButton.interactable = true; };
        factoryCapsule.start_close_action = () => { skillFactoryButton.interactable = false; abilityFactoryButton.interactable = false; };
        skillFactoryButton.interactable = false;
        abilityFactoryButton.interactable = false;
    }

    public void ActivateSkillFactory()
    {
        skillFactoryButton.interactable = false;
        abilityFactoryButton.interactable = false;
        SceneManager2.I.LoadSceneAsync2(GameScenes.SKILLFACTORY, FadeType.bottom);
    }

    public void ActivateAbilityFactory()
    {
        skillFactoryButton.interactable = false;
        abilityFactoryButton.interactable = false;
        SceneManager2.I.LoadSceneAsync2(GameScenes.ABILITYFACTORY, FadeType.bottom);
    }
}
