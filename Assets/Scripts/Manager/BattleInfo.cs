using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BattleInfo
{
    // オンラインかどうか
    public static bool isMulti {get; set;}
    public static bool isHost {get; set;}

    // ルール

    // ステージ


    // Participantsの基本情報をまとめて格納する
    public struct ParticipantBattleData
    {
        public int fighterNo;
        public bool isPlayer;
        public ulong? clientId;
        public string name;
        public Team team;
        public int?[] skillIds;
        public int?[] skillLevels;
        public ParticipantBattleData(int fighterNo, bool isPlayer, ulong? clientId, string name, Team team, int?[] skillIds, int?[] skillLevels)
        {
            this.fighterNo = fighterNo;
            this.isPlayer = isPlayer;
            this.clientId = clientId;
            this.name = name;
            this.team = team;
            this.skillIds = skillIds;
            this.skillLevels = skillLevels;
        }
    }
    public static ParticipantBattleData[] battleDatas {get; set;} = new ParticipantBattleData[GameInfo.max_player_count];
    public static ParticipantBattleData? GetBattleDataByFighterNo(int fighterNo)
    {
        foreach(ParticipantBattleData battleData in battleDatas)
        {
            if(battleData.fighterNo == fighterNo) return battleData;
        }
        return null;
    }
    public static ParticipantBattleData? GetBattleDataByClientId(ulong clientId)
    {
        foreach(ParticipantBattleData battleData in battleDatas)
        {
            if(battleData.clientId == clientId) return battleData;
        }
        return null;
    }
}
