using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Skill/Create Skill Level Data", fileName = "SkillLevelData")]
public class SkillLevelData : ScriptableObject
{
    [SerializeField, OnValueChanged("OnSkillDataSet"), Required]
    SkillData skillData;
    void OnSkillDataSet()
    {
        if (skillData != null)
        {
            skillName = skillData.GetName();
            skillId = skillData.GetId();
            skillType = skillData.GetSkillType();
        }
        else
        {
            skillName = null;
            skillId = -1;
            skillType = SkillType.attack;
        }
    }
    bool HasSkillData() { return skillData != null; }

    public LevelData GetLevelData(int level)
    {
        if (level < 1 || 5 < level)
        {
            Debug.LogError("レベルが範囲を超えています");
            return null;
        }
        switch (skillType)
        {
            case SkillType.attack:
                return attackLevelDatas[level - 1];

            case SkillType.heal:
                return healLevelDatas[level - 1];

            case SkillType.assist:
                return assistLevelDatas[level - 1];

            case SkillType.disturb:
                return disturbLevelDatas[level - 1];

            default:
                Debug.LogError("SkillTypeがNullです");
                return null;
        }
    }

    [SerializeField, ReadOnly, ShowIf("HasSkillData")] string skillName;
    public string GetName() { return skillName; }
    [SerializeField, ReadOnly, ShowIf("HasSkillData")] int skillId;
    public int GetId() { return skillId; }
    [SerializeField, ReadOnly, ShowIf("HasSkillData")] SkillType skillType;
    public SkillType GetSkillType() { return skillType; }
    bool IsAttack() { return skillType == SkillType.attack; }
    bool IsHeal() { return skillType == SkillType.heal; }
    bool IsAssist() { return skillType == SkillType.assist; }
    bool IsDisturb() { return skillType == SkillType.disturb; }


    [ShowIf("IsAttack"), SerializeField]
    AttackLevelData[] attackLevelDatas = new AttackLevelData[5];

    [ShowIf("IsHeal"), SerializeField]
    HealLevelData[] healLevelDatas = new HealLevelData[5];

    [ShowIf("IsAssist"), SerializeField]
    AssistLevelData[] assistLevelDatas = new AssistLevelData[5];

    [ShowIf("IsDisturb"), SerializeField]
    DisturbLevelData[] disturbLevelDatas = new DisturbLevelData[5];
}


public interface LevelData
{
    public float ChargeTime { get; set; }
    public float FreeFloat1 { get; set; }
    public float FreeFloat2 { get; set; }
    public float FreeFloat3 { get; set; }
    public string[] EnhanceDetails { get; set; }
}


[System.Serializable]
public struct AttackLevelData : LevelData
{
    [field: SerializeField] public float ChargeTime { get; set; }
    [field: SerializeField] public float FreeFloat1 { get; set; }
    [field: SerializeField] public float FreeFloat2 { get; set; }
    [field: SerializeField] public float FreeFloat3 { get; set; }
    [field: SerializeField] public string[] EnhanceDetails { get; set; }

    [Header("Basics")]
    public float Power;
    public float Speed;
    public float Lifespan;
    public int WeaponCount;

    [Header("Homing")]
    public HomingType HomingType;
    [Range(0, 1f)] public float HomingAccuracy;
    [Range(0, 180)] public float HomingAngle;

    [Header("Additional Effects")]
    public bool SpeedDown;
    public bool PowerDown;
    public bool DefenceDown;
    [Range(-3, 0)] public int SpeedGrade, PowerGrade, DefenceGrade;
    [Range(0, 1)] public float SpeedDownProbability, PowerDownProbability, DefenceDownProbability;
    public float SpeedDuration, PowerDuration, DefenceDuration;
}


[System.Serializable]
public struct HealLevelData : LevelData
{
    [field: SerializeField] public float ChargeTime { get; set; }
    [field: SerializeField] public float FreeFloat1 { get; set; }
    [field: SerializeField] public float FreeFloat2 { get; set; }
    [field: SerializeField] public float FreeFloat3 { get; set; }
    [field: SerializeField] public string[] EnhanceDetails { get; set; }

    public float RepairAmount;
}


[System.Serializable]
public struct AssistLevelData : LevelData
{
    [field: SerializeField] public float ChargeTime { get; set; }
    [field: SerializeField] public float FreeFloat1 { get; set; }
    [field: SerializeField] public float FreeFloat2 { get; set; }
    [field: SerializeField] public float FreeFloat3 { get; set; }
    [field: SerializeField] public string[] EnhanceDetails { get; set; }

    [Range(0, 3)] public int SpeedGrade, PowerGrade, DefenceGrade;
    public float SpeedDuration, PowerDuration, DefenceDuration;
}


[System.Serializable]
public struct DisturbLevelData : LevelData
{
    [field: SerializeField] public float ChargeTime { get; set; }
    [field: SerializeField] public float FreeFloat1 { get; set; }
    [field: SerializeField] public float FreeFloat2 { get; set; }
    [field: SerializeField] public float FreeFloat3 { get; set; }
    [field: SerializeField] public string[] EnhanceDetails { get; set; }

    [Header("Basics")]
    public float Speed;
    public float Lifespan;
    public int WeaponCount;

    [Header("Homing")]
    public HomingType HomingType;
    [Range(0, 1f)] public float HomingAccuracy;
    [Range(0, 180)] public float HomingAngle;

    [Header("Additional Effects")]
    public bool SpeedDown;
    public bool PowerDown;
    public bool DefenceDown;
    [Range(-3, 0)] public int SpeedGrade, PowerGrade, DefenceGrade;
    [Range(0, 1)] public float SpeedDownProbability, PowerDownProbability, DefenceDownProbability;
    public float SpeedDuration, PowerDuration, DefenceDuration;
}

public enum HomingType { Normal, PreHoming, Homing }