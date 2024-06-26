using System.Collections;
using System.Collections.Generic;

public static class SkillUtilities
{
    public static void GenerateSkills(out int?[] skillIds, out int?[] skillLevels, int null_count = 0)
    {
        skillIds = new int?[GameInfo.MAX_SKILL_COUNT];
        skillLevels = new int?[GameInfo.MAX_SKILL_COUNT];
        int nonNull_count = GameInfo.MAX_SKILL_COUNT - null_count;
        int[] skillIds_nonNull = Utilities.RandomMultiSelect(0, SkillDatabase.I.skill_type_count, nonNull_count);
        int[] skillLevels_nonNull = Utilities.RandomMultiSelect(1, 6, nonNull_count);
        for (int k = 0; k < GameInfo.MAX_SKILL_COUNT; k++)
        {
            if (k < nonNull_count)
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
