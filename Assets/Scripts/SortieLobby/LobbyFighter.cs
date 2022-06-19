using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Unity.Netcode;

public class LobbyFighter : Singleton<LobbyFighter>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    public GameObject[] fighters;
    GameObject[] afterburners;
    TextMeshProUGUI[] nameTexts;
    const float prepareDuration = 0.3f, sortieDuration = 0.35f;
    const float interval = 0.1f;
    public bool listChanged { get; set; }
    public static bool selectedMulti = true;



    void Start()
    {
        afterburners = new GameObject[fighters.Length];
        nameTexts = new TextMeshProUGUI[fighters.Length];
        foreach (GameObject fighter in fighters)
        {
            fighter.transform.DOMoveZ(-5.5f, 0);
            if (fighter.activeSelf) fighter.SetActive(false);
            afterburners[Array.IndexOf(fighters, fighter)] = fighter.transform.Find("AfterBurners").gameObject;
            TextMeshProUGUI textMeshPro = fighter.transform.Find("Canvas/Text (TMP)").GetComponent<TextMeshProUGUI>();
            textMeshPro.text = "";
            nameTexts[Array.IndexOf(fighters, fighter)] = textMeshPro;
        }

        if (!selectedMulti) return;
        RefreshFighterPreparation();
        LobbyLinkedData.I.AddOnValueChangedAction((NetworkListEvent<LobbyParticipantData> listEvent) =>
        {
            RefreshFighterPreparation();
        });
    }

    public void PrepareFighter(int index)
    {
        fighters[index].SetActive(true);
        fighters[index].transform.DOMoveZ(-0.5f, prepareDuration);
    }

    public void SortieFighter(int index)
    {
        fighters[index].transform.DOMoveZ(6, sortieDuration);
        var particles = afterburners[index].GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particle in particles) particle.Play();
    }

    public void DisableFighter(int index)
    {
        fighters[index].SetActive(false);
        fighters[index].transform.DOMoveZ(-5.5f, 0);
    }

    public void PrepareAllFighters(Team team, Action callback = null)
    {
        StartCoroutine(prepareAllFighters(team, callback));
    }

    IEnumerator prepareAllFighters(Team team, Action callback)
    {
        int[] nos = GameInfo.GetNosFromTeam(team);
        PrepareFighter(nos[0]);
        for (int k = 1; k < nos.Length; k++)
        {
            yield return new WaitForSeconds(interval);
            PrepareFighter(nos[k]);
        }
        if (callback != null) callback();
    }

    public void SortieAllFighters(Team team, Action callback = null)
    {
        StartCoroutine(sortieAllFighters(team, callback));
    }

    IEnumerator sortieAllFighters(Team team, Action callback)
    {
        int[] nos = GameInfo.GetNosFromTeam(team);
        SortieFighter(nos[0]);
        for (int k = 1; k < nos.Length; k++)
        {
            yield return new WaitForSeconds(interval);
            SortieFighter(nos[k]);
        }
        if (callback != null) callback();
    }

    public void RefreshFighterPreparation()
    {
        LobbyParticipantData myData = LobbyLinkedData.I.GetParticipantDataByClientId(NetworkManager.Singleton.LocalClientId).Value;
        Team myTeam = myData.team;
        int[] teamNos = GameInfo.GetNosFromTeam(myTeam);
        List<int> preparedNos = new List<int>();
        foreach (LobbyParticipantData data in LobbyLinkedData.I.participantDatas)
        {
            if (data.team != myTeam) continue;
            preparedNos.Add(data.number);
            if (fighters[data.number].activeSelf) continue;
            PrepareFighter(data.number);
            nameTexts[data.number].text = data.name.ToString();
        }
        foreach (int teamNo in teamNos)
        {
            if (preparedNos.Contains(teamNo)) continue;
            DisableFighter(teamNo);
        }
    }

    public void NameFighter(int fighterNo, string name)
    {
        nameTexts[fighterNo].text = name;
    }
}
