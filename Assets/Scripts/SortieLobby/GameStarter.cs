using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;
using System.Linq;

public class SortieLobbyManager : NetworkBehaviour
{
    static SortieLobbyManager instance;
    public static SortieLobbyManager I
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SortieLobbyManager>();
                if (instance == null) { instance = new GameObject(typeof(SortieLobbyManager).ToString()).AddComponent<SortieLobbyManager>(); }
            }
            return instance;
        }
    }
    void Awake()
    {
        if (this != I)
        {
            Destroy(this.gameObject);
            return;
        }
    }

    void Start()
    {
        if (BattleInfo.isMulti)
        {
            // When new participant data was added.
            LobbyLinkedData.I.AddOnValueChangedAction((NetworkListEvent<LobbyParticipantData> listEvent) =>
            {
                if (!IsHost) return;
                if (GameNetPortal.I.gameStarted) return;
                if (LobbyLinkedData.I.participantCount < 2) return;
                if (!LobbyLinkedData.I.IsEveryoneReady()) return;
                MultiGameStarterClientRpc(BattleInfo.rule);
            });
        }

        else
        {
            // If solo play, generate AI at start.
            FillBattleDataWithAis();
        }
    }



    // LobbyParticipantData => ParticipantBattleData //////////////////////////////////////////////////////////////////
    BattleInfo.ParticipantBattleData Lobby2BattleData(LobbyParticipantData lobby_data, bool is_player)
    {
        int number = lobby_data.number;
        ulong client_id = lobby_data.clientId;
        string name = lobby_data.name.Value;
        Team team = lobby_data.team;
        int?[] skillIds, skillLevels;
        List<int> abilityIds = new List<int>();
        LobbyParticipantData.SkillCodeDecoder(lobby_data.skillCode.ToString(), out skillIds, out skillLevels);
        LobbyParticipantData.AbilityCodeDecoder(lobby_data.abilityCode.ToString(), out abilityIds);
        BattleInfo.ParticipantBattleData battle_data = new BattleInfo.ParticipantBattleData
            (number, is_player, client_id, name, team, skillIds, skillLevels, abilityIds); ;
        return battle_data;
    }



    // Fill empty BattleInfo.battleDatas with AI battle data.
    List<BattleInfo.ParticipantBattleData> FillBattleDataWithAis()
    {
        List<BattleInfo.ParticipantBattleData> battleDatas = new List<BattleInfo.ParticipantBattleData>();
        List<int> occupied_no = BattleInfo.battleDatas.Select(b => b.fighterNo).ToList();
        int red_count = BattleInfo.battleDatas.Count(b => b.team == Team.RED);
        int blue_count = BattleInfo.battleDatas.Count(b => b.team == Team.BLUE);
        string[] aiNames = AiUtilities.aiNames.RandomizeOrder().ToArray();
        for (int k = 0; k < GameInfo.max_player_count; k++)
        {
            if (occupied_no.Contains(k))
            {
                continue;
            }

            BattleInfo.ParticipantBattleData battleData;
            int aiNo = k;
            string aiName = aiNames[k];
            Team aiTeam = Team.NONE;
            if (red_count < GameInfo.team_member_count)
            {
                aiTeam = Team.RED;
                red_count++;
            }
            else if (blue_count < GameInfo.team_member_count)
            {
                aiTeam = Team.BLUE;
                blue_count++;
            }
            if (aiTeam == Team.NONE)
            {
                Debug.LogError("team was NONE!!");
                break;
            }
            battleData = AiUtilities.GenerateAI(aiNo, aiName, aiTeam);
            BattleInfo.battleDatas[aiNo] = battleData;
            battleDatas.Add(battleData);
        }
        return battleDatas;
    }



    // Solo ///////////////////////////////////////////////////////////////////////////////////////////////////
    // Only the team & skill deck number changes in sortie lobby.
    public void SoloGameStarter(Team myTeam, int deck_num) => StartCoroutine(soloGameStarter(myTeam, deck_num));
    IEnumerator soloGameStarter(Team myTeam, int deck_num)
    {
        // Set player's battle data to BattleInfo.
        string myName = PlayerInfo.I.myName;
        int?[] skillIds, skillLevels;
        PlayerInfo.I.SkillIdsGetter(deck_num, out skillIds);
        PlayerInfo.I.SkillLevelsGetter(deck_num, out skillLevels);
        List<int> abilityIds = PlayerInfo.I.AbilityIdsGetter();
        BattleInfo.ParticipantBattleData battleData = new BattleInfo.ParticipantBattleData(0, true, null, myName, myTeam, skillIds, skillLevels, abilityIds);
        BattleInfo.battleDatas[0] = battleData;

        // Set AI's battle data to BattleInfo.
        List<BattleInfo.ParticipantBattleData> ai_battleDatas = FillBattleDataWithAis();
        List<BattleInfo.ParticipantBattleData> ai_teamMates = ai_battleDatas.Where(b => b.team == myTeam).ToList();

        // Update sortie lobby UI and wait for few seconds.
        for (int blockId = 1; blockId < GameInfo.team_member_count; blockId++)
        {
            BattleInfo.ParticipantBattleData ai_battleData = ai_teamMates[blockId - 1];
            SortieLobbyUI.I.NameUISetter(blockId, ai_battleData.name);
            SortieLobbyUI.I.SkillUISetter(blockId, ai_battleData.skillIds);
        }
        yield return new WaitForSeconds(1.5f);

        // Sortie all fighters & load scene.
        LobbyFighter.I.SortieAllFighters(myTeam, () =>
        {
            DOVirtual.DelayedCall(1, () =>
            {
                SceneManager2.I.LoadSceneAsync2(BattleInfo.stage, FadeType.left);
            }).Play();
        });
    }



    // Multi //////////////////////////////////////////////////////////////////////////////////////////////////
    [ClientRpc]
    void MultiGameStarterClientRpc(Rule rule) => StartCoroutine(MultiGameStarter(rule));

    IEnumerator MultiGameStarter(Rule rule)
    {
        GameNetPortal.I.gameStarted = true;

        // Must wait for few seconds, otherwise clients cannot receive first AI's battle data. (unexplained)
        yield return new WaitForSeconds(0.5f);

        // If not host, set battle information to BattleInfo. (Host has already set rule by himself.)
        if (!IsHost)
        {
            BattleInfo.rule = rule;
        }

        // Get & Define necessary variables.
        LobbyParticipantData myData = (LobbyParticipantData)LobbyLinkedData.I.GetParticipantDataByClientId(NetworkManager.Singleton.LocalClientId);
        myTeam = myData.team;
        int redAICounter = 0, blueAICounter = 0;

        // Initialize BattleInfo.playerCount.
        BattleInfo.playerCount = 0;

        // Set battle data to BattleInfo.
        for (int no = 0; no < GameInfo.max_player_count; no++)
        {
            // Define necessary variables.
            int?[] skillIds, skillLevels;
            List<int> abilityIds = new List<int>();

            // Check if there is a player at fighterNo : {no}.
            LobbyParticipantData? data_nullable = LobbyLinkedData.I.GetParticipantDataByNo(no);

            // If there is a player.
            if (data_nullable.HasValue)
            {
                LobbyParticipantData data = (LobbyParticipantData)data_nullable;
                LobbyParticipantData.SkillCodeDecoder(data.skillCode.ToString(), out skillIds, out skillLevels);
                LobbyParticipantData.AbilityCodeDecoder(data.abilityCode.ToString(), out abilityIds);
                BattleInfo.battleDatas[no] = new BattleInfo.ParticipantBattleData(no, true, data.clientId, data.name.ToString(), data.team, skillIds, skillLevels, abilityIds);

                // Plus player counter in BattleInfo.
                BattleInfo.playerCount++;
            }

            // If there is no player.
            else
            {
                // Only the host creates AIs.
                if (IsHost)
                {
                    // Create AI battle data.
                    string aiName;
                    Team aiTeam;
                    string aiSkillCode;
                    string aiAbilityCode;

                    // Determine AI team.
                    aiTeam = GameInfo.GetTeamFromNo(no);

                    // Determine AI name.
                    if (aiTeam == Team.RED)
                    {
                        redAICounter++;
                        aiName = "AIRed" + redAICounter.ToString();
                    }

                    // Generate AI name & team. (Blue Team)
                    else
                    {
                        blueAICounter++;
                        aiName = "AIBlue" + blueAICounter.ToString();
                    }

                    // Generate AI skills.
                    LobbyAiSkillGenerator.I.GenerateSkills(out skillIds, out skillLevels);
                    LobbyParticipantData.SkillCodeEncoder(skillIds, skillLevels, out aiSkillCode);

                    // Generate AI abilities.
                    abilityIds = AbilityUtilities.RandomSelect().Select(a => AbilityDatabase.I.GetIdFromAbility(a)).ToList();
                    LobbyParticipantData.AbilityCodeEncoder(abilityIds, out aiAbilityCode);

                    // Send the generated battle data to clients (including host).
                    SendAIBattleDataClientRpc(no, aiName, aiTeam, aiSkillCode, aiAbilityCode);

                    // Host requests to all clients to show the name block of this AI.
                    // Do not execute these for enemy AIs.
                    int aiBlockNo = GameInfo.GetBlockNoFromNo(no);
                    NameUIUpdateRequestClientRpc(no, aiBlockNo, aiName);
                    SkillUIUpdateRequestClientRpc(no, aiBlockNo, aiSkillCode);
                }

                // Do not execute these for enemy AIs.
                if (GameInfo.GetTeamFromNo(no) != myTeam) continue;

                // Prepare this AI fighter at each clients.
                LobbyFighter.I.PrepareFighter(no);
            }
        }

        yield return new WaitForSeconds(1.5f);

        bool sortied = false;
        float fadeout_duration = 0;
        LobbyFighter.I.SortieAllFighters(myTeam, () =>
        {
            sortied = true;
            DOVirtual.DelayedCall(1, () => fadeout_duration = FadeCanvas.I.FadeOut(FadeType.left)).Play();
            DOVirtual.DelayedCall(1 + fadeout_duration, () => FadeCanvas.I.StartBlink()).Play();
        });

        if (IsHost)
        {
            yield return new WaitUntil(() => sortied);

            yield return new WaitForSeconds(1 + fadeout_duration);

            // Stage Fallback : Space
            string stage = "Space";
            switch (BattleInfo.stage)
            {
                case global::Stage.CANYON: stage = "Canyon"; break;
                case global::Stage.SPACE: stage = "Space"; break;
                case global::Stage.SNOWPEAK: stage = "SnowPeak"; break;
            }
            NetworkManager.SceneManager.LoadScene(stage, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }


    // Receive battle data from the host & Set battle data at client side.
    [ClientRpc]
    void SendAIBattleDataClientRpc(int no, string aiName, Team team, string skillCode, string abilityCode)
    {
        int?[] skillIds, skillLevels;
        LobbyParticipantData.SkillCodeDecoder(skillCode, out skillIds, out skillLevels);
        List<int> abilityIds = new List<int>();
        LobbyParticipantData.AbilityCodeDecoder(abilityCode, out abilityIds);
        BattleInfo.battleDatas[no] = new BattleInfo.ParticipantBattleData(no, false, null, aiName, team, skillIds, skillLevels, abilityIds);
    }


    // Used for filtering in client rpcs.
    Team myTeam;

    // As SortieLobbyUI cannot call RPC, code here.
    // Called at both host and client.
    [ClientRpc]
    void SkillUIUpdateRequestClientRpc(int fighterNo, int deck_id, string skillCode)
    {
        // Do not read if enemy team.
        if (GameInfo.GetTeamFromNo(fighterNo) != myTeam) return;
        int?[] skillIds, skillLevels;
        LobbyParticipantData.SkillCodeDecoder(skillCode, out skillIds, out skillLevels);
        for (int k = 0; k < GameInfo.max_skill_count; k++)
        {
            if (skillIds[k].HasValue)
            {
                int skillId = (int)skillIds[k];
                SkillData skillData = SkillDatabase.I.SearchSkillById(skillId);
                SortieLobbyUI.I.SkillUISetter(deck_id, k, skillData.GetSprite(), Color.white, skillData.GetColor());
            }
            else
            {
                SortieLobbyUI.I.SkillUISetter(deck_id, k, null, Color.clear, Color.white);
            }
        }
    }

    // As SortieLobbyUI cannot call RPC, code here.
    // Called at both host and client.
    [ClientRpc]
    void NameUIUpdateRequestClientRpc(int fighterNo, int blockNo, string name)
    {
        // Do not read if enemy team.
        if (GameInfo.GetTeamFromNo(fighterNo) != myTeam) return;
        LobbyFighter.I.NameFighter(fighterNo, name);
        SortieLobbyUI.I.NameUISetter(blockNo, name);
    }

}