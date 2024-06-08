using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Skill/Create SkillData", fileName = "SkillData")]
public class SkillData : ScriptableObject
{
    [SerializeField] string skillName;
    [SerializeField] string skillNameJp;
    [SerializeField] int skillId;

    [SerializeField, OnValueChanged("SetColorBySkillType")]
    SkillType skillType;
    void SetColorBySkillType()
    {
        switch (skillType)
        {
            case SkillType.attack: skillColor = Color.red; break;
            case SkillType.heal: skillColor = Color.green; break;
            case SkillType.assist: skillColor = new Color(0, 0.15f, 1, 1); break;
            case SkillType.disturb: skillColor = new Color(0.55f, 0, 1, 1); break;
            default: skillColor = Color.gray; break;
        }
    }

    [SerializeField, ReadOnly] Color skillColor;
    [SerializeField, ShowAssetPreview] Sprite skillSprite;
    [SerializeField] GameObject skillPrefabRed;
    [SerializeField] GameObject skillPrefabBlue;
    [SerializeField] Skill skillScript;
    [SerializeField, TextArea(3, 5)] string skillInformation;
    [SerializeField] string[] skillFeatures;

    public string GetName() { return skillName; }
    public string GetNameJp() { return skillNameJp; }
    public int GetId() { return skillId; }
    public (int, int) GetPageOrder()
    {
        int page, order;
        page = skillId / SkillDeckList.num_in_page;
        order = skillId % SkillDeckList.num_in_page;
        return (page, order);
    }
    public SkillType GetSkillType() { return skillType; }
    public Color GetColor() { return skillColor; }
    public Sprite GetSprite() { return skillSprite; }
    public GameObject GetPrefabRed() { return skillPrefabRed; }
    public GameObject GetPrefabBlue() { return skillPrefabBlue; }
    public Skill GetScript() { return skillScript; }
    public string GetInfomation() { return skillInformation; }
    public string[] GetFeatures() { return skillFeatures; }
}

public enum SkillType { attack, heal, assist, disturb }