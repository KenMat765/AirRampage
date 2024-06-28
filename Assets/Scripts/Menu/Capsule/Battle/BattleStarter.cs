using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleStarter : Utilities
{
    Capsule battleCapsule;
    [SerializeField] Button practiceButton, matchButton;

    void Start()
    {
        battleCapsule = GetComponentInParent<Capsule>();
        battleCapsule.finish_open_action = () => { /* practiceButton.interactable = true; */ matchButton.interactable = true; };
        battleCapsule.start_close_action = () => { practiceButton.interactable = false; matchButton.interactable = false; };
        practiceButton.interactable = false;
        matchButton.interactable = false;
    }

    public void SoloBattle()
    {
        practiceButton.interactable = false;
        matchButton.interactable = false;
        BattleInfo.isMulti = false;
        SceneManager2.I.LoadSceneAsync2(GameScenes.SORTIELOBBY, FadeType.left);
    }

    public void MultiBattle()
    {
        practiceButton.interactable = false;
        matchButton.interactable = false;
        BattleInfo.isMulti = true;
        SceneManager2.I.LoadSceneAsync2(GameScenes.ONLINELOBBY, FadeType.bottom);
    }
}