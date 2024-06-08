using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

public class SortieLobbyManager : NetworkSingleton<SortieLobbyManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;


    // Skill deck number selected.
    public static int myDeckNum = 0;


    // === CONSTANT LobbyParticipantData properties === //
    public static int myNumber = 0;         // my number is 0 when solo
    public static int myMemberNumber = 0;   // my member number is 0 when solo
    public static string myName { get { return PlayerInfo.I.myName; } }
    public static ulong myClientId;
    public static Team myTeam = Team.RED;   // my team is RED when solo
    public static string myAbilityCode;
    public static void Init()
    {
        myClientId = NetworkManager.Singleton.LocalClientId;
        LobbyParticipantData myData = (LobbyParticipantData)LobbyLinkedData.I.GetParticipantDataByClientId(myClientId);
        myNumber = myData.number;
        myMemberNumber = myData.memberNo;
        myTeam = myData.team;
        myAbilityCode = myData.abilityCode.ToString();
    }


    void Start()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            // When participant pressed "Ready for battle" button, lobby_data's "isReady" toggles and this method is called.
            LobbyLinkedData.I.AddOnValueChangedAction((NetworkListEvent<LobbyParticipantData> listEvent) =>
            {
                // Start game when everyone is ready.
                if (LobbyLinkedData.I.IsEveryoneReady())
                {
                    LobbyLinkedData.I.acceptDataChange = false;
                    GameStarterClientRpc(BattleInfo.rule, BattleInfo.stage, BattleInfo.time_sec);
                }
            });
        }

        // Prepare all fighters.
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
        SortieLobbyUI.I.ExitMenuReturn().Play();

        // Sortie all fighters and fade out.
        bool sortied = false;
        float fadeout_duration = 0;
        LobbyFighter.I.SortieAllFighters(myTeam, () =>
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
