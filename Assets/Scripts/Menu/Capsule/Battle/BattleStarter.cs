using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleStarter : Utilities
{
    Capsule battleCapsule;
    [SerializeField] Button soloButton, multiButton;
    int selected_deck;

    void Start()
    {
        battleCapsule = GetComponentInParent<Capsule>();
        battleCapsule.finish_open_action = () => { soloButton.interactable = true; multiButton.interactable = true; };
        battleCapsule.start_close_action = () => { soloButton.interactable = false; multiButton.interactable = false; };
        soloButton.interactable = false;
        multiButton.interactable = false;
    }

    public void SoloBattle()
    {
        soloButton.interactable = false;
        multiButton.interactable = false;
        SortieLobbyUI.selectedMulti = false;
        LobbyFighter.selectedMulti = false;
        SceneManager2.I.LoadSceneAsync2(GameScenes.sortielobby, FadeType.left, FadeType.bottom);
    }

    public void MultiBattle()
    {
        soloButton.interactable = false;
        multiButton.interactable = false;
        SortieLobbyUI.selectedMulti = true;
        LobbyFighter.selectedMulti = true;
        SceneManager2.I.LoadSceneAsync2(GameScenes.onlinelobby, FadeType.bottom, FadeType.left);
    }
}