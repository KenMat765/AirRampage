using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class SortieLobbyManager : NetworkSingleton<SortieLobbyManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    // Skill deck number selected.
    public int myDeckNum { get; set; } = 0;

    // Cache of your own lobby data.
    public LobbyParticipantData myData { get; private set; }


    void Start()
    {
        // When participant pressed "Ready for battle" button, lobby_data's "isReady" toggles and this method is called.
        LobbyLinkedData.I.AddOnValueChangedAction((NetworkListEvent<LobbyParticipantData> listEvent) =>
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // Start game when everyone is ready.
                if (LobbyLinkedData.I.IsEveryoneReady())
                {
                    LobbyLinkedData.I.acceptDataChange = false;
                    GameStarterClientRpc(BattleInfo.rule, BattleInfo.stage, BattleInfo.time_sec);
                }
            }
        });
    }



    // Participant Determined /////////////////////////////////////////////////////////////////////////////////////////
    public async void OnParticipantDetermined()
    {
        // Only the host can determine the participants.
        if (!IsHost)
        {
            Debug.LogError("Only the host can determine participants.");
            return;
        }

        // Kill Lobby.
        LobbyLinkedData.I.acceptDataChange = false;
        await GameNetPortal.I.KillJoinedLobby();

        // Determine participants.
        LobbyLinkedData.I.DetermineParticipants();

        // Wait for few seconds for LobbyLinkedData to synchronize.
        int wait_milisec = 1000;
        await UniTask.Delay(wait_milisec);

        // Callback called on every participants.
        OnParticipantDeterminedClientRpc();
    }

    [ClientRpc]
    void OnParticipantDeterminedClientRpc()
    {
        // Cache your own lobby data to SortieLobbyManager & Change page.
        ulong client_id = NetworkManager.Singleton.LocalClientId;
        myData = (LobbyParticipantData)LobbyLinkedData.I.GetParticipantDataByClientId(client_id);

        // Change the page of OnlineLobbyUI.
        if (IsHost)
        {
            OnlineLobbyUI.I.SetPage(OnlineLobbyUI.Page.RULE);
        }
        else
        {
            OnlineLobbyUI.I.SetPage(OnlineLobbyUI.Page.PARTICIPANT);
        }

        // Prepare all fighters.
        Team myTeam = myData.team;
        for (int m_num = 0; m_num < GameInfo.team_member_count; m_num++)
        {
            int number = -1;
            if (LobbyLinkedData.I.TryGetNumber(myTeam, m_num, ref number))
            {
                string name = LobbyLinkedData.I.GetParticipantDataByNumber(number).Value.name.Value;
                LobbyFighter.I.NameFighter(myTeam, m_num, name);
            }
        }
        LobbyFighter.I.PrepareAllFighters(myTeam);
    }



    // LobbyParticipantData -> ParticipantBattleData //////////////////////////////////////////////////////////////////
    BattleInfo.ParticipantBattleData ConvertLobby2Battle(LobbyParticipantData lobby_data)
    {
        int number = lobby_data.number;
        int member_number = lobby_data.memberNo;
        ulong client_id = lobby_data.clientId;
        string name = lobby_data.name.Value;
        Team team = lobby_data.team;
        bool is_player = lobby_data.isPlayer;
        int?[] skillIds, skillLevels;
        List<int> abilityIds = new List<int>();
        LobbyParticipantData.SkillCodeDecoder(lobby_data.skillCode.ToString(), out skillIds, out skillLevels);
        LobbyParticipantData.AbilityCodeDecoder(lobby_data.abilityCode.ToString(), out abilityIds);
        BattleInfo.ParticipantBattleData battle_data = new BattleInfo.ParticipantBattleData
            (number, member_number, is_player, client_id, name, team, skillIds, skillLevels, abilityIds); ;
        return battle_data;
    }



    // Game Starter ///////////////////////////////////////////////////////////////////////////////////////////////////
    // Automaticaly called when every participant is ready. (Called on every participants)
    [ClientRpc]
    void GameStarterClientRpc(Rule rule, Stage stage, int time_sec) => StartCoroutine(GameStarter(rule, stage, time_sec));

    IEnumerator GameStarter(Rule rule, Stage stage, int time_sec)
    {
        // Must wait for few seconds, otherwise clients cannot receive first AI's battle data. (unexplained)
        yield return new WaitForSeconds(0.5f);

        // If client, set battle information to BattleInfo. (Host has already set rule by himself.)
        if (!NetworkManager.Singleton.IsHost)
        {
            BattleInfo.rule = rule;
            BattleInfo.stage = stage;
            BattleInfo.time_sec = time_sec;
        }

        // Set battle data to BattleInfo.
        BattleInfo.playerCount = 0;
        foreach (LobbyParticipantData lobby_data in LobbyLinkedData.I.participantDatas)
        {
            int number = lobby_data.number;
            BattleInfo.ParticipantBattleData battle_data = ConvertLobby2Battle(lobby_data);
            BattleInfo.battleDatas[number] = battle_data;
            if (lobby_data.isPlayer)
            {
                BattleInfo.playerCount++;
            }
        }

        // Exit menu & return button UI.
        OnlineLobbyUI.I.ExitMenuReturn().Play();

        // Sortie all fighters and fade out.
        bool sortied = false;
        float fadeout_duration = 0;
        LobbyFighter.I.SortieAllFighters(myData.team, () =>
        {
            sortied = true;
            DOVirtual.DelayedCall(1, () => fadeout_duration = FadeCanvas.I.FadeOut(FadeType.left)).Play();
            DOVirtual.DelayedCall(1 + fadeout_duration, () => FadeCanvas.I.StartBlink()).Play();
        });

        // Only the host changes the scene (this will change all clients scenes too)
        if (NetworkManager.Singleton.IsHost)
        {
            yield return new WaitUntil(() => sortied);
            yield return new WaitForSeconds(1 + fadeout_duration);

            string stage_str;
            switch (BattleInfo.stage)
            {
                case global::Stage.CANYON: stage_str = "Canyon"; break;
                case global::Stage.SPACE: stage_str = "Space"; break;
                case global::Stage.SNOWPEAK: stage_str = "SnowPeak"; break;
                default: stage_str = "Space"; break;    // Fallback : Space
            }
            NetworkManager.SceneManager.LoadScene(stage_str, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
