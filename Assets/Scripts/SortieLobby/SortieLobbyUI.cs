using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SortieLobbyUI : Singleton<SortieLobbyUI>
{
    protected override bool dont_destroy_on_load {get; set;} = false;
    public static bool selectedMulti=true, selectedHost;
    const float tweenDuration = 0.1f, tweenInterval = 0.8f;
    GameObject stageObject, ruleObject, participantObject;
    RectTransform menuRect, returnRect;
    Button returnButton, readyForBattleButton;
    Text titleText, readyForBattleText;
    enum Page { stage, rule, participant }
    Page page;
    bool pageChanged = false;
    void SetPage(Page next)
    {
        page = next;
        pageChanged = true;
    }
    ulong myClientId;
    int myNumber;
    string myName;
    Team myTeam;



    void Start()
    {
        menuRect = transform.Find("Menu").GetComponent<RectTransform>();
        returnRect = transform.Find("Return").GetComponent<RectTransform>();
        returnButton = returnRect.GetComponent<Button>();
        stageObject = menuRect.Find("Stage").gameObject;
        ruleObject = menuRect.Find("Rule").gameObject;
        titleText = menuRect.Find("Title").GetComponent<Text>();
        participantObject = menuRect.Find("Participant").gameObject;
        readyForBattleButton = participantObject.transform.Find("ReadyButton").GetComponent<Button>();
        readyForBattleText = participantObject.transform.Find("ReadyButton/Text").GetComponent<Text>();

        menuRect.DOScaleY(0, 0);
        returnRect.DOAnchorPosX(-100, 0);
        returnButton.onClick.AddListener(ExitLobby);
        returnButton.interactable = false;

        titleText.DOFade(0, 0);

        stageObject.SetActive(false);
        ruleObject.SetActive(false);
        participantObject.SetActive(false);

        readyForBattleText.text = "Ready For Battle";

        BattleInfo.isMulti = selectedMulti;
        BattleInfo.isHost = selectedHost;
        
        // Multi //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        if(selectedMulti)
        {
            // 不変の値を取得
            myClientId = NetworkManager.Singleton.LocalClientId;
            LobbyParticipantData myData = (LobbyParticipantData)LobbyLinkedData.I.GetParticipantDataByClientId(NetworkManager.Singleton.LocalClientId);
            myNumber = myData.number;
            myName = myData.name.ToString();
            myTeam = myData.team;

            if(selectedHost)
            {
                titleText.text = "Stage";
                stageObject.transform.DOScaleX(0, 0);
                stageObject.SetActive(true);
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    menuRect.DOScaleY(1, tweenDuration)
                        .OnComplete(() =>
                        {
                            stageObject.transform.DOScaleX(1, tweenDuration);
                            titleText.DOFade(0.7f, tweenDuration);
                            page = Page.stage;
                        });
                    returnRect.DOAnchorPosX(100, tweenDuration)
                        .OnComplete(() => returnButton.interactable = true);
                }).Play();
            }
            else
            {
                titleText.text = "Players";
                participantObject.transform.DOScaleX(0, 0);
                participantObject.SetActive(true);
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    menuRect.DOScaleY(1, tweenDuration)
                        .OnComplete(() =>
                        {
                            participantObject.transform.DOScaleX(1, tweenDuration);
                            titleText.DOFade(0.7f, tweenDuration);
                            page = Page.participant;
                        });
                    returnRect.DOAnchorPosX(100, tweenDuration)
                        .OnComplete(() => returnButton.interactable = true);
                }).Play();
                RefreshPlayerSkillIcons();
                RefreshNames();
            }

            LobbyLinkedData.I.AddOnValueChangedAction((NetworkListEvent<LobbyParticipantData> listEvent) =>
            {
                RefreshPlayerSkillIcons();
                RefreshNames();
            });
        }

        // Solo ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        else
        {
            // UI & Fighter Preparation //////////////////////////////////////////////////////////////////////////////
            stageObject.transform.DOScaleX(0, 0);
            stageObject.SetActive(true);
            titleText.text = "Stage";
            DOVirtual.DelayedCall(0.5f, () =>
            {
                menuRect.DOScaleY(1, tweenDuration)
                    .OnComplete(() =>
                    {
                        stageObject.transform.DOScaleX(1, tweenDuration);
                        titleText.DOFade(0.7f, tweenDuration);
                        page = Page.stage;
                    });
                returnRect.DOAnchorPosX(100, tweenDuration)
                    .OnComplete(() => returnButton.interactable = true);
                LobbyFighter.I.PrepareAllFighters(Team.Red);
            }).Play();


            // AI Setups //////////////////////////////////////////////////////////////////////////////////////////////
            // Red AIs.
            int redAiCount = 3;
            for(int red = 0; red < redAiCount; red++)
            {
                // Generate skills.
                int?[] aiSkillIds, aiSkillLevels;
                LobbyAiSkillGenerator.I.GenerateSkills(out aiSkillIds, out aiSkillLevels);

                // Show skills on UI.
                for(int iconNo = 0; iconNo < GameInfo.max_skill_count; iconNo ++)
                {
                    int? skillId_nullable = aiSkillIds[iconNo];
                    if(skillId_nullable.HasValue)
                    {
                        int skillId = (int)skillId_nullable;
                        SkillData data = SkillDatabase.I.SearchSkillById(skillId);
                        SkillUISetter(1 + red, iconNo, data.GetSprite(), Color.white, data.GetColor());
                    }
                    else
                    {
                        SkillUISetter(1 + red, iconNo, null, Color.clear, Color.white);
                    }
                }

                // Generate & Send battle data to BattleInfo.
                int aiNo = GameInfo.GetNoFromTeam(Team.Red, 1 + red);
                string aiName = "AIRed" + (red + 1).ToString();
                BattleInfo.ParticipantBattleData battleData = new BattleInfo.ParticipantBattleData(aiNo, false, null, aiName, Team.Blue, aiSkillIds, aiSkillLevels);
                BattleInfo.battleDatas[aiNo] = battleData;
            }

            // Blue AIs.
            int blueAiCount = 4;
            for(int blue = 0; blue < blueAiCount; blue++)
            {
                // Generate skills.
                int?[] aiSkillIds, aiSkillLevels;
                LobbyAiSkillGenerator.I.GenerateSkills(out aiSkillIds, out aiSkillLevels);

                // Generate & Send battle data to BattleInfo.
                int aiNo = GameInfo.GetNoFromTeam(Team.Blue, blue);
                string aiName = "AIBlue" + (blue + 1).ToString();
                BattleInfo.ParticipantBattleData battleData = new BattleInfo.ParticipantBattleData(aiNo, false, null, aiName, Team.Blue, aiSkillIds, aiSkillLevels);
                BattleInfo.battleDatas[aiNo] = battleData;
            }
        }
    }



    void Update()
    {
        if(!pageChanged) return;

        Sequence sequence = DOTween.Sequence();
        switch(page)
        {
            case Page.stage:
                if (ruleObject.activeSelf)
                {
                    sequence.Append(titleText.DOFade(0, tweenDuration).OnComplete(() => titleText.text = "Stage"));
                    sequence.Join(ruleObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => ruleObject.SetActive(false)));
                    sequence.Join(stageObject.transform.DOScaleX(0, 0).OnComplete(() => { stageObject.SetActive(true); returnButton.interactable = false; }));
                    sequence.AppendInterval(tweenInterval);
                    sequence.Append(stageObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                }

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(ExitLobby);
                break;

            case Page.rule:
                if (stageObject.activeSelf)
                {
                    sequence.Append(titleText.DOFade(0, tweenDuration).OnComplete(() => titleText.text = "Rule"));
                    sequence.Join(stageObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => stageObject.SetActive(false)));
                    sequence.Join(ruleObject.transform.DOScaleX(0, 0).OnComplete(() => { ruleObject.SetActive(true); returnButton.interactable = false; }));
                    sequence.AppendInterval(tweenInterval);
                    sequence.Append(ruleObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                }

                else if (participantObject.activeSelf)
                {
                    sequence.Append(titleText.DOFade(0, tweenDuration).OnComplete(() => titleText.text = "Rule"));
                    sequence.Join(participantObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => participantObject.SetActive(false)));
                    sequence.Join(ruleObject.transform.DOScaleX(0, 0).OnComplete(() => { ruleObject.SetActive(true); returnButton.interactable = false; }));
                    sequence.AppendInterval(tweenInterval);
                    sequence.Append(ruleObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                }

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() => SetPage(Page.stage));
                break;



            case Page.participant:
                if (ruleObject.activeSelf)
                {
                    sequence.Append(titleText.DOFade(0, tweenDuration).OnComplete(() => titleText.text = "Players"));
                    sequence.Join(ruleObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => ruleObject.SetActive(false)));
                    sequence.Join(participantObject.transform.DOScaleX(0, 0).OnComplete(() => { participantObject.SetActive(true); returnButton.interactable = false; }));
                    sequence.AppendInterval(tweenInterval);
                    sequence.Append(participantObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                }

                returnButton.onClick.RemoveAllListeners();
                if (selectedMulti)
                {
                    if(selectedHost)
                    {
                        returnButton.onClick.AddListener(() =>
                        {
                            SetPage(Page.rule);
                        });
                    }
                    else
                    {
                        returnButton.onClick.AddListener(() =>
                        {
                            ExitLobby();
                        });
                    }
                    RefreshNames();
                }
                else
                {
                    returnButton.onClick.AddListener(() => SetPage(Page.rule));
                }
                RefreshPlayerSkillIcons();
                break;
        }

        sequence.Join(titleText.DOFade(0.7f, tweenDuration));
        sequence.Play();

        pageChanged = false;
    }



    // Stage //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Stage(int stage_id)
    {
        switch(stage_id)
        {
            case 0:
            break;

            case 1:
            break;

            case 2:
            break;

            case 3:
            break;
        }
        SetPage(Page.rule);
    }
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



    // Rule ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Rule(int rule_id)
    {
        switch(rule_id)
        {
            case 0:
            break;

            case 1:
            break;
        }
        SetPage(Page.participant);
    }
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



    // Participant ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
    int deck_num = 0;
    bool isReady = false;

    public void SkillUISetter(int deck_id, int skill_id, Sprite sprite, Color icon_color, Color frame_color)
    {
        skillDecks[deck_id].skills[skill_id].icon.sprite = sprite;
        skillDecks[deck_id].skills[skill_id].icon.color = icon_color;
        skillDecks[deck_id].skills[skill_id].frame.color = frame_color;
   }

    public void SkillArrow(int direction)
    {
        switch(direction)
        {
            case 1:
            deck_num ++;
            deck_num = Mathf.Clamp(deck_num, 0, GameInfo.deck_count - 1);
            break;

            case -1:
            deck_num --;
            deck_num = Mathf.Clamp(deck_num, 0, GameInfo.deck_count - 1);
            break;
        }
        if(selectedMulti)
        {
            int?[] new_skillIds, new_skillLevels;
            PlayerInfo.SkillIdGetter(deck_num, out new_skillIds);
            PlayerInfo.SkillLevelGetter(deck_num, out new_skillLevels);
            string new_skillCode;
            LobbyParticipantData.SkillCodeEncoder(new_skillIds, new_skillLevels, out new_skillCode);
            LobbyParticipantData new_data = new LobbyParticipantData(myNumber, myName, myClientId, myTeam, isReady, new_skillCode);
            LobbyLinkedData.I.SetParticipantDataServerRpc(NetworkManager.Singleton.LocalClientId, new_data);
            // LinkedDataの値が更新されるので、自動的にRefreshPlayerSkillIconsが呼ばれる
        }
        else
        {
            RefreshPlayerSkillIcons();
        }
    }

    public void NameUISetter(int blockId, string name)
    {
        names[blockId].text = name;
    }

    // SkillIconを全て更新する
    void RefreshPlayerSkillIcons()
    {
        if(selectedMulti)
        {
            for(int deck_id = 0; deck_id < skillDecks.Length; deck_id++)
            {
                LobbyParticipantData? data_nullable;
                int fighterNo = GameInfo.GetNoFromTeam(Team.Red, deck_id);
                data_nullable = LobbyLinkedData.I.GetParticipantDataByNo(fighterNo);

                if(data_nullable.HasValue)
                {
                    LobbyParticipantData data = data_nullable.Value;
                    int?[] skillIds, skillLevels;
                    LobbyParticipantData.SkillCodeDecoder(data.skillCode.ToString(), out skillIds, out skillLevels);
                    for(int skillNum = 0; skillNum < GameInfo.max_skill_count; skillNum++)
                    {
                        if(skillIds[skillNum].HasValue)
                        {
                            int skillId = (int)skillIds[skillNum];
                            SkillData skillData = SkillDatabase.I.SearchSkillById(skillId);
                            SkillUISetter(deck_id, skillNum, skillData.GetSprite(), Color.white, skillData.GetColor());
                        }
                        else
                        {
                            SkillUISetter(deck_id, skillNum, null, Color.clear, Color.white);
                        }
                    }
                }
                else
                {
                    for(int skillNum = 0; skillNum < GameInfo.max_skill_count; skillNum++)
                    {
                        SkillUISetter(deck_id, skillNum, null, Color.clear, Color.white);
                    }
                }
            }
        }

        else
        {
            for(int k = 0; k < GameInfo.max_skill_count; k++)
            {
                int? skillId = PlayerInfo.deck_skill_ids[deck_num, k];
                if(skillId.HasValue)
                {
                    SkillData data = SkillDatabase.I.SearchSkillById((int)skillId);
                    SkillUISetter(0, k, data.GetSprite(), Color.white, data.GetColor());
                }
                else
                {
                    SkillUISetter(0, k, null, Color.clear, Color.white);
                }
            }
        }
    }

    // 名前を全て更新する(Multiの時のみ使用)
    void RefreshNames()
    {
        int nameblockIndex = GameInfo.GetBlockNoFromNo(myNumber);
        for (int blockNo = 0; blockNo < GameInfo.max_player_count / 2; blockNo++)
        {
            if (blockNo == nameblockIndex)
            {
                if(!arrows[blockNo].activeSelf) arrows[blockNo].SetActive(true);
                NameUISetter(blockNo, myName);
            }
            else
            {
                if(arrows[blockNo].activeSelf) arrows[blockNo].SetActive(false);

                // Set your team mates names to UI.
                int mateFighterNo = GameInfo.GetNoFromTeam(myTeam, blockNo);
                LobbyParticipantData? mateData_nullable = LobbyLinkedData.I.GetParticipantDataByNo(mateFighterNo);
                if (mateData_nullable.HasValue)
                {
                    LobbyParticipantData mateData = (LobbyParticipantData)mateData_nullable;
                    NameUISetter(blockNo, mateData.name.ToString());
                }
                else
                {
                    NameUISetter(blockNo, "");
                }
            }
        }
    }

    public void ReadyForBattle()
    {
        if(selectedMulti)
        {
            isReady = !isReady;
            LobbyParticipantData old_data = (LobbyParticipantData)LobbyLinkedData.I.GetParticipantDataByClientId(myClientId);
            LobbyParticipantData new_data = new LobbyParticipantData(myNumber, myName, myClientId, myTeam, isReady, old_data.skillCode);
            LobbyLinkedData.I.SetParticipantDataServerRpc(myClientId, new_data);
            if(isReady)
            {
                readyForBattleText.text = "Change Skill Deck";
                returnButton.interactable = false;
            }
            else
            {
                readyForBattleText.text = "Ready For Battle";
                returnButton.interactable = true;
            }
        }

        else
        {
            readyForBattleButton.interactable = false;
            var seq = DOTween.Sequence();
            seq.Append(participantObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => {
                LobbyFighter.I.SortieAllFighters(Team.Red, () => DOVirtual.DelayedCall(1, () => SceneManager2.I.LoadSceneAsync2(GameScenes.offline, FadeType.left, FadeType.bottom)).Play()); }));
            seq.Join(returnRect.DOAnchorPosX(-100, tweenDuration));
            seq.Append(menuRect.DOScaleY(0, tweenDuration));
            seq.Play();

            // Send battle data to BattleInfo.
            int?[] skillIds, skillLevels;
            PlayerInfo.SkillIdGetter(deck_num, out skillIds);
            PlayerInfo.SkillLevelGetter(deck_num, out skillLevels);
            BattleInfo.ParticipantBattleData battleData = new BattleInfo.ParticipantBattleData(0, true, null, "Player", Team.Red, skillIds, skillLevels);
            BattleInfo.battleDatas[0] = battleData;
        }
    }
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



    void ExitLobby()
    {
        returnButton.interactable = false;
        returnRect.DOAnchorPosX(-100, tweenDuration);

        if(selectedMulti)
        {
            NetworkManager.Singleton.Shutdown();
            if(selectedHost)
            {
                stageObject.transform.DOScaleX(0, tweenDuration);
            }
            else
            {
                participantObject.transform.DOScaleX(0, tweenDuration);
            }
            DOVirtual.DelayedCall(tweenDuration, () =>
            {
                menuRect.DOScaleY(0, tweenDuration).OnComplete(() => SceneManager2.I.LoadScene2(GameScenes.onlinelobby));
            }).Play();
        }
        else
        {
            stageObject.transform.DOScaleX(0, tweenDuration);
            DOVirtual.DelayedCall(tweenDuration, () =>
            {
                menuRect.DOScaleY(0, tweenDuration);
                SceneManager2.I.LoadSceneAsync2(GameScenes.menu, FadeType.gradually, FadeType.left);
            }).Play();
        }
    }
}