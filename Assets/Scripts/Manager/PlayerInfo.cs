using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerInfo
{
    // The only instance of PlayerInfo.
    public static PlayerInfo I { get; set; }

    // 
    // 
    // 
    public int[,] ddd = new int[GameInfo.deck_count, GameInfo.max_skill_count];

    public int?[,] deck_skill_ids { get; set; } = new int?[GameInfo.deck_count, GameInfo.max_skill_count];
    public void SkillIdGetter(int deckNumber, out int?[] skillIds)
    {
        skillIds = new int?[GameInfo.max_skill_count];
        for (int k = 0; k < GameInfo.max_skill_count; k++)
        {
            skillIds[k] = deck_skill_ids[deckNumber, k];
        }
    }
    public void SkillLevelGetter(int deckNumber, out int?[] skillLevels)
    {
        skillLevels = new int?[GameInfo.max_skill_count];
        for (int k = 0; k < GameInfo.max_skill_count; k++)
        {
            int? skillId_nullable = deck_skill_ids[deckNumber, k];
            if (skillId_nullable.HasValue)
            {
                int skillId = (int)skillId_nullable;
                skillLevels[k] = level[skillId];
            }
            else
            {
                skillLevels[k] = null;
            }
        }
    }

    // skill_id と unlock したかのセット
    // スキルの数だけ手動で追加する必要がある
    public Dictionary<int, bool> unlock { get; set; } = new Dictionary<int, bool>()
    {
        {0, true}, {1, true}, {2, true}, {3, true}, {4, true}, {5, true},
        {6, true}, {7, true}, {8, true}, {9, true}, {10, true}, {11, true},
    };

    // skill_id と 各スキルのレベル のセット
    // スキルの数だけ手動で追加する必要がある
    public Dictionary<int, int> level { get; set; } = new Dictionary<int, int>()
    {
        {0, 3}, {1, 3}, {2, 3}, {3, 3}, {4, 3}, {5, 3},
        {6, 3}, {7, 3}, {8, 3}, {9, 3}, {10, 3}, {11, 3},
    };
}
