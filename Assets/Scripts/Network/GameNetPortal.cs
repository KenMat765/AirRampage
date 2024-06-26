using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public class GameNetPortal : Singleton<GameNetPortal>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    readonly string SERVER_TO_CLIENT_CONNECTIONRESULT = "ServerToClientConnectionResult";
    public enum ConnectStatus { SUCCESS, SERVER_FULL }
    public ConnectStatus connectStatus;
    Action<ConnectStatus> OnRecieveConnectionResult;
    List<LobbyParticipantData> clientDataQueue = new List<LobbyParticipantData>();

    /// <summary>
    /// Called when client was kicked out from Relay. (!! This is NOT called for the host !!)
    /// </summary>
    public Action OnKickedOutAction { get; set; }



    // ====== Lobby ====== //
    public Lobby joinedLobby { get; private set; }
    public string playerId { get; private set; }
    public const string KEY_RELAY_CODE = "RelayCode";
    float heartbeatTimer = 0f;
    float heatbeatInterval = 15f;

    public async Task<bool> CreateLobbyAsync(bool isPrivate, string relay_code = "")
    {
        try
        {
            string lobbyName = "TestLobby";
            int maxPlayers = GameInfo.MAX_PLAYER_COUNT;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject>()
                {
                    {KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relay_code)},
                }
            };
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            Debug.Log("Lobby created: " + joinedLobby.LobbyCode);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Error creating lobby: " + e.Message);
            return false;
        }
    }

    /// <summary>Join random public lobby.</summary>
    public async Task<bool> EnterRandomLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            Debug.Log("Lobby joined : " + joinedLobby.LobbyCode);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Error joining lobby : " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Join private lobby by lobby code.
    /// Password can be set to lobby, but use lobby code as password for now.
    /// Lobby code is visible only to lobby members, and players from outside can join to the private lobby by lobby code.
    /// </summary>
    /// <param name="lobbyCode">code necessary to join lobby.</param>
    public async Task<bool> EnterLobbyWithCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions();
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            Debug.Log("Lobby joined:" + joinedLobby.LobbyCode);
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Error joining lobby:" + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Send heartbeat to lobby, so as not to kill lobby. (Host only method)
    /// Call this in Update.
    /// </summary>
    async void RefreshLobbyHeatbeat()
    {
        // This method is only for host.
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        // Do nothig if not joined to any lobby.
        if (joinedLobby == null)
        {
            return;
        }

        // Send heartbeat after heartbeatInterval.
        heartbeatTimer += Time.deltaTime;
        if (heartbeatTimer > heatbeatInterval)
        {
            heartbeatTimer = 0.0f;
            await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
        }
    }

    public async void UpdateJoinedLobby(bool? is_private = null, bool? is_locked = null, string relay_code = "")
    {
        if (joinedLobby == null)
        {
            Debug.LogWarning("joinedLobby is null");
            return;
        }
        joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
        {
            IsPrivate = is_private.HasValue ? is_private.Value : joinedLobby.IsPrivate,
            IsLocked = is_locked.HasValue ? is_locked.Value : joinedLobby.IsLocked,
            Data = new Dictionary<string, DataObject>()
            {
                {KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member,
                    relay_code != "" ? relay_code : joinedLobby.Data[KEY_RELAY_CODE].Value)},
            }
        });
    }

    public string GetRelayCodeFromJoinedLobby()
    {
        if (joinedLobby == null)
        {
            Debug.LogWarning("joinedLobby is null");
            return "";
        }
        return joinedLobby.Data[KEY_RELAY_CODE].Value;
    }

    public async Task ExitJoinedLobby()
    {
        if (joinedLobby == null)
        {
            Debug.LogWarning("joinedLobby is null");
            return;
        }
        try
        {
            await Lobbies.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
        }
        catch (Exception e)
        {
            Debug.Log("Error removing player from joinedLobby : " + e);
        }
        joinedLobby = null;
    }

    public async Task KillJoinedLobby()
    {
        if (joinedLobby == null)
        {
            Debug.LogWarning("joinedLobby is null");
            return;
        }
        await Lobbies.Instance.DeleteLobbyAsync(joinedLobby.Id);
        joinedLobby = null;
    }



    // ====== Relay ====== //
    public async Task<string> CreateRelayAsync(int max_player_count)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(max_player_count);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );

            // If host is not running.
            if (!NetworkManager.Singleton.IsHost)
            {
                // If client is currently running, stop it.
                if (NetworkManager.Singleton.IsClient)
                {
                    NetworkManager.Singleton.Shutdown();
                    await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
                }
                NetworkManager.Singleton.StartHost();
            }

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log("Failed to create Relay" + e);
            return "";
        }
    }

    public async Task<bool> EnterRelayAsync(string join_code)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(join_code);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );

            // If client is not running or host is running.
            if (!NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
            {
                // If host is currently running, stop it.
                if (NetworkManager.Singleton.IsHost)
                {
                    NetworkManager.Singleton.Shutdown();
                    await UniTask.WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
                }
                NetworkManager.Singleton.StartClient();
            }

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.Log("Failed to enter Relay: " + e);
            return false;
        }
    }

    /// <summary>
    /// Set connection data (name, skill code, ability_code) to NetworkManager's singleton.
    /// !! Called BEFORE starting host or client !!
    /// </summary>
    void SetConnectionData()
    {
        // Generate skill code
        int deckNum = 0;
        string skillCode;
        int?[] skillIds, skillLevels;
        PlayerInfo.I.SkillIdsGetter(deckNum, out skillIds);
        PlayerInfo.I.SkillLevelsGetter(deckNum, out skillLevels);
        LobbyParticipantData.SkillCodeEncoder(skillIds, skillLevels, out skillCode);

        // Generate ability code
        string abilityCode;
        List<int> abilityIds = PlayerInfo.I.AbilityIdsGetter();
        LobbyParticipantData.AbilityCodeEncoder(abilityIds, out abilityCode);

        var payload = new ConnectionPayload(PlayerInfo.I.myName, skillCode, abilityCode);
        string payloadJSON = JsonUtility.ToJson(payload);
        byte[] payloadBytes = Encoding.ASCII.GetBytes(payloadJSON);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
    }



    async void Start()
    {
        // Sign in to UnityServices & AuthenticationService. (returns player ID)
        playerId = await RelayAllocation.SignInPlayerAsync();

        NetworkManager.Singleton.OnServerStarted += HandleOnServerStarted;
        NetworkManager.Singleton.OnServerStopped += HandleOnServerStopped;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleOnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleOnClientDisconnect;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        OnRecieveConnectionResult += (ConnectStatus connectStatus) =>
        {
            switch (connectStatus)
            {
                case ConnectStatus.SUCCESS:
                    break;

                case ConnectStatus.SERVER_FULL:
                    break;
            }
        };

        SetConnectionData();
    }

    void Update()
    {
        RefreshLobbyHeatbeat();
    }


    // Called at server side. Host assigns member number of each team here.
    void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log("Approval Check");

        response.Pending = true;

        ulong clientId = request.ClientNetworkId;
        byte[] connectionData = request.Payload;
        string payloadJSON = Encoding.ASCII.GetString(connectionData);
        ConnectionPayload payload = JsonUtility.FromJson<ConnectionPayload>(payloadJSON);

        // Serverは無条件で入れる
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Server Entered");

            response.Approved = true;
            response.CreatePlayerObject = false;
            response.PlayerPrefabHash = null;
            response.Position = null;
            response.Rotation = null;
            response.Pending = false;

            LobbyParticipantData hostData = new LobbyParticipantData(0, 0, PlayerInfo.I.myName, clientId, false, Team.RED, false, true, payload.skillCode, payload.abilityCode);
            clientDataQueue.Add(hostData);
            return;
        }

        // Set connection status for client.
        if (NetworkManager.Singleton.ConnectedClients.Count > GameInfo.MAX_PLAYER_COUNT) connectStatus = ConnectStatus.SERVER_FULL;
        else connectStatus = ConnectStatus.SUCCESS;

        // Enter client even if connectStatus is not SUCCESS, in order to send connectStatus to client.
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.PlayerPrefabHash = null;
        response.Position = null;
        response.Rotation = null;
        response.Pending = false;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Networked ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Send connectStatus to connected client.
        ServerToClientConnectResult(clientId, connectStatus);

        if (connectStatus == ConnectStatus.SUCCESS)
        {
            // Add client's lobby data to queue if connection was approved. (properties set later : number, member_number, team, selectedTeam)
            LobbyParticipantData clientData = new LobbyParticipantData(-1, -1, payload.playerName, clientId, false, Team.RED, false, true, payload.skillCode, payload.abilityCode);
            clientDataQueue.Add(clientData);
        }
        else
        {
            // Disconnect client here after sending connectStatus.
            StartCoroutine(DisconnectClientDelayed(clientId));
        }
    }



    void HandleOnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log("<color=yellow>Scene Load Complete</color>");
    }

    void HandleOnServerStarted()
    {
        Debug.Log("<color=green>Server Started</color>");

        // Initialize LobbyLinkedData properties.
        LobbyLinkedData.I.participantDatas.Clear();
        LobbyLinkedData.I.participantDetermined = false;
    }

    // Called only at server.
    void HandleOnServerStopped(bool stopHost)
    {
        Debug.Log($"<color=red>Server Stopped</color> {stopHost}");

        string scene_name = SceneManager.GetActiveScene().name;
        switch (scene_name)
        {
            case "OnlineLobby":
                // Disable all fighters in my team. (for Host)
                Team my_team = SortieLobbyManager.I.myData.team;
                if (my_team != Team.NONE)
                {
                    LobbyFighter.I.DisableAllFighters(my_team);
                }
                // Go back to public or private lobby select page.
                OnlineLobbyUI.I.SetPage(OnlineLobbyUI.Page.PUBLIC_PRIVATE);
                break;
        }
    }

    void HandleOnClientConnected(ulong clientId)
    {
        Debug.Log($"<color=green>New Client Connected</color> {clientId}");

        // Server.
        if (NetworkManager.Singleton.IsHost)
        {
            // Add connected clients (including host) lobby data to LobbyLinkedData.
            foreach (LobbyParticipantData lobby_data in clientDataQueue)
            {
                if (lobby_data.clientId == clientId)
                {
                    Debug.Log($"<color=green>Added lobby data</color> {clientId}");
                    LobbyLinkedData.I.participantDatas.Add(lobby_data);
                    clientDataQueue.Remove(lobby_data);
                    break;
                }
            }
        }
    }

    // Called only at server & disconnected client (!! This is not called if disconnected client was HOST !!)
    void HandleOnClientDisconnect(ulong clientId)
    {
        Debug.Log($"<color=red>Client Disconnected</color> {clientId}");

        // Current scene name.
        string scene_name = SceneManager.GetActiveScene().name;

        // Server.
        if (NetworkManager.Singleton.IsHost)
        {
            switch (scene_name)
            {
                case "OnlineLobby":
                    // If participants were already determined (At page: Rule or Participant)
                    if (LobbyLinkedData.I.participantDetermined)
                    {
                        // Stop the Host (this kicks out all participants)
                        NetworkManager.Singleton.Shutdown();
                    }
                    // If still at the Team page.
                    else
                    {
                        // Remove disconnected client's lobby data.
                        LobbyLinkedData.I.RemoveParticipantData(clientId);
                    }
                    break;
            }
        }

        // Disconnected Client.
        else
        {
            switch (scene_name)
            {
                case "OnlineLobby":
                    // Disable all fighters in my team. (for Clients)
                    Team my_team = SortieLobbyManager.I.myData.team;
                    if (my_team != Team.NONE)
                    {
                        LobbyFighter.I.DisableAllFighters(my_team);
                    }
                    // Go back to join lobby page.
                    OnlineLobbyUI.I.SetPage(OnlineLobbyUI.Page.JOIN_LOBBY);
                    break;
            }
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleOnServerStarted;
            NetworkManager.Singleton.OnServerStopped -= HandleOnServerStopped;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleOnClientDisconnect;
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            if (NetworkManager.Singleton.CustomMessagingManager != null) UnRegisterMessageHandlers();
            if (NetworkManager.Singleton.SceneManager != null) NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleOnSceneLoadComplete;
        }
        OnRecieveConnectionResult = null;
        OnKickedOutAction = null;
    }



    void RegisterMessageHandlers()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SERVER_TO_CLIENT_CONNECTIONRESULT, (recieverClientId, reader) =>
        {
            // 送られてきた reader 内から status を抽出し、その値を用いて処理を行う
            reader.ReadValueSafe(out ConnectStatus status);
            OnRecieveConnectionResult?.Invoke(status);
        });
    }

    void UnRegisterMessageHandlers()
    {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SERVER_TO_CLIENT_CONNECTIONRESULT);
    }



    void ServerToClientConnectResult(ulong clientId, ConnectStatus status)
    {
        var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
        writer.WriteValueSafe(status);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SERVER_TO_CLIENT_CONNECTIONRESULT, clientId, writer);
    }

    IEnumerator DisconnectClientDelayed(ulong clientId)
    {
        yield return new WaitForSeconds(0.5f);
        NetworkManager.Singleton.DisconnectClient(clientId);
    }
}