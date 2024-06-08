using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using NaughtyAttributes;

public class ParamMonitor : MonoBehaviour
{
    [BoxGroup("NetworkManager")] public ulong clientId;
    [BoxGroup("NetworkManager")] public int clientCount;

    [BoxGroup("BattleInfo")] public bool isMulti;
    [BoxGroup("BattleInfo")] public bool isHost;
    [BoxGroup("BattleInfo")] public int playerCount;
    [BoxGroup("BattleInfo")] public Rule rule;
    [BoxGroup("BattleInfo")] public Stage stage;
    [BoxGroup("BattleInfo")] public int time_sec;
    [BoxGroup("BattleInfo")] public BattleInfo.ParticipantBattleData[] battleDatas;
    [BoxGroup("BattleInfo")] public Dictionary<int, (Team team, int memberNo)> team_memberNum;

    [BoxGroup("SortieLobbyManager")] public int myDeckNum;
    [BoxGroup("SortieLobbyManager")] public int myNumber;
    [BoxGroup("SortieLobbyManager")] public string myName;
    [BoxGroup("SortieLobbyManager")] public ulong myClientId;
    [BoxGroup("SortieLobbyManager")] public Team myTeam;
    [BoxGroup("SortieLobbyManager")] public string myAbilityCode;

    [BoxGroup("SortieLobbyUI")] public SortieLobbyUI.Page page;

    [BoxGroup("LobbyLinkedData")] public int lobbyDataCount;

    void Update()
    {
        // NetworkManager
        if (NetworkManager.Singleton)
        {
            if (BattleInfo.isHost)
            {
                clientId = NetworkManager.Singleton.LocalClientId;
                clientCount = NetworkManager.Singleton.ConnectedClients.Count;
            }
        }

        // BattleInfo
        isMulti = BattleInfo.isMulti;
        isHost = BattleInfo.isHost;
        playerCount = BattleInfo.playerCount;
        rule = BattleInfo.rule;
        stage = BattleInfo.stage;
        time_sec = BattleInfo.time_sec;
        battleDatas = BattleInfo.battleDatas;

        // SortieLobbyManager
        myDeckNum = SortieLobbyManager.myDeckNum;
        myNumber = SortieLobbyManager.myNumber;
        myName = SortieLobbyManager.myName;
        myClientId = SortieLobbyManager.myClientId;
        myTeam = SortieLobbyManager.myTeam;
        myAbilityCode = SortieLobbyManager.myAbilityCode;

        // SortieLobbyUI
        page = SortieLobbyUI.page;

        // LobbyLinkedData
        lobbyDataCount = LobbyLinkedData.I.participantDatas.Count;
    }
}
