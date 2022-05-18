using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LobbyAiSkillGenerator : Singleton<LobbyAiSkillGenerator>
{
    protected override bool dont_destroy_on_load {get; set;} = false;

    public void GenerateSkills(out int?[] skillIds, out int?[] skillLevels, int null_count = 0)
    {
        skillIds = new int?[GameInfo.max_skill_count];
        skillLevels = new int?[GameInfo.max_skill_count];
        int nonNull_count = GameInfo.max_skill_count - null_count;
        int[] skillIds_nonNull = Utilities.RandomMultiSelect(0, SkillDatabase.I.skill_type_count, nonNull_count);
        int[] skillLevels_nonNull = Utilities.RandomMultiSelect(1, 6, nonNull_count);
        for(int k = 0; k < GameInfo.max_skill_count; k ++)
        {
            if(k < nonNull_count)
            {
                skillIds[k] = skillIds_nonNull[k];
                skillLevels[k] = skillLevels_nonNull[k];
            }
            else
            {
                skillIds[k] = null;
                skillLevels[k] = null;
            }
        }
    }
}
