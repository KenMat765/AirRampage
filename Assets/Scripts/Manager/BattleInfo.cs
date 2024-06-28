using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BattleInfo
{
    // オンラインかどうか
    public static bool isMulti { get; set; }
    public static bool isHost { get; set; }

    // オンライン時、何人のプレイヤーがいるか
    public static int playerCount { get; set; }

    // ルール
    public static Rule rule;

    // ステージ
    public static Stage stage;

    // タイム
    public static int time_sec;


    // Participantsの基本情報をまとめて格納する
    public struct ParticipantBattleData
    {
        public int fighterNo;
        public int memberNo;
        public bool isPlayer;
        public ulong? clientId;
        public string name;
        public Team team;
        public int?[] skillIds;
        public int?[] skillLevels;
        public List<int> abilities;
        public ParticipantBattleData(int fighterNo, int memberNo, bool isPlayer, ulong? clientId, string name, Team team, int?[] skillIds, int?[] skillLevels, List<int> abilities)
        {
            this.fighterNo = fighterNo;
            this.memberNo = memberNo;
            this.isPlayer = isPlayer;
            this.clientId = clientId;
            this.name = name;
            this.team = team;
            this.skillIds = skillIds;
            this.skillLevels = skillLevels;
            this.abilities = abilities;
        }
    }
    public static ParticipantBattleData[] battleDatas { get; set; } = new ParticipantBattleData[GameInfo.MAX_PLAYER_COUNT];
    public static ParticipantBattleData? GetBattleDataByFighterNo(int fighterNo)
    {
        foreach (ParticipantBattleData battleData in battleDatas)
        {
            if (battleData.fighterNo == fighterNo) return battleData;
        }
        return null;
    }
}
