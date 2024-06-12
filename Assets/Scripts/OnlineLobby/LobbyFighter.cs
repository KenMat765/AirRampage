using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class LobbyFighter : Singleton<LobbyFighter>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    [SerializeField] GameObject[] fighters_red;
    [SerializeField] GameObject[] fighters_blue;
    GameObject[] afterburners_red;
    GameObject[] afterburners_blue;
    TextMeshProUGUI[] nameTexts_red;
    TextMeshProUGUI[] nameTexts_blue;
    const float prepareDuration = 0.3f, sortieDuration = 0.35f;
    const float interval = 0.1f;
    public bool listChanged { get; set; }

    void Start()
    {
        // Red fighters.
        afterburners_red = new GameObject[fighters_red.Length];
        nameTexts_red = new TextMeshProUGUI[fighters_red.Length];
        foreach (GameObject fighter_red in fighters_red)
        {
            fighter_red.transform.DOMoveZ(-5.5f, 0);
            if (fighter_red.activeSelf) fighter_red.SetActive(false);
            afterburners_red[Array.IndexOf(fighters_red, fighter_red)] = fighter_red.transform.Find("AfterBurners").gameObject;
            TextMeshProUGUI textMeshPro = fighter_red.transform.Find("Canvas/Text (TMP)").GetComponent<TextMeshProUGUI>();
            textMeshPro.text = "";
            nameTexts_red[Array.IndexOf(fighters_red, fighter_red)] = textMeshPro;
        }

        // Blue fighters.
        afterburners_blue = new GameObject[fighters_blue.Length];
        nameTexts_blue = new TextMeshProUGUI[fighters_blue.Length];
        foreach (GameObject fighter_blue in fighters_blue)
        {
            fighter_blue.transform.DOMoveZ(-5.5f, 0);
            if (fighter_blue.activeSelf) fighter_blue.SetActive(false);
            afterburners_blue[Array.IndexOf(fighters_blue, fighter_blue)] = fighter_blue.transform.Find("AfterBurners").gameObject;
            TextMeshProUGUI textMeshPro = fighter_blue.transform.Find("Canvas/Text (TMP)").GetComponent<TextMeshProUGUI>();
            textMeshPro.text = "";
            nameTexts_blue[Array.IndexOf(fighters_blue, fighter_blue)] = textMeshPro;
        }
    }

    public void PrepareFighter(Team team, int block_id)
    {
        switch (team)
        {
            case Team.RED:
                fighters_red[block_id].SetActive(true);
                fighters_red[block_id].transform.DOMoveZ(-0.5f, prepareDuration);
                break;

            case Team.BLUE:
                fighters_blue[block_id].SetActive(true);
                fighters_blue[block_id].transform.DOMoveZ(-0.5f, prepareDuration);
                break;

            default:
                Debug.LogError("Team was NONE!!");
                break;
        }
    }

    public void PrepareAllFighters(Team team, Action callback = null)
    {
        StartCoroutine(prepareAllFighters(team, callback));
    }

    IEnumerator prepareAllFighters(Team team, Action callback)
    {
        PrepareFighter(team, 0);
        for (int member_num = 1; member_num < GameInfo.team_member_count; member_num++)
        {
            yield return new WaitForSeconds(interval);
            PrepareFighter(team, member_num);
        }

        // Callback.
        if (callback != null)
        {
            callback();
        }
    }

    public void SortieFighter(Team team, int block_id)
    {
        switch (team)
        {
            case Team.RED:
                fighters_red[block_id].transform.DOMoveZ(8, sortieDuration);
                var particles_red = afterburners_red[block_id].GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem particle in particles_red) particle.Play();
                break;

            case Team.BLUE:
                fighters_blue[block_id].transform.DOMoveZ(8, sortieDuration);
                var particles_blue = afterburners_blue[block_id].GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem particle in particles_blue) particle.Play();
                break;

            default:
                Debug.LogError("Team was NONE!!");
                break;
        }
    }

    public void SortieAllFighters(Team team, Action callback = null)
    {
        StartCoroutine(sortieAllFighters(team, callback));
    }

    IEnumerator sortieAllFighters(Team team, Action callback)
    {
        SortieFighter(team, 0);
        for (int k = 1; k < GameInfo.team_member_count; k++)
        {
            yield return new WaitForSeconds(interval);
            SortieFighter(team, k);
        }

        // Callback.
        if (callback != null)
        {
            callback();
        }
    }

    public void DisableFighter(Team team, int block_id)
    {
        switch (team)
        {
            case Team.RED:
                fighters_red[block_id].transform.DOMoveZ(-5.5f, prepareDuration)
                    .OnComplete(() => fighters_red[block_id].SetActive(false));
                break;

            case Team.BLUE:
                fighters_blue[block_id].transform.DOMoveZ(-5.5f, prepareDuration)
                    .OnComplete(() => fighters_blue[block_id].SetActive(false));
                break;

            default:
                Debug.LogError("Team was NONE!!");
                break;
        }
    }

    public void DisableAllFighters(Team team, Action callback = null)
    {
        StartCoroutine(disableAllFighters(team, callback));
    }

    IEnumerator disableAllFighters(Team team, Action callback)
    {
        DisableFighter(team, 0);
        for (int k = 1; k < GameInfo.team_member_count; k++)
        {
            yield return new WaitForSeconds(interval);
            DisableFighter(team, k);
        }

        // Callback.
        if (callback != null)
        {
            callback();
        }
    }

    public void NameFighter(Team team, int block_id, string name)
    {
        switch (team)
        {
            case Team.RED:
                nameTexts_red[block_id].text = name;
                break;

            case Team.BLUE:
                nameTexts_blue[block_id].text = name;
                break;

            default:
                Debug.LogError("Team was NONE!!");
                break;
        }
    }
}
