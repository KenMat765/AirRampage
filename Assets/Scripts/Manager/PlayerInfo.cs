using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PlayerInfo
{
    // The only instance of PlayerInfo.
    public static PlayerInfo I { get; set; }


    // === Skill === //

    /// <Summary> returns -1 if null. </Summary>
    public string[] deck_skill_ids = new string[GameInfo.deck_count];

    public void SkillIdSetter(int deckNo, int skillNo, int? skillId)
    {
        int[] skillIds = StringToSkillIds(deck_skill_ids[deckNo]);
        if (skillId.HasValue)
        {
            skillIds[skillNo] = (int)skillId;
        }
        else
        {
            skillIds[skillNo] = -1;
        }
        string skillIds_str = SkillIdsToString(skillIds);
        deck_skill_ids[deckNo] = skillIds_str;
    }

    public void SkillIdsGetter(int deckNumber, out int?[] skillIds)
    {
        skillIds = new int?[GameInfo.max_skill_count];
        int[] skillIds_temp = StringToSkillIds(deck_skill_ids[deckNumber]);
        for (int k = 0; k < GameInfo.max_skill_count; k++)
        {
            if (skillIds_temp[k] == -1)
            {
                skillIds[k] = null;
            }
            else
            {
                skillIds[k] = skillIds_temp[k];
            }
        }
    }

    public void SkillLevelsGetter(int deckNumber, out int?[] skillLevels)
    {
        skillLevels = new int?[GameInfo.max_skill_count];
        int[] skillIds_temp = StringToSkillIds(deck_skill_ids[deckNumber]);
        for (int k = 0; k < GameInfo.max_skill_count; k++)
        {
            int skillId_temp = skillIds_temp[k];
            if (skillId_temp == -1)
            {
                skillLevels[k] = null;
            }
            else
            {
                skillLevels[k] = skl_level[skillId_temp];
            }
        }
    }

    // Convert Skill Ids to string, in order to convert to JSON.
    // [1,4,-1,3,2] -> "1/4/-1/3/2/"
    string SkillIdsToString(int[] skillIds)
    {
        string result = "";
        for (int k = 0; k < skillIds.Length; k++)
        {
            result += skillIds[k] + "/";
        }
        return result;
    }

    // "1/4/-1/3/2/" -> [1,4,-1,3,2]
    int[] StringToSkillIds(string skillIds_str)
    {
        int[] result = new int[GameInfo.max_skill_count];

        // Input will be null when there are no save data.
        if (skillIds_str == null || skillIds_str == "")
        {
            for (int m = 0; m < result.Length; m++)
            {
                result[m] = -1;
            }
            return result;
        }

        string id_str = "";
        int no = 0;
        for (int k = 0; k < skillIds_str.Length; k++)
        {
            char cur_char = skillIds_str[k];
            if (cur_char == '/')
            {
                result[no] = int.Parse(id_str);
                id_str = "";
                no++;
            }
            else
            {
                id_str += cur_char;
            }
        }
        return result;
    }

    // skill_id と unlock したかのセット
    // スキルの数だけ手動で追加する必要がある
    public bool[] skl_unlock =
    {
        false, false, false, false, false, false,
        true, true, true, true, true, true,
        true
    };

    // skill_id と 各スキルのレベル のセット
    // スキルの数だけ手動で追加する必要がある
    public int[] skl_level =
    {
        1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1,
        1
    };


    // === Ability === //
    public List<int> AbilityIdsGetter()
    {
        List<int> abilityIds = new List<int>();
        for (int id = 0; id < AbilityDatabase.I.ability_count; id++)
        {
            bool is_equiped = abi_equip[id];
            if (is_equiped)
            {
                abilityIds.Add(id);
            }
        }
        return abilityIds;
    }

    public bool[] abi_unlock =
    {
        true, true, true, true, true, true,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
    };

    public bool[] abi_equip =
    {
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
        false, false, false, false, false, false,
    };


    // === Settings === //
    public string myName = "Kari name";
    public float volume = 1.0f;
    public int fps = 60;
    public float bgm = 1.0f;
    public bool postprocess = true;
    public int coins
    {
        get
        {
            return Coins;
        }
        set
        {
            Coins = Mathf.Clamp(value, 0, 9999);
        }
    }
    [SerializeField] int Coins = 9999;
}
