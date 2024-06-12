using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using Cysharp.Threading.Tasks;
using System;

public class OnlineLobbyUI : Singleton<OnlineLobbyUI>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    #region UIs
    RectTransform menuRect, returnRect;
    Button returnButton, teamButton;
    TextMeshProUGUI teamButtonText;
    TMP_InputField codeInputField;
    GameObject hostClientObj, publicPrivateObj, joinLobbyObj, lobbyCodeObj, teamObj;
    TextMeshProUGUI[] redNames = new TextMeshProUGUI[GameInfo.max_player_count];
    TextMeshProUGUI[] blueNames = new TextMeshProUGUI[GameInfo.max_player_count];
    GameObject ruleObj, participantObj;
    TextMeshProUGUI ruleText, stageText, timeText;
    TextMeshProUGUI readyText;
    [SerializeField] Color nameColor, myNameColor, selectedNameColor, selectedMyNameColor;
    #endregion

    #region Lobby Status
    TextMeshProUGUI statusText;
    Tween statusAnim;
    enum Status { CREATING_ROOM, ENTERING_ROOM, FAILED_CREATING_ROOM, FAILED_ENTERING_ROOM, LOBBY_CODE }
    void SetStatusText(Status status, bool blink, string lobby_code = "")
    {
        switch (status)
        {
            case Status.CREATING_ROOM:
                statusText.text = "Creating Lobby ...";
                break;

            case Status.ENTERING_ROOM:
                statusText.text = "Entering Lobby ...";
                break;

            case Status.FAILED_CREATING_ROOM:
                statusText.text = "Failed Creating Lobby";
                break;

            case Status.FAILED_ENTERING_ROOM:
                statusText.text = "Failed Entering Lobby";
                break;

            case Status.LOBBY_CODE:
                statusText.text = "Lobby Code = " + lobby_code;
                break;
        }

        if (statusAnim.IsActive())
        {
            statusAnim.Kill();
        }

        if (blink)
        {
            float d = 0.5f;
            statusAnim = statusText.DOFade(0.7f, d)
                .OnStart(() => statusText.FadeColor(0.35f))
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.OutQuart);
        }
        else
        {
            statusText.FadeColor(0.7f);
        }
    }

    void StopStatusText()
    {
        if (statusAnim.IsActive())
        {
            statusAnim.Kill();
        }
        statusText.text = "";
        statusText.FadeColor(0.5f);
    }
    #endregion

    #region Game Settings
    Rule rule = Rule.BATTLEROYAL;
    Stage stage = Stage.SPACE;
    int time_sec = GameInfo.min_time_sec;
    #endregion

    #region UI Animation
    const float tweenDuration = 0.1f;
    const float tweenInterval = 0.4f;
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
    #endregion

    # region Page
    public enum Page { HOST_CLIENT, PUBLIC_PRIVATE, JOIN_LOBBY, LOBBY_CODE, TEAM, RULE, PARTICIPANT }
    Page page;
    bool pageChanged = false;
    public void SetPage(Page next)
    {
        page = next;
        pageChanged = true;
    }
    #endregion

    #region Player State
    ulong clientId { get { return NetworkManager.Singleton.LocalClientId; } }
    bool selectedTeam = false;
    bool selectedHost
    {
        get { return BattleInfo.isHost; }
        set { BattleInfo.isHost = value; }
    }
    #endregion

    /// <summary>Boolean to prevent creating multiple lobbies when pressed button in a row.</summary>
    bool accessingLobby = false;


    void Start()
    {
        #region Get UIs

        #region Menu & Return
        menuRect = transform.Find("Menu").GetComponent<RectTransform>();
        returnRect = transform.Find("Return").GetComponent<RectTransform>();
        returnButton = returnRect.GetComponent<Button>();
        menuRect.DOScaleY(0, 0);
        returnRect.DOAnchorPosX(-100, 0);
        returnButton.onClick.AddListener(() =>
        {
            returnButton.interactable = false;
            RelayAllocation.SignOutPlayer(); // SignOut from AuthenticationService.
            NetworkManager.Singleton.Shutdown(); // Shutdown (Stop host or client)
            ExitLobbyUI();
        });
        returnButton.interactable = false;
        #endregion

        #region Lobby
        hostClientObj = menuRect.Find("HostClient").gameObject;
        publicPrivateObj = menuRect.Find("PublicPrivate").gameObject;
        joinLobbyObj = menuRect.Find("JoinLobby").gameObject;
        lobbyCodeObj = menuRect.Find("LobbyCode").gameObject;
        teamObj = menuRect.Find("Team").gameObject;
        codeInputField = lobbyCodeObj.transform.Find("InputField").GetComponent<TMP_InputField>();
        teamButton = teamObj.transform.Find("StartButton").GetComponent<Button>();
        teamButtonText = teamObj.transform.Find("StartButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        statusText = menuRect.Find("StatusText").GetComponent<TextMeshProUGUI>();
        statusText.text = "";
        hostClientObj.SetActive(true);
        publicPrivateObj.SetActive(false);
        joinLobbyObj.SetActive(false);
        lobbyCodeObj.SetActive(false);
        teamObj.SetActive(false);
        #endregion

        #region Fighter
        for (int k = 0; k < GameInfo.max_player_count; k++)
        {
            redNames[k] = teamObj.transform.Find("Players/Red").GetChild(k).GetComponent<TextMeshProUGUI>();
            blueNames[k] = teamObj.transform.Find("Players/Blue").GetChild(k).GetComponent<TextMeshProUGUI>();
            redNames[k].text = "";
            blueNames[k].text = "";
        }
        #endregion

        #region Game Settings & Skill Select
        ruleObj = menuRect.Find("GameSettings").gameObject;
        participantObj = menuRect.Find("Participant").gameObject;
        ruleText = ruleObj.transform.Find("Rule/Text (TMP)").GetComponent<TextMeshProUGUI>();
        stageText = ruleObj.transform.Find("Stage/Text (TMP)").GetComponent<TextMeshProUGUI>();
        timeText = ruleObj.transform.Find("Time/Text (TMP)").GetComponent<TextMeshProUGUI>();
        readyText = participantObj.transform.Find("ConfirmButton").GetComponentInChildren<TextMeshProUGUI>();
        ruleObj.SetActive(false);
        participantObj.SetActive(false);
        ruleText.text = rule.ToString();
        stageText.text = stage.ToString();
        int time_min = time_sec / 60;
        timeText.text = time_min.ToString() + " minutes";
        for (int k = 0; k < GameInfo.team_member_count; k++)
        {
            Transform nameBlock_trans = participantObj.transform.Find($"NameBlock{k}");
            names[k] = nameBlock_trans.Find("Name").GetComponent<TextMeshProUGUI>();
            arrows[k] = nameBlock_trans.Find("Arrows").gameObject;
            skillDecks[k].skills = new SkillUI[GameInfo.max_skill_count];
            for (int m = 0; m < skillDecks[k].skills.Length; m++)
            {
                Transform frame_trans = nameBlock_trans.Find($"SkillIcon{m}");
                skillDecks[k].skills[m].frame = frame_trans.GetComponent<Image>();
                skillDecks[k].skills[m].icon = frame_trans.Find("Image").GetComponent<Image>();
            }
        }
        #endregion

        #endregion

        #region Set Callbacks to LobbyLinkedData & GameNetPortal
        LobbyLinkedData.I.acceptDataChange = true;
        LobbyLinkedData.I.AddOnValueChangedAction((NetworkListEvent<LobbyParticipantData> listEvent) =>
        {
            switch (page)
            {
                case Page.TEAM:
                    RefreshTeamUI();
                    break;

                case Page.PARTICIPANT:
                    RefreshAllParticipantsSkill();
                    break;
            }
        });
        GameNetPortal.I.OnKickedOutAction += () =>
        {
            SetPage(Page.JOIN_LOBBY);
        };
        #endregion

        #region Menu & Return Enter Animation.
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(0.5f);
        seq.Append(EnterMenuReturn());
        seq.OnComplete(() =>
        {
            hostClientObj.transform.DOScaleX(1, tweenDuration);
            page = Page.HOST_CLIENT;
        });
        seq.Play();
        #endregion
    }


    void Update()
    {
        // === Check if host can the start the game === //
        if (selectedHost)
        {
            if (page == Page.TEAM)
            {
                if (GameNetPortal.I.joinedLobby != null)
                {
                    bool team_member_within_capacity =
                        LobbyLinkedData.I.GetTeamMemberCount(Team.RED) <= GameInfo.team_member_count &&
                        LobbyLinkedData.I.GetTeamMemberCount(Team.BLUE) <= GameInfo.team_member_count;
                    bool everyone_selected_team = LobbyLinkedData.I.EveryoneSelectedTeamExceptHost();
                    teamButton.interactable = team_member_within_capacity && everyone_selected_team;
                }
            }
        }

        // === On page changed === //
        if (!pageChanged) return;
        pageChanged = false;

        // Reset status text when page changed.
        StopStatusText();

        Sequence sequence = DOTween.Sequence();
        switch (page)
        {
            case Page.HOST_CLIENT:
                #region Host or Client
                sequence.AppendCallback(() => { hostClientObj.SetActive(true); });
                sequence.Join(publicPrivateObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => publicPrivateObj.SetActive(false)));
                sequence.Join(joinLobbyObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => joinLobbyObj.SetActive(false)));
                sequence.Join(hostClientObj.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(hostClientObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() =>
                {
                    returnButton.interactable = false;
                    RelayAllocation.SignOutPlayer(); // SignOut from AuthenticationService.
                    NetworkManager.Singleton.Shutdown(); // Shutdown (Stop host or client)
                    ExitLobbyUI();
                });
                #endregion
                break;

            // Host
            case Page.PUBLIC_PRIVATE:
                #region Public or Private
                sequence.AppendCallback(() => { publicPrivateObj.SetActive(true); accessingLobby = false; });
                sequence.Join(hostClientObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => hostClientObj.SetActive(false)));
                sequence.Join(lobbyCodeObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => lobbyCodeObj.SetActive(false)));
                sequence.Join(teamObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => teamObj.SetActive(false)));
                sequence.Join(ruleObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => ruleObj.SetActive(false)));
                sequence.Join(participantObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => participantObj.SetActive(false)));
                sequence.Join(publicPrivateObj.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(publicPrivateObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() =>
                {
                    returnButton.interactable = false;
                    SetPage(Page.HOST_CLIENT);
                });
                #endregion
                break;

            // Client
            case Page.JOIN_LOBBY:
                #region Join Lobby
                sequence.AppendCallback(() => { joinLobbyObj.SetActive(true); accessingLobby = false; });
                sequence.Join(hostClientObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => hostClientObj.SetActive(false)));
                sequence.Join(lobbyCodeObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => lobbyCodeObj.SetActive(false)));
                sequence.Join(teamObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => teamObj.SetActive(false)));
                sequence.Join(participantObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => participantObj.SetActive(false)));
                sequence.Join(joinLobbyObj.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(joinLobbyObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() =>
                {
                    returnButton.interactable = false;
                    SetPage(Page.HOST_CLIENT);
                });
                #endregion
                break;

            // Client
            case Page.LOBBY_CODE:
                #region Lobby Code
                sequence.AppendCallback(() => { lobbyCodeObj.SetActive(true); accessingLobby = false; });
                sequence.Join(joinLobbyObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => joinLobbyObj.SetActive(false)));
                sequence.Join(teamObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => teamObj.SetActive(false)));
                sequence.Join(lobbyCodeObj.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(lobbyCodeObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                returnButton.onClick.RemoveAllListeners();
                if (selectedHost)
                {
                    returnButton.onClick.AddListener(() =>
                    {
                        returnButton.interactable = false;
                        SetPage(Page.PUBLIC_PRIVATE);
                    });
                }
                else
                {
                    returnButton.onClick.AddListener(() =>
                    {
                        returnButton.interactable = false;
                        SetPage(Page.JOIN_LOBBY);
                    });
                }
                #endregion
                break;

            // All Participants
            case Page.TEAM:
                #region Team
                // Enable team changing.
                LobbyLinkedData.I.acceptDataChange = true;
                sequence.AppendCallback(() => { teamObj.SetActive(true); });
                sequence.Join(publicPrivateObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => publicPrivateObj.SetActive(false)));
                sequence.Join(joinLobbyObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => joinLobbyObj.SetActive(false)));
                sequence.Join(lobbyCodeObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => lobbyCodeObj.SetActive(false)));
                sequence.Join(teamObj.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(teamObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                returnButton.onClick.RemoveAllListeners();
                teamButton.onClick.RemoveAllListeners();
                if (selectedHost)
                {
                    teamButtonText.text = "Close Lobby";
                    returnButton.onClick.AddListener(async () =>
                    {
                        returnButton.interactable = false;
                        // Kill Lobby.
                        await GameNetPortal.I.KillJoinedLobby();
                        // Shutdown to exit from Relay.
                        NetworkManager.Singleton.Shutdown();
                        await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
                        SetPage(Page.PUBLIC_PRIVATE);
                    });
                    teamButton.onClick.AddListener(() =>
                    {
                        SortieLobbyManager.I.OnParticipantDetermined();
                    });
                }
                else
                {
                    teamButtonText.text = "Select Team";
                    returnButton.onClick.AddListener(async () =>
                    {
                        returnButton.interactable = false;
                        // Exit Lobby.
                        await GameNetPortal.I.ExitJoinedLobby();
                        // Shutdown to exit from Relay.
                        NetworkManager.Singleton.Shutdown();
                        await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
                        SetPage(Page.JOIN_LOBBY);
                    });
                    teamButton.onClick.AddListener(() =>
                    {
                        // Toggle selected team & set to LobbyLinkedData.
                        selectedTeam = !selectedTeam;
                        if (selectedTeam) teamButtonText.text = "Change Team";
                        else teamButtonText.text = "Select Team";
                        LobbyLinkedData.I.RequestServerModifyParticipantData(clientId, selectedTeam: selectedTeam);
                    });
                    teamButton.interactable = true;
                }

                // Refresh team when entered Page.TEAM.
                RefreshTeamUI();

                // Show Lobby Code.
                SetStatusText(Status.LOBBY_CODE, false, GameNetPortal.I.joinedLobby.LobbyCode);
                #endregion
                break;

            // All Participants
            case Page.RULE:
                #region Rule
                // Enable skill deck changing.
                LobbyLinkedData.I.acceptDataChange = true;
                sequence.AppendCallback(() => { ruleObj.SetActive(true); });
                sequence.Join(teamObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => teamObj.SetActive(false)));
                sequence.Join(participantObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => participantObj.SetActive(false)));
                sequence.Join(ruleObj.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(ruleObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(async () =>
                {
                    returnButton.interactable = false;

                    // Shutdown to exit from Relay.
                    NetworkManager.Singleton.Shutdown();
                    await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);

                    // Change page.
                    SetPage(Page.PUBLIC_PRIVATE);
                });
                #endregion
                break;

            // All Participants
            case Page.PARTICIPANT:
                #region Participant
                sequence.AppendCallback(() => { participantObj.SetActive(true); });
                sequence.Join(teamObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => teamObj.SetActive(false)));
                sequence.Join(ruleObj.transform.DOScaleX(0, tweenDuration).OnComplete(() => ruleObj.SetActive(false)));
                sequence.Join(participantObj.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(participantObj.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));
                returnButton.onClick.RemoveAllListeners();
                if (selectedHost)
                {
                    returnButton.onClick.AddListener(() =>
                    {
                        returnButton.interactable = false;
                        SetPage(Page.RULE);
                    });
                }
                else
                {
                    returnButton.onClick.AddListener(async () =>
                    {
                        returnButton.interactable = false;

                        // Shutdown to exit from Relay.
                        NetworkManager.Singleton.Shutdown();
                        await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);

                        // Change page.
                        SetPage(Page.JOIN_LOBBY);
                    });
                }
                RefreshAllParticipantsSkill();
                RefreshAllParticipantsNames();
                #endregion
                break;
        }
        sequence.Play();
    }


    #region On Button Pressed Callbacks
    public void Host()
    {
        selectedHost = true;
        SetPage(Page.PUBLIC_PRIVATE);
    }

    public void Client()
    {
        selectedHost = false;
        SetPage(Page.JOIN_LOBBY);
    }

    // === Host Only === //
    public async void CreateLobbyRelay(bool is_private)
    {
        if (!selectedHost)
        {
            return;
        }
        if (accessingLobby)
        {
            return;
        }
        accessingLobby = true;

        // === Host Create Relay & Lobby === //
        SetStatusText(Status.CREATING_ROOM, true);
        string join_code = await GameNetPortal.I.CreateRelayAsync(GameInfo.max_player_count); // Create Relay first to get join_code.
        if (join_code != "")
        {
            bool created = await GameNetPortal.I.CreateLobbyAsync(is_private, join_code);
            if (created)
            {
                SetPage(Page.TEAM);
                return; // leave accessingLobby true if succeed all processes.
            }
            else
            {
                SetStatusText(Status.FAILED_CREATING_ROOM, false);
                NetworkManager.Singleton.Shutdown(); // Shutdown to stop Relay if failed to create Lobby.
                await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            }
        }
        else
        {
            SetStatusText(Status.FAILED_CREATING_ROOM, false);
        }
        // =============================== //

        // If failed entering Lobby & Relay.
        accessingLobby = false;
    }

    // === Client Only === //
    public async void JoinRandomSelected()
    {
        if (selectedHost)
        {
            return;
        }
        if (accessingLobby)
        {
            return;
        }
        accessingLobby = true;

        // === Client Enter Lobby & Relay === //
        SetStatusText(Status.ENTERING_ROOM, true);
        bool enter_lobby = await GameNetPortal.I.EnterRandomLobby();
        if (enter_lobby)
        {
            string join_code = GameNetPortal.I.GetRelayCodeFromJoinedLobby();
            bool enter_relay = await GameNetPortal.I.EnterRelayAsync(join_code);
            if (enter_relay)
            {
                SetPage(Page.TEAM);
                return; // leave accessingLobby true if succeed all processes.
            }
            else
            {
                SetStatusText(Status.FAILED_ENTERING_ROOM, false);
                await GameNetPortal.I.ExitJoinedLobby(); // Exit the Lobby entered if failed to enter Relay.
            }
        }
        else
        {
            SetStatusText(Status.FAILED_ENTERING_ROOM, false);
        }
        // ================================= //

        // If failed entering Lobby & Relay.
        accessingLobby = false;
    }

    public void JoinWithCodeSelected()
    {
        if (selectedHost)
        {
            return;
        }
        SetPage(Page.LOBBY_CODE);
    }

    public async void CodeInputFinish()
    {
        if (selectedHost)
        {
            return;
        }
        if (accessingLobby)
        {
            return;
        }
        accessingLobby = true;

        // === Client Enter Lobby & Relay === //
        string lobby_code = codeInputField.text;
        bool enter_lobby = await GameNetPortal.I.EnterLobbyWithCode(lobby_code);
        if (enter_lobby)
        {
            string join_code = GameNetPortal.I.GetRelayCodeFromJoinedLobby();
            bool enter_relay = await GameNetPortal.I.EnterRelayAsync(join_code);
            if (enter_relay)
            {
                SetPage(Page.TEAM);
                return; // leave accessingLobby true if succeed all processes.
            }
            else
            {
                SetStatusText(Status.FAILED_ENTERING_ROOM, false);
                await GameNetPortal.I.ExitJoinedLobby(); // Exit the Lobby entered if failed to enter Relay.
            }
        }
        else
        {
            SetStatusText(Status.FAILED_ENTERING_ROOM, false);
        }
        // ================================= //

        // If failed entering Lobby & Relay.
        accessingLobby = false;
    }

    public void OnTeamArrowPressed(int team_id)
    {
        // If already selected team, do nothing.
        if (selectedTeam)
        {
            return;
        }

        // Return if same team.
        LobbyParticipantData lobby_data = LobbyLinkedData.I.GetParticipantDataByClientId(clientId).Value;
        Team new_team = (Team)team_id;
        Team current_team = lobby_data.team;
        if (new_team == current_team)
        {
            return;
        }

        // Update lobby player data.
        LobbyLinkedData.I.RequestServerModifyParticipantData(clientId, team: new_team);
    }

    public void OnRuleSettingsButton()
    {
        DetermineSettings();
        SetPage(Page.PARTICIPANT);
    }

    public void OnReadyForBattle()
    {
        ReadyForBattle();
    }

    #endregion

    #region Team
    /// <summary>When team was NONE, resets the UI.</summary>
    void TeamUISetter(int number, Team team, string name, Color color)
    {
        switch (team)
        {
            case Team.RED:
                redNames[number].text = name;
                redNames[number].FadeColor(1);
                redNames[number].DOColor(color, 0);
                blueNames[number].FadeColor(0);
                break;

            case Team.BLUE:
                blueNames[number].text = name;
                redNames[number].FadeColor(0);
                blueNames[number].FadeColor(1);
                blueNames[number].DOColor(color, 0);
                break;

            default:
                redNames[number].text = "";
                blueNames[number].text = "";
                redNames[number].FadeColor(0);
                blueNames[number].FadeColor(0);
                return;
        }
    }

    /// <summary>Refreshes all participants team UIs with reference to GameNetPortal.</summary>
    void RefreshTeamUI()
    {
        // Reset all numbers first.
        for (int number = 0; number < GameInfo.max_player_count; number++)
        {
            TeamUISetter(number, Team.NONE, "", nameColor);
        }

        // Set player name to UI if exists in lobby player data.
        for (int k = 0; k < LobbyLinkedData.I.participantCount; k++)
        {
            LobbyParticipantData lobby_data = LobbyLinkedData.I.participantDatas[k];
            string player_name = lobby_data.name.Value;
            Team player_team = lobby_data.team;

            // If it's my data.
            if (lobby_data.clientId == clientId)
            {
                if (lobby_data.selectedTeam)
                {
                    TeamUISetter(k, player_team, player_name, selectedMyNameColor);
                }
                else
                {
                    TeamUISetter(k, player_team, player_name, myNameColor);
                }
            }

            // Other players's data.
            else
            {
                if (lobby_data.selectedTeam)
                {
                    TeamUISetter(k, player_team, player_name, selectedNameColor);
                }
                else
                {
                    TeamUISetter(k, player_team, player_name, nameColor);
                }
            }
        }
    }
    #endregion

    #region Settings
    void DetermineSettings()
    {
        BattleInfo.rule = rule;
        BattleInfo.stage = stage;
        BattleInfo.time_sec = time_sec;
    }

    // Rule ///////////////////////////////////////////////////////////////////////////////
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

    #region Participant Page
    struct SkillUI
    {
        public Image frame;
        public Image icon;
    }
    struct SkillDeck
    {
        public SkillUI[] skills;
    }
    SkillDeck[] skillDecks = new SkillDeck[GameInfo.team_member_count];
    GameObject[] arrows = new GameObject[GameInfo.team_member_count];
    TextMeshProUGUI[] names = new TextMeshProUGUI[GameInfo.team_member_count];
    bool isReady = false;

    int myDeckNum
    {
        get { return SortieLobbyManager.I.myDeckNum; }
        set { SortieLobbyManager.I.myDeckNum = value; }
    }

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
        int myNumber = SortieLobbyManager.I.myData.number;
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

                Debug.Log($"MyNo:{myNumber} BlockNo:{lobby_data.number}");

                // My block Id
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

    #region Ready for Battle
    void ReadyForBattle()
    {
        isReady = !isReady;
        LobbyLinkedData.I.RequestServerModifyParticipantData(NetworkManager.Singleton.LocalClientId, isReady: isReady);
        if (isReady)
        {
            readyText.text = "Change Skill Deck";
            returnButton.interactable = false;
        }
        else
        {
            readyText.text = "Ready For Battle";
            returnButton.interactable = true;
        }
    }
    #endregion

    #region Exit Lobby
    void ExitLobbyUI()
    {
        returnButton.interactable = false;
        returnRect.DOAnchorPosX(-100, tweenDuration);
        hostClientObj.transform.DOScaleX(0, tweenDuration);
        DOVirtual.DelayedCall(tweenDuration, () =>
        {
            menuRect.DOScaleY(0, tweenDuration);
            SceneManager2.I.LoadSceneAsync2(GameScenes.MENU, FadeType.gradually);
        }).Play();
    }
    #endregion
}
