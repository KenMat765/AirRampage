using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SortieLobbyUI : Singleton<SortieLobbyUI>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    const float tweenDuration = 0.1f, tweenInterval = 0.8f;
    GameObject settingsObj, participantObj;
    RectTransform menuRect, returnRect;
    Button returnButton, confirmButton;
    TextMeshProUGUI titleText, confirmText;
    TextMeshProUGUI ruleText, stageText, timeText;
    public enum Page { SETTINGS, PARTICIPANT }
    public static Page page { get; private set; }
    bool pageChanged = false;
    public void SetPage(Page next)
    {
        page = next;
        pageChanged = true;
    }

    // Skill deck number selected.
    int myDeckNum
    {
        get { return SortieLobbyManager.I.myDeckNum; }
        set { SortieLobbyManager.I.myDeckNum = value; }
    }


    void Start()
    {
        // Get components.
        menuRect = transform.Find("Menu").GetComponent<RectTransform>();
        returnRect = transform.Find("Return").GetComponent<RectTransform>();
        returnButton = returnRect.GetComponent<Button>();
        titleText = menuRect.Find("Title").GetComponent<TextMeshProUGUI>();
        settingsObj = menuRect.Find("GameSettings").gameObject;
        participantObj = menuRect.Find("Participant").gameObject;
        confirmButton = menuRect.transform.Find("ConfirmButton").GetComponent<Button>();
        confirmText = menuRect.transform.Find("ConfirmButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        ruleText = settingsObj.transform.Find("Rule/Text (TMP)").GetComponent<TextMeshProUGUI>();
        stageText = settingsObj.transform.Find("Stage/Text (TMP)").GetComponent<TextMeshProUGUI>();
        timeText = settingsObj.transform.Find("Time/Text (TMP)").GetComponent<TextMeshProUGUI>();

        // Initialize components.
        menuRect.DOScaleY(0, 0);
        returnRect.DOAnchorPosX(-100, 0);
        returnButton.onClick.AddListener(ExitLobby);
        returnButton.interactable = false;
        titleText.FadeColor(0);
        settingsObj.SetActive(false);
        participantObj.SetActive(false);
        InitSettings();

        // Menu & Return_Button UI animation.
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(0.5f);
        seq.Append(EnterMenuReturn());
        if (BattleInfo.isHost)
        {
            seq.AppendCallback(() => SetPage(Page.SETTINGS));
        }
        else
        {
            seq.AppendCallback(() => SetPage(Page.PARTICIPANT));
        }
        LobbyLinkedData.I.AddOnValueChangedAction((NetworkListEvent<LobbyParticipantData> listEvent) =>
        {
            if (page == Page.PARTICIPANT)
            {
                RefreshAllParticipantsSkill();
                RefreshAllParticipantsNames();
            }
        });
        seq.AppendCallback(() => LobbyLinkedData.I.acceptDataChange = true);
        seq.Play();
    }



    void Update()
    {
        if (!pageChanged) return;
        pageChanged = false;

        string title = "";
        Sequence sequence = DOTween.Sequence();
        switch (page)
        {
            case Page.SETTINGS:
                title = "Rules";
                confirmButton.interactable = true;
                confirmText.text = "Confirm";

                sequence.Join(settingsObj.transform.DOScaleX(0, 0).OnComplete(() => { settingsObj.SetActive(true); returnButton.interactable = false; }));
                if (participantObj.activeSelf)
                {
                    sequence.Join(titleText.DOFade(0, tweenDuration));
                    sequence.Join(participantObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => participantObj.SetActive(false)));
                    sequence.AppendInterval(tweenInterval);
                }
                sequence.Append(settingsObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(ExitLobby);
                break;

            case Page.PARTICIPANT:
                title = "Players";
                confirmButton.interactable = true;
                confirmText.text = "Ready For Battle";

                sequence.Join(participantObj.transform.DOScaleX(0, 0).OnComplete(() => { participantObj.SetActive(true); returnButton.interactable = false; }));
                if (settingsObj.activeSelf)
                {
                    sequence.Join(titleText.DOFade(0, tweenDuration));
                    sequence.Join(settingsObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => settingsObj.SetActive(false)));
                    sequence.AppendInterval(tweenInterval);
                }
                sequence.Append(participantObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                if (BattleInfo.isHost)
                {
                    returnButton.onClick.AddListener(() =>
                    {
                        SetPage(Page.SETTINGS);
                    });
                }
                else
                {
                    returnButton.onClick.AddListener(() =>
                    {
                        ExitLobby();
                    });
                }
                RefreshAllParticipantsSkill();
                RefreshAllParticipantsNames();
                break;
        }
        sequence.Join(titleText.DOFade(0.7f, tweenDuration).OnStart(() => titleText.text = title));
        sequence.Play();
    }



    // Menu & Return Button ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    public Sequence EnterMenuReturn()
    {
        var seq = DOTween.Sequence();
        seq.Join(menuRect.DOScaleY(1, tweenDuration));
        seq.Join(returnRect.DOAnchorPosX(100, tweenDuration));
        seq.AppendCallback(() => returnButton.interactable = true);
        return seq;
    }

    public Sequence ExitMenuReturn()
    {
        var seq = DOTween.Sequence();
        seq.AppendCallback(() => returnButton.interactable = false);
        seq.Join(menuRect.DOScaleY(0, tweenDuration));
        seq.Join(returnRect.DOAnchorPosX(-100, tweenDuration));
        return seq;
    }



    // Settings ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region settings
    void InitSettings()
    {
        // Set default value.
        rule = Rule.BATTLEROYAL;
        stage = Stage.SPACE;
        time_sec = GameInfo.min_time_sec;

        // Change UI.
        ruleText.text = rule.ToString();
        stageText.text = stage.ToString();
        int time_min = time_sec / 60;
        timeText.text = time_min.ToString() + " minutes";
    }
    void DetermineSettings()
    {
        BattleInfo.rule = rule;
        BattleInfo.stage = stage;
        BattleInfo.time_sec = time_sec;
    }

    // Rule ///////////////////////////////////////////////////////////////////////////////
    Rule rule;
    public void OnRuleArrowPressed(int delta_id)
    {
        // Change stage of BattleInfo
        int rule_id = (int)rule;
        int rule_count = Enum.GetNames(typeof(Rule)).Length;
        rule_id += delta_id;
        if (rule_id < 0) rule_id += rule_count;
        else if (rule_count <= rule_id) rule_id -= rule_count;
        rule = (Rule)rule_id;

        // Change UI
        ruleText.text = rule.ToString();
    }

    // Stage //////////////////////////////////////////////////////////////////////////////
    Stage stage;
    public void OnStageArrowPressed(int delta_id)
    {
        // Change stage of BattleInfo
        int stage_id = (int)stage;
        int stage_count = Enum.GetNames(typeof(Stage)).Length;
        stage_id += delta_id;
        if (stage_id < 0) stage_id += stage_count;
        else if (stage_count <= stage_id) stage_id -= stage_count;
        stage = (Stage)stage_id;

        // Change UI
        stageText.text = stage.ToString();
    }

    // Time ///////////////////////////////////////////////////////////////////////////////
    int time_sec;
    public void OnTimeArrowPressed(int delta_sec)
    {
        // Change stage of BattleInfo
        time_sec += delta_sec;
        if (GameInfo.max_time_sec < time_sec)
        {
            time_sec = GameInfo.min_time_sec;
        }
        else if (time_sec < GameInfo.min_time_sec)
        {
            time_sec = GameInfo.max_time_sec;
        }

        // Change UI
        int time_min = time_sec / 60;
        timeText.text = time_min.ToString() + " minutes";
    }
    #endregion
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



    // Participant ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region participant page
    [System.Serializable]
    struct SkillUI
    {
        public Image frame;
        public Image icon;
    }

    [System.Serializable]
    struct SkillDeck
    {
        public SkillUI[] skills;
    }

    [SerializeField] SkillDeck[] skillDecks;
    [SerializeField] GameObject[] arrows;
    [SerializeField] TextMeshProUGUI[] names;
    bool isReady = false;

    public void SkillUISetter(int block, int?[] skill_ids)
    {
        for (int k = 0; k < GameInfo.max_skill_count; k++)
        {
            int? skill_id = skill_ids[k];
            if (skill_id.HasValue)
            {
                SkillData skill_data = SkillDatabase.I.SearchSkillById((int)skill_id);
                skillDecks[block].skills[k].icon.sprite = skill_data.GetSprite();
                skillDecks[block].skills[k].icon.color = Color.white;
                skillDecks[block].skills[k].frame.color = skill_data.GetColor();
            }
            else
            {
                skillDecks[block].skills[k].icon.sprite = null;
                skillDecks[block].skills[k].icon.color = Color.clear;
                skillDecks[block].skills[k].frame.color = Color.white;
            }
        }
    }

    public void SkillArrow(int direction)
    {
        if (isReady)
        {
            return;
        }

        myDeckNum += direction;
        if (myDeckNum < 0) myDeckNum += GameInfo.deck_count;
        else if (GameInfo.deck_count <= myDeckNum) myDeckNum -= GameInfo.deck_count;

        // Get skill Ids & skill levels.
        int?[] new_skillIds, new_skillLevels;
        PlayerInfo.I.SkillIdsGetter(myDeckNum, out new_skillIds);
        PlayerInfo.I.SkillLevelsGetter(myDeckNum, out new_skillLevels);

        // Encode skill informations.
        string new_skillCode;
        LobbyParticipantData.SkillCodeEncoder(new_skillIds, new_skillLevels, out new_skillCode);

        // Set new lobby data to LobbyLinkedData.
        LobbyLinkedData.I.RequestServerModifyParticipantData(NetworkManager.Singleton.LocalClientId, skillCode: new_skillCode);

        // LinkedDataの値が更新されるので、自動的にRefreshPlayerSkillIconsが呼ばれる
    }

    public void NameUISetter(int blockId, string name)
    {
        names[blockId].text = name;
    }

    // Updates all participants skills.
    // Automaticaly called when participant data changed. (Called on every participants)
    void RefreshAllParticipantsSkill()
    {
        Team myTeam = SortieLobbyManager.I.myData.team;
        for (int block_id = 0; block_id < skillDecks.Length; block_id++)
        {
            int fighterNo = -1;
            if (!LobbyLinkedData.I.TryGetNumber(myTeam, block_id, ref fighterNo))
            {
                Debug.LogError($"Could not get fighter number!! team : {myTeam}, block : {block_id}");
                return;
            }

            LobbyParticipantData? data_nullable;
            data_nullable = LobbyLinkedData.I.GetParticipantDataByNumber(fighterNo);
            int?[] skillIds, skillLevels;

            if (data_nullable.HasValue)
            {
                LobbyParticipantData data = data_nullable.Value;
                LobbyParticipantData.SkillCodeDecoder(data.skillCode.ToString(), out skillIds, out skillLevels);
                SkillUISetter(block_id, skillIds);
            }
            else
            {
                skillIds = new int?[GameInfo.max_skill_count];
                for (int k = 0; k < skillIds.Length; k++)
                {
                    skillIds[k] = null;
                }
                SkillUISetter(block_id, skillIds);
            }
        }
    }

    // Updates all participants names.
    // Automaticaly called when participant data changed. (Called on every participants)
    void RefreshAllParticipantsNames()
    {
        Team myTeam = SortieLobbyManager.I.myData.team;
        for (int blockNo = 0; blockNo < GameInfo.team_member_count; blockNo++)
        {
            // Get fighter number from team and block number.
            int fighterNo = -1;
            if (!LobbyLinkedData.I.TryGetNumber(myTeam, blockNo, ref fighterNo))
            {
                return;
            }

            // Get participant data from fighter number.
            LobbyParticipantData? nullable_data = LobbyLinkedData.I.GetParticipantDataByNumber(fighterNo);

            // Participant exists.
            if (nullable_data.HasValue)
            {
                LobbyParticipantData lobby_data = (LobbyParticipantData)nullable_data;
                NameUISetter(blockNo, lobby_data.name.Value);

                // My block Id
                int myNumber = SortieLobbyManager.I.myData.number;
                if (lobby_data.number == myNumber)
                {
                    if (!arrows[blockNo].activeSelf)
                    {
                        arrows[blockNo].SetActive(true);
                    }
                }

                // Not my block Id
                else
                {
                    if (arrows[blockNo].activeSelf)
                    {
                        arrows[blockNo].SetActive(false);
                    }
                }
            }

            // No participant.
            else
            {
                NameUISetter(blockNo, "");
            }
        }
    }
    #endregion



    // Confirm ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public void OnConfirmButton()
    {
        switch (page)
        {
            case Page.SETTINGS:
                DetermineSettings();
                SetPage(Page.PARTICIPANT);
                break;

            case Page.PARTICIPANT:
                ReadyForBattle();
                break;
        }
    }

    void ReadyForBattle()
    {
        isReady = !isReady;
        LobbyLinkedData.I.RequestServerModifyParticipantData(NetworkManager.Singleton.LocalClientId, isReady: isReady);
        if (isReady)
        {
            confirmText.text = "Change Skill Deck";
            returnButton.interactable = false;
        }
        else
        {
            confirmText.text = "Ready For Battle";
            returnButton.interactable = true;
        }
    }
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



    void ExitLobby()
    {
        returnButton.interactable = false;
        returnRect.DOAnchorPosX(-100, tweenDuration);
        NetworkManager.Singleton.Shutdown();
        if (BattleInfo.isHost)
        {
            settingsObj.transform.DOScaleX(0, tweenDuration);
        }
        else
        {
            participantObj.transform.DOScaleX(0, tweenDuration);
        }
        DOVirtual.DelayedCall(tweenDuration, () =>
        {
            menuRect.DOScaleY(0, tweenDuration).OnComplete(() => SceneManager2.I.LoadScene2(GameScenes.ONLINELOBBY));
        }).Play();
    }
}