using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;

public class LobbyLinkedData : NetworkBehaviour
{
    static LobbyLinkedData instance;
    public static LobbyLinkedData I
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<LobbyLinkedData>();
                if(instance == null) {instance = new GameObject(typeof(LobbyLinkedData).ToString()).AddComponent<LobbyLinkedData>();}
            }
            return instance;
        }
    }

    void Awake()
    {
        if(this != I)
        {
            Destroy(this.gameObject);
            return;
        }
        participantDatas = new NetworkList<LobbyParticipantData>();
    }



    public NetworkList<LobbyParticipantData> participantDatas;

    public override void OnNetworkSpawn()
    {
        // Hostはここでparticipant dataを入れる
        if(IsHost) participantDatas.Add(GameNetPortal.I.hostData);
    }

    public void AddOnValueChangedAction(Action<NetworkListEvent<LobbyParticipantData>> action)
    {
        participantDatas.OnListChanged += (NetworkListEvent<LobbyParticipantData> listEvent) => action(listEvent);
    }

    public LobbyParticipantData? GetParticipantDataByClientId(ulong clientId)
    {
        foreach(LobbyParticipantData data in participantDatas)
        {
            if(data.clientId == clientId)
            {
                return data;
            }
        }
        return null;
    }

    public LobbyParticipantData? GetParticipantDataByNo(int number)
    {
        foreach(LobbyParticipantData data in participantDatas)
        {
            if(data.number == number)
            {
                return data;
            }
        }
        return null;
    }

    public int? GetUnusedNumber()
    {
        for(int k = 0; k < GameInfo.max_player_count; k++)
        {
            int counter = 0;
            foreach(LobbyParticipantData data in participantDatas)
            {
                if(data.number == k) break;
                else counter ++;
            }
            if(counter == participantDatas.Count) return k;
        }
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetParticipantDataServerRpc(ulong clientId, LobbyParticipantData inputData)
    {
        int? dataIndex = null;
        foreach(LobbyParticipantData data in participantDatas)
        {
            if(data.clientId == clientId)
            {
                dataIndex = participantDatas.IndexOf(data);
                break;
            }
        }
        if(dataIndex.HasValue)
        {
            participantDatas[(int)dataIndex] = inputData;
        }
        else
        {
            Debug.LogError("clientIdに対応するデータが見つかりません");
        }
    }

    public void DeleteParticipantData(ulong clientId)
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
        foreach(LobbyParticipantData data in participantDatas)
        {
            if(!data.isReady) return false;
        }
        return true;
    }

    public int ParticipantCount(Team? team = null)
    {
        int count = 0;
        if (!team.HasValue)
        {
            count = participantDatas.Count;
        }
        else
        {
            foreach(LobbyParticipantData data in participantDatas)
            {
                if(data.team == team)
                {
                    count ++;
                }
            }
        }
        return count;
    }

    public override void OnDestroy()
    {
        if(participantDatas == null) participantDatas = new NetworkList<LobbyParticipantData>();
        base.OnDestroy();
    }
}



public struct LobbyParticipantData : INetworkSerializable, IEquatable<LobbyParticipantData>
{
    public int number;
    public FixedString32Bytes name;
    public ulong clientId;
    public Team team;
    public bool isReady;
    public FixedString32Bytes skillCode;

    public LobbyParticipantData(int number, FixedString32Bytes name, ulong clientId, Team team, bool isReady, FixedString32Bytes skillCode)
    {
        this.number = number;
        this.name = name;
        this.clientId = clientId;
        this.team = team;
        this.isReady = isReady;
        this.skillCode = skillCode;
    }

    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref number);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref team);
        serializer.SerializeValue(ref isReady);
        serializer.SerializeValue(ref skillCode);
    }

    bool IEquatable<LobbyParticipantData>.Equals(LobbyParticipantData other)
    {
        return number == other.number &&
        name.Equals(other.name) &&
        clientId.Equals(other.clientId) &&
        team.Equals(other.team) &&
        isReady == other.isReady &&
        skillCode.Equals(other.skillCode);
    }

    // 1-2/10-5/n-n... : skillId-Level/skillId-Level/...
    public static void SkillCodeEncoder(int?[] skillIds, int?[] skillLevels, out string skillCode)
    {
        skillCode = "";
        for(int k = 0; k < GameInfo.max_skill_count; k++)
        {
            if(skillIds[k].HasValue)
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
        skillIds = new int?[GameInfo.max_skill_count];
        skillLevels = new int?[GameInfo.max_skill_count];
        int skillNumber = 0;
        bool decodingSkillId = true;
        string skillId_cashe = "";
        for(int k = 0; k < skillCode.Length; k++)
        {
            if(skillCode[k] == '-')
            {
                decodingSkillId = false;
                if(skillId_cashe == "n")
                {
                    skillIds[skillNumber] = null;
                }
                else
                {
                    skillIds[skillNumber] = int.Parse(skillId_cashe);
                }
                skillId_cashe = "";
            }
            else if(skillCode[k] == '/')
            {
                decodingSkillId = true;
                skillNumber ++;
            }
            else
            {
                if(decodingSkillId)
                {
                    if(skillCode[k] == 'n')
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
                    if(skillCode[k] == 'n')
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
}