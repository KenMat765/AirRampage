using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using Cysharp.Threading.Tasks;

public class OnlineLobbyUI : Singleton<OnlineLobbyUI>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    // Migrated from SortieLobby.
    [SerializeField] GameObject sortieCanvas;
    [SerializeField] SortieLobbyManager sortieLobbyManager;

    #region UIs
    RectTransform menuRect, returnRect;
    Button returnButton, teamButton;
    TextMeshProUGUI teamButtonText;
    TMP_InputField codeInputField;
    GameObject hostClientObject, publicPrivateObject, joinLobbyObject, lobbyCodeObject, teamObject;
    TextMeshProUGUI[] redNames = new TextMeshProUGUI[GameInfo.max_player_count];
    TextMeshProUGUI[] blueNames = new TextMeshProUGUI[GameInfo.max_player_count];
    #endregion

    [SerializeField] Color nameColor, myNameColor, selectedNameColor, selectedMyNameColor;

    #region status
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

    const float tweenDuration = 0.1f, tweenInterval = 0.4f;

    enum Page { HOST_CLIENT, PUBLIC_PRIVATE, JOIN_LOBBY, LOBBY_CODE, TEAM }
    Page page;
    bool pageChanged = false;
    void SetPage(Page next)
    {
        page = next;
        pageChanged = true;
    }

    ulong clientId { get { return NetworkManager.Singleton.LocalClientId; } }
    bool selectedTeam = false;
    bool selectedHost
    {
        get { return BattleInfo.isHost; }
        set { BattleInfo.isHost = value; }
    }

    /// <summary>Boolean to prevent creating multiple lobbies when pressed button in a row.</summary>
    bool accessingLobby = false;



    void Start()
    {
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

        hostClientObject = menuRect.Find("HostClient").gameObject;
        publicPrivateObject = menuRect.Find("PublicPrivate").gameObject;
        joinLobbyObject = menuRect.Find("JoinLobby").gameObject;
        lobbyCodeObject = menuRect.Find("LobbyCode").gameObject;
        teamObject = menuRect.Find("Team").gameObject;
        hostClientObject.SetActive(true);
        publicPrivateObject.SetActive(false);
        joinLobbyObject.SetActive(false);
        lobbyCodeObject.SetActive(false);
        teamObject.SetActive(false);

        codeInputField = lobbyCodeObject.transform.Find("InputField").GetComponent<TMP_InputField>();
        for (int k = 0; k < GameInfo.max_player_count; k++)
        {
            redNames[k] = teamObject.transform.Find("Players/Red").GetChild(k).GetComponent<TextMeshProUGUI>();
            blueNames[k] = teamObject.transform.Find("Players/Blue").GetChild(k).GetComponent<TextMeshProUGUI>();
            redNames[k].text = "";
            blueNames[k].text = "";
        }
        teamButton = teamObject.transform.Find("StartButton").GetComponent<Button>();
        teamButtonText = teamObject.transform.Find("StartButton/Text (TMP)").GetComponent<TextMeshProUGUI>();
        statusText = menuRect.Find("StatusText").GetComponent<TextMeshProUGUI>();
        statusText.text = "";

        LobbyLinkedData.I.acceptDataChange = true;
        LobbyLinkedData.I.AddOnValueChangedAction((NetworkListEvent<LobbyParticipantData> listEvent) =>
        {
            RefreshTeamUI();
        });
        GameNetPortal.I.OnKickedOutAction += () =>
        {
            SetPage(Page.JOIN_LOBBY);
        };

        DOVirtual.DelayedCall(0.5f, () =>
        {
            menuRect.DOScaleY(1, tweenDuration)
                .OnComplete(() =>
                {
                    hostClientObject.transform.DOScaleX(1, tweenDuration);
                    page = Page.HOST_CLIENT;
                });
            returnRect.DOAnchorPosX(100, tweenDuration)
                .OnComplete(() => returnButton.interactable = true);
        }).Play();
    }

    void Update()
    {
        // === Check if host can the start game === //
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
                sequence.AppendCallback(() => { hostClientObject.SetActive(true); });
                sequence.Join(publicPrivateObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => publicPrivateObject.SetActive(false)));
                sequence.Join(joinLobbyObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => joinLobbyObject.SetActive(false)));
                sequence.Join(hostClientObject.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(hostClientObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() =>
                {
                    returnButton.interactable = false;
                    RelayAllocation.SignOutPlayer(); // SignOut from AuthenticationService.
                    NetworkManager.Singleton.Shutdown(); // Shutdown (Stop host or client)
                    ExitLobbyUI();
                });
                break;

            // Host
            case Page.PUBLIC_PRIVATE:
                sequence.AppendCallback(() => { publicPrivateObject.SetActive(true); accessingLobby = false; });
                sequence.Join(hostClientObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => hostClientObject.SetActive(false)));
                sequence.Join(lobbyCodeObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => lobbyCodeObject.SetActive(false)));
                sequence.Join(teamObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => teamObject.SetActive(false)));
                sequence.Join(publicPrivateObject.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(publicPrivateObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() =>
                {
                    returnButton.interactable = false;
                    SetPage(Page.HOST_CLIENT);
                });
                break;

            // Client
            case Page.JOIN_LOBBY:
                sequence.AppendCallback(() => { joinLobbyObject.SetActive(true); accessingLobby = false; });
                sequence.Join(hostClientObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => hostClientObject.SetActive(false)));
                sequence.Join(lobbyCodeObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => lobbyCodeObject.SetActive(false)));
                sequence.Join(teamObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => teamObject.SetActive(false)));
                sequence.Join(joinLobbyObject.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(joinLobbyObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() =>
                {
                    returnButton.interactable = false;
                    SetPage(Page.HOST_CLIENT);
                });
                break;

            // Client
            case Page.LOBBY_CODE:
                sequence.AppendCallback(() => { lobbyCodeObject.SetActive(true); accessingLobby = false; });
                sequence.Join(joinLobbyObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => joinLobbyObject.SetActive(false)));
                sequence.Join(teamObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => teamObject.SetActive(false)));
                sequence.Join(lobbyCodeObject.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(lobbyCodeObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

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
                break;

            // Both
            case Page.TEAM:
                sequence.AppendCallback(() => { teamObject.SetActive(true); });
                sequence.Join(publicPrivateObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => publicPrivateObject.SetActive(false)));
                sequence.Join(joinLobbyObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => joinLobbyObject.SetActive(false)));
                sequence.Join(lobbyCodeObject.transform.DOScaleX(0, tweenDuration).OnComplete(() => lobbyCodeObject.SetActive(false)));
                sequence.Join(teamObject.transform.DOScaleX(0, 0));
                sequence.AppendInterval(tweenInterval);
                sequence.Append(teamObject.transform.DOScaleX(1, tweenDuration).OnComplete(() => returnButton.interactable = true));

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
                    teamButton.onClick.AddListener(async () =>
                    {
                        // Close Lobby & determine participants.
                        LobbyLinkedData.I.acceptDataChange = false;
                        await GameNetPortal.I.KillJoinedLobby();
                        LobbyLinkedData.I.DetermineParticipants();

                        // === Change to SortieLobby === //
                        sortieCanvas.SetActive(true);
                        SortieLobbyManager.Init();
                        sortieLobbyManager.enabled = true;
                        gameObject.SetActive(false);
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

                break;
        }
        sequence.Play();
    }



    // ====== On Button Pressed Methods ====== //
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


    // ====== Team ====== //
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

    void ExitLobbyUI()
    {
        returnButton.interactable = false;
        returnRect.DOAnchorPosX(-100, tweenDuration);
        hostClientObject.transform.DOScaleX(0, tweenDuration);
        DOVirtual.DelayedCall(tweenDuration, () =>
        {
            menuRect.DOScaleY(0, tweenDuration);
            SceneManager2.I.LoadSceneAsync2(GameScenes.MENU, FadeType.gradually);
        }).Play();
    }
}
