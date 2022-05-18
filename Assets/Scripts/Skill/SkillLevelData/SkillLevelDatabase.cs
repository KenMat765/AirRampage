using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Skill/Create Skill Level Database", fileName = "SkillLevelDatabase")]
public class SkillLevelDatabase : ScriptableObject
{
    static SkillLevelDatabase instance;
    public static SkillLevelDatabase I
    {
        get
        {
            if(instance == null)
            {
                instance = Resources.Load<SkillLevelDatabase>("SkillLevelDatabase");
                if(instance == null)
                {
                    Debug.LogError("SkillLevelDatabaseが見つかりませんでした");
                }
            }
            return instance;
        }
    }

    [SerializeField, ReorderableList] List<SkillLevelData> skillLevelData;

    public SkillLevelData SearchSkillByName(string name)
    {
        SkillLevelData[] skill_level_data;
        if(skillLevelData.FindElement(data => data.GetName() == name, out skill_level_data))
        {
            return skill_level_data[0];
        }
        else
        {
            return null;
        }
    }

    public SkillLevelData SearchSkillById(int id)
    {
        SkillLevelData[] skill_level_data;
        if(skillLevelData.FindElement(data => data.GetId() == id, out skill_level_data))
        {
            return skill_level_data[0];
        }
        else
        {
            return null;
        }
    }
}
