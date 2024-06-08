using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AiUtilities
{
    // Set AI's client id to 999.
    public const ulong aiClientId = 999;

    public static string[] aiNames =
    {
        "Johnny",
        "Michael",
        "Jennifer",
        "David",
        "James",
        "Robert",
        "Mary",
        "Sarah",
        "Emily",
        "Elizabeth",
    };

    // Used in multi player. (Converted to battle data in SortieLobbyManager when game starts)
    public static LobbyParticipantData GenerateAILobbyData(int aiNo, int aiMemberNo, string aiName, Team aiTeam)
    {
        // Set random skills.
        int?[] aiSkillIds, aiSkillLevels;
        string aiSkillCode;
        SkillUtilities.GenerateSkills(out aiSkillIds, out aiSkillLevels);
        LobbyParticipantData.SkillCodeEncoder(aiSkillIds, aiSkillLevels, out aiSkillCode);

        // Set random abilities.
        string aiAbilityCode;
        List<int> aiAbilityIds = AbilityUtilities.RandomSelect().Select(a => AbilityDatabase.I.GetIdFromAbility(a)).ToList();
        LobbyParticipantData.AbilityCodeEncoder(aiAbilityIds, out aiAbilityCode);

        // Generate lobby data.
        LobbyParticipantData lobby_data =
            new LobbyParticipantData(aiNo, aiMemberNo, aiName, aiClientId, true, aiTeam, true, false, aiSkillCode, aiAbilityCode);
        return lobby_data;
    }

    // Used in solo player. (Generated in SortieLobbyManager when participant determines)
    public static BattleInfo.ParticipantBattleData GenerateAIBattleData(int aiNo, int aiMemberNo, string aiName, Team aiTeam)
    {
        // Set random skills.
        int?[] aiSkillIds, aiSkillLevels;
        SkillUtilities.GenerateSkills(out aiSkillIds, out aiSkillLevels);

        // Set random abilities.
        List<int> aiAbilityIds = AbilityUtilities.RandomSelect().Select(a => AbilityDatabase.I.GetIdFromAbility(a)).ToList();

        // Generate battle data.
        BattleInfo.ParticipantBattleData battle_data
            = new BattleInfo.ParticipantBattleData(aiNo, aiMemberNo, false, aiClientId, aiName, aiTeam, aiSkillIds, aiSkillLevels, aiAbilityIds);
        return battle_data;
    }
}
