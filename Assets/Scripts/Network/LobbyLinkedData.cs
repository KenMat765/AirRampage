using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;
using System.Linq;

public class LobbyLinkedData : NetworkSingleton<LobbyLinkedData>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    protected override void Awake()
    {
        base.Awake();

        // !! Initialize here !!
        participantDatas = new NetworkList<LobbyParticipantData>();
    }

    /// <summary>
    /// !! Index is NOT equal to fighter number !!
    /// </summary>
    public NetworkList<LobbyParticipantData> participantDatas; // !! DO NOT initialize NetworkList here, otherwise memory leak occurs on build !!
    public int participantCount { get { return participantDatas.Count; } }
    public bool participantDetermined { get; set; } = false;

    public override void OnNetworkSpawn()
    {
        Debug.Log("<color=yellow>OnNetworkSpawn</color>");
    }

    public void AddOnValueChangedAction(Action<NetworkListEvent<LobbyParticipantData>> action)
    {
        participantDatas.OnListChanged += (NetworkListEvent<LobbyParticipantData> listEvent) => action(listEvent);
    }

    public LobbyParticipantData? GetParticipantDataByClientId(ulong clientId)
    {
        foreach (LobbyParticipantData data in participantDatas)
        {
            if (data.clientId == clientId)
            {
                return data;
            }
        }
        Debug.LogWarning("Could not get participant data by clientId: " + clientId);
        return null;
    }

    public LobbyParticipantData? GetParticipantDataByNumber(int number)
    {
        foreach (LobbyParticipantData data in participantDatas)
        {
            if (data.number == number)
            {
                return data;
            }
        }
        Debug.LogWarning("Could not get participant data by number: " + number);
        return null;
    }

    public bool acceptDataChange { get; set; } = true;

    /// <summary>
    /// Modifiys properties of lobby data of client_id's. Only property which is not null is modified.
    /// Because RPC can not send nullable, put isReady and selectedTeam (bool) as int, and set default value to -1.
    /// </summary>
    void ModifyParticipantData(ulong client_id, int number = -1, int memberNo = -1, int isReady = -1, Team team = Team.NONE, int selectedTeam = -1, string skillCode = "")
    {
        LobbyParticipantData? nullable_data = GetParticipantDataByClientId(client_id);
        if (!nullable_data.HasValue)
        {
            return;
        }

        LobbyParticipantData current_data = nullable_data.Value;
        int new_number = number != -1 ? number : current_data.number;
        int new_memberNo = memberNo != -1 ? memberNo : current_data.memberNo;
        bool new_isReady = isReady != -1 ? Convert.ToBoolean(isReady) : current_data.isReady;
        Team new_team = team != Team.NONE ? team : current_data.team;
        bool new_selectedTeam = selectedTeam != -1 ? Convert.ToBoolean(selectedTeam) : current_data.selectedTeam;
        string new_skillCode = skillCode != "" ? skillCode : current_data.skillCode.Value;

        LobbyParticipantData new_data = new LobbyParticipantData(new_number, new_memberNo, current_data.name.Value, current_data.clientId,
            new_isReady, new_team, new_selectedTeam, current_data.isPlayer, new_skillCode, current_data.abilityCode.Value);
        int index = participantDatas.IndexOf(current_data);
        participantDatas[index] = new_data;
    }

    [ServerRpc(RequireOwnership = false)]
    void ModifyParticipantDataServerRpc(ulong client_id, int number = -1, int memberNo = -1, int isReady = -1, Team team = Team.NONE, int selectedTeam = -1, string skillCode = "")
    {
        if (!acceptDataChange)
        {
            return;
        }
        ModifyParticipantData(client_id, number, memberNo, isReady, team, selectedTeam, skillCode);
    }

    /// <summary>
    /// Interface for calling ModifyParticipantDataServerRpc.
    /// </summary>
    public void RequestServerModifyParticipantData(ulong client_id, int number = -1, int memberNo = -1, bool? isReady = null, Team team = Team.NONE, bool? selectedTeam = null, string skillCode = "")
    {
        int is_ready = isReady.HasValue ? Convert.ToInt16(isReady.Value) : -1;
        int selected_team = selectedTeam.HasValue ? Convert.ToInt16(selectedTeam.Value) : -1;
        ModifyParticipantDataServerRpc(client_id, number, memberNo, is_ready, team, selected_team, skillCode);
    }

    public void RemoveParticipantData(ulong clientId)
    {
        foreach (LobbyParticipantData data in participantDatas)
        {
            if (data.clientId == clientId)
            {
                participantDatas.Remove(data);
            }
        }
    }

    public bool IsEveryoneReady()
    {
        if (participantDatas.Count == 0)
        {
            return false;
        }
        foreach (LobbyParticipantData data in participantDatas)
        {
            if (!data.isReady) return false;
        }
        return true;
    }

    public bool TryGetNumber(Team team, int member_num, ref int number)
    {
        foreach (LobbyParticipantData lobby_data in participantDatas)
        {
            if (lobby_data.memberNo == member_num && lobby_data.team == team)
            {
                number = lobby_data.number;
                return true;
            }
        }
        return false;
    }

    public int GetTeamMemberCount(Team team)
    {
        int team_member_count = 0;
        foreach (LobbyParticipantData lobby_data in participantDatas)
        {
            Team player_team = lobby_data.team;
            if (team == player_team)
            {
                team_member_count++;
            }
        }
        return team_member_count;
    }

    public bool EveryoneSelectedTeamExceptHost()
    {
        foreach (LobbyParticipantData lobby_data in participantDatas)
        {
            // Skip host.
            if (lobby_data.clientId == NetworkManager.ServerClientId)
            {
                continue;
            }
            if (!lobby_data.selectedTeam)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Determines participants number & member_number, and generate AIs for absence. (Host only method)</summary>
    public void DetermineParticipants()
    {
        if (!IsHost)
        {
            Debug.LogWarning("Only the host can determine participants.");
            return;
        }

        // Determine players' number & member number.
        int member_number_red = 0;
        int member_number_blue = 0;
        for (int number = 0; number < participantCount; number++)
        {
            LobbyParticipantData cur_data = participantDatas[number];
            int member_number;
            if (cur_data.team == Team.RED)
            {
                member_number = member_number_red;
                member_number_red++;
            }
            else
            {
                member_number = member_number_blue;
                member_number_blue++;
            }
            ModifyParticipantData(cur_data.clientId, number: number, memberNo: member_number);
        }

        // Generate AI lobby data for absences.
        string[] ai_names = AiUtilities.aiNames.RandomizeOrder().ToArray();
        for (int number = participantCount; number < GameInfo.MAX_PLAYER_COUNT; number++)
        {
            Team ai_team;
            int ai_member_num;
            if (member_number_red < GameInfo.TEAM_MEMBER_COUNT)
            {
                ai_team = Team.RED;
                ai_member_num = member_number_red;
                member_number_red++;
            }
            else
            {
                ai_team = Team.BLUE;
                ai_member_num = member_number_blue;
                member_number_blue++;
            }
            LobbyParticipantData ai_lobby_data = AiUtilities.GenerateAILobbyData(number, ai_member_num, ai_names[number], ai_team);
            participantDatas.Add(ai_lobby_data);
        }

        // Set participantsDetermined to true.
        participantDetermined = true;
    }
}



public struct LobbyParticipantData : INetworkSerializable, IEquatable<LobbyParticipantData>
{
    public int number;
    public int memberNo;
    public FixedString32Bytes name;
    public ulong clientId;
    public bool isReady;
    public Team team;
    public bool selectedTeam;
    public bool isPlayer;
    public FixedString32Bytes skillCode;
    public FixedString32Bytes abilityCode;

    public LobbyParticipantData(int number, int memberNo, string name, ulong clientId, bool isReady, Team team, bool selectedTeam, bool isPlayer, string skillCode, string abilityCode)
    {
        this.number = number;
        this.memberNo = memberNo;
        this.name = name;
        this.clientId = clientId;
        this.isReady = isReady;
        this.team = team;
        this.selectedTeam = selectedTeam;
        this.isPlayer = isPlayer;
        this.skillCode = skillCode;
        this.abilityCode = abilityCode;
    }

    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref number);
        serializer.SerializeValue(ref memberNo);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref isReady);
        serializer.SerializeValue(ref team);
        serializer.SerializeValue(ref selectedTeam);
        serializer.SerializeValue(ref isPlayer);
        serializer.SerializeValue(ref skillCode);
        serializer.SerializeValue(ref abilityCode);
    }

    bool IEquatable<LobbyParticipantData>.Equals(LobbyParticipantData other)
    {
        return number == other.number &&
        memberNo == other.memberNo &&
        name.Equals(other.name) &&
        clientId.Equals(other.clientId) &&
        isReady == other.isReady &&
        team == other.team &&
        selectedTeam == other.selectedTeam &&
        isPlayer == other.isPlayer &&
        skillCode.Equals(other.skillCode) &&
        abilityCode.Equals(other.abilityCode);
    }

    // 1-2/10-5/n-n... : skillId-Level/skillId-Level/...
    public static void SkillCodeEncoder(int?[] skillIds, int?[] skillLevels, out string skillCode)
    {
        skillCode = "";
        for (int k = 0; k < GameInfo.MAX_SKILL_COUNT; k++)
        {
            if (skillIds[k].HasValue)
            {
                skillCode += skillIds[k].ToString() + "-";
                skillCode += skillLevels[k].ToString() + "/";
            }
            else
            {
                skillCode += "n-n/";
            }
        }
    }

    // 1-2/10-5/n-n... : skillId-Level/skillId-Level/...
    public static void SkillCodeDecoder(string skillCode, out int?[] skillIds, out int?[] skillLevels)
    {
        skillIds = new int?[GameInfo.MAX_SKILL_COUNT];
        skillLevels = new int?[GameInfo.MAX_SKILL_COUNT];
        int skillNumber = 0;
        bool decodingSkillId = true;
        string skillId_cashe = "";
        for (int k = 0; k < skillCode.Length; k++)
        {
            if (skillCode[k] == '-')
            {
                decodingSkillId = false;
                if (skillId_cashe == "n")
                {
                    skillIds[skillNumber] = null;
                }
                else
                {
                    skillIds[skillNumber] = int.Parse(skillId_cashe);
                }
                skillId_cashe = "";
            }
            else if (skillCode[k] == '/')
            {
                decodingSkillId = true;
                skillNumber++;
            }
            else
            {
                if (decodingSkillId)
                {
                    if (skillCode[k] == 'n')
                    {
                        skillId_cashe += "n";
                    }
                    else
                    {
                        skillId_cashe += skillCode[k].ToString();
                    }
                }
                else
                {
                    if (skillCode[k] == 'n')
                    {
                        skillLevels[skillNumber] = null;
                    }
                    else
                    {
                        skillLevels[skillNumber] = int.Parse(skillCode[k].ToString());
                    }
                }
            }
        }
    }

    // 1/10/... : abilityId1/abilityId2/...
    public static void AbilityCodeEncoder(List<int> abilityIds, out string abilityCode)
    {
        abilityCode = "";
        foreach (int id in abilityIds)
        {
            abilityCode += id.ToString() + "/";
        }
    }

    // 1/10/... : abilityId1/abilityId2/...
    public static void AbilityCodeDecoder(string abilityCode, out List<int> abilityIds)
    {
        abilityIds = new List<int>();
        string abilityId_cashe = "";
        for (int k = 0; k < abilityCode.Length; k++)
        {
            char code = abilityCode[k];
            if (code == '/')
            {
                int abilityId = int.Parse(abilityId_cashe);
                abilityIds.Add(abilityId);
                abilityId_cashe = "";
            }
            else
            {
                abilityId_cashe += code;
            }
        }
    }
}