using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;



[CreateAssetMenu(menuName = "Skill/Create SkillDatabase", fileName = "SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    static SkillDatabase instance;
    public static SkillDatabase I
    {
        get
        {
            if(instance == null)
            {
                instance = Resources.Load<SkillDatabase>("SkillDatabase");
                if(instance == null)
                {
                    Debug.LogError("SkillDatabaseが見つかりませんでした");
                }
            }
            return instance;
        }
    }

    [SerializeField, ReorderableList] List<SkillData> skillData;

    public int skill_type_count { get{return skillData.Count;} }

    public SkillData SearchSkillByName(string name)
    {
        SkillData[] skill_data;
        if(skillData.FindElement(data => data.GetName() == name, out skill_data))
        {
            return skill_data[0];
        }
        else
        {
            return null;
        }
    }

    public SkillData SearchSkillById(int id)
    {
        SkillData[] skill_data;
        if(skillData.FindElement(data => data.GetId() == id, out skill_data))
        {
            return skill_data[0];
        }
        else
        {
            return null;
        }
    }

    public SkillData SearchSkillByPageOrder(int page, int order)
    {
        SkillData[] skill_data;
        if(skillData.FindElement(data => data.GetPageOrder() == (page, order), out skill_data))
        {
            return skill_data[0];
        }
        else
        {
            return null;
        }
    }
}