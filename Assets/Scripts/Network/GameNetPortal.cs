using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetPortal : Singleton<GameNetPortal>
{
    protected override bool dont_destroy_on_load { get; set; } = true;
    readonly string SERVER_TO_CLIENT_CONNECTIONRESULT = "ServerToClientConnectionResult";
    public enum ConnectStatus { SUCCESS, WRONG_PASSWORD, SERVER_FULL }
    public ConnectStatus connectStatus;
    Action<ConnectStatus> OnRecieveConnectionResult;
    // string password;
    // public void SetPassword(string password) => this.password = password;
    public LobbyParticipantData hostData { get; private set; }
    public bool gameStarted = false;



    void Start()
    {
        NetworkManager.Singleton.OnServerStarted += HandleOnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleOnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleOnClientDisconnect;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        OnRecieveConnectionResult += (ConnectStatus connectStatus) =>
        {
            switch (connectStatus)
            {
                case ConnectStatus.SUCCESS:
                    break;

                case ConnectStatus.WRONG_PASSWORD:
                    break;

                case ConnectStatus.SERVER_FULL:
                    break;
            }
        };
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // StartHost(), StartClient() ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        // ゲームが開始されていたら入れない
        if (gameStarted)
        {
            callback(false, null, false, null, null);
            return;
        }

        string payloadJSON = Encoding.ASCII.GetString(connectionData);
        var payload = JsonUtility.FromJson<ConnectionPayload>(payloadJSON);

        // Serverは無条件で入れる
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            callback(false, null, true, null, null);
            NetworkManager.Singleton.SceneManager.LoadScene("SortieLobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
            NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleOnSceneLoadComplete;
            // HostのデータはLobbyLinkedDataのOnNetworkSpawn()でListに加える
            hostData = new LobbyParticipantData(0, payload.playerName, clientId, Team.Red, false, payload.skillCode);
            return;
        }

        // if(password != payload.password) connectStatus = ConnectStatus.WRONG_PASSWORD;
        if (NetworkManager.Singleton.ConnectedClients.Count > GameInfo.max_player_count) connectStatus = ConnectStatus.SERVER_FULL;
        else connectStatus = ConnectStatus.SUCCESS;

        // とりあえず入れる
        callback(false, null, true, null, null);

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Networked /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // 接続結果をClientに送信
        ServerToClientConnectResult(clientId, connectStatus);

        if (connectStatus == ConnectStatus.SUCCESS)
        {
            // 固有番号を取得
            int? number = LobbyLinkedData.I.GetUnusedNumber();
            if (!number.HasValue)
            {
                Debug.LogError("人数が超過しています");
                return;
            }

            // Determine team from number.
            Team team = GameInfo.GetTeamFromNo((int)number);

            // ClientのデータはServer側で入室時に入れる
            LobbyParticipantData data = new LobbyParticipantData((int)number, payload.playerName, clientId, team, false, payload.skillCode);
            LobbyLinkedData.I.participantDatas.Add(data);
        }
        else
        {
            StartCoroutine(DisconnectClientDelayed(clientId));
        }
    }

    private void HandleOnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        // 
        // 
        // 
        // Debug.Log("Scene Load Complete");
    }

    private void HandleOnServerStarted()
    {

    }

    private void HandleOnClientConnected(ulong clientId)
    {
        RefreshAllFightersClientRpc(clientId);
    }

    [ClientRpc]
    void RefreshAllFightersClientRpc(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return;
        LobbyFighter.I.RefreshFighterPreparation();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Shutdown() ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void HandleOnClientDisconnect(ulong clientId)
    {
        DisableFighterClientRpc(clientId);
        if (NetworkManager.Singleton.IsHost)
        {
            LobbyLinkedData.I.DeleteParticipantData(clientId);
        }
        else
        {
            SceneManager2.I.LoadScene2(GameScenes.sortielobby);
        }
    }

    [ClientRpc]
    void DisableFighterClientRpc(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return;
        LobbyParticipantData data = LobbyLinkedData.I.GetParticipantDataByClientId(clientId).Value;
        LobbyFighter.I.DisableFighter(data.number);
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleOnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleOnClientDisconnect;
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            if (NetworkManager.Singleton.CustomMessagingManager != null) UnRegisterMessageHandlers();
            if (NetworkManager.Singleton.SceneManager != null) NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleOnSceneLoadComplete;
        }
    }



    public void RegisterMessageHandlers()
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



    public void ServerToClientConnectResult(ulong clientId, ConnectStatus status)
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



    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        RegisterMessageHandlers();
    }
}