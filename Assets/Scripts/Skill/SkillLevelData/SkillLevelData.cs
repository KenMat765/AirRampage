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
        switch (skillType)
        {
            case SkillType.attack:
                switch (level)
                {
                    case 1: return attackLevelData1;
                    case 2: return attackLevelData2;
                    case 3: return attackLevelData3;
                    case 4: return attackLevelData4;
                    case 5: return attackLevelData5;
                    default: Debug.LogError("レベルが範囲を超えています"); return null;
                }

            case SkillType.heal:
                switch (level)
                {
                    case 1: return healLevelData1;
                    case 2: return healLevelData2;
                    case 3: return healLevelData3;
                    case 4: return healLevelData4;
                    case 5: return healLevelData5;
                    default: Debug.LogError("レベルが範囲を超えています"); return null;
                }

            case SkillType.assist:
                switch (level)
                {
                    case 1: return assistLevelData1;
                    case 2: return assistLevelData2;
                    case 3: return assistLevelData3;
                    case 4: return assistLevelData4;
                    case 5: return assistLevelData5;
                    default: Debug.LogError("レベルが範囲を超えています"); return null;
                }

            case SkillType.disturb:
                switch (level)
                {
                    case 1: return disturbLevelData1;
                    case 2: return disturbLevelData2;
                    case 3: return disturbLevelData3;
                    case 4: return disturbLevelData4;
                    case 5: return disturbLevelData5;
                    default: Debug.LogError("レベルが範囲を超えています"); return null;
                }

            default: Debug.LogError("SkillTypeがNullです"); return null;
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


    [ShowIf("IsAttack"), BoxGroup("Level 1"), SerializeField]
    AttackLevelData attackLevelData1;

    [ShowIf("IsAttack"), BoxGroup("Level 2"), SerializeField]
    AttackLevelData attackLevelData2;

    [ShowIf("IsAttack"), BoxGroup("Level 3"), SerializeField]
    AttackLevelData attackLevelData3;

    [ShowIf("IsAttack"), BoxGroup("Level 4"), SerializeField]
    AttackLevelData attackLevelData4;

    [ShowIf("IsAttack"), BoxGroup("Level 5"), SerializeField]
    AttackLevelData attackLevelData5;


    [ShowIf("IsHeal"), BoxGroup("Level 1"), SerializeField]
    HealLevelData healLevelData1;

    [ShowIf("IsHeal"), BoxGroup("Level 2"), SerializeField]
    HealLevelData healLevelData2;

    [ShowIf("IsHeal"), BoxGroup("Level 3"), SerializeField]
    HealLevelData healLevelData3;

    [ShowIf("IsHeal"), BoxGroup("Level 4"), SerializeField]
    HealLevelData healLevelData4;

    [ShowIf("IsHeal"), BoxGroup("Level 5"), SerializeField]
    HealLevelData healLevelData5;


    [ShowIf("IsAssist"), BoxGroup("Level 1"), SerializeField]
    AssistLevelData assistLevelData1;

    [ShowIf("IsAssist"), BoxGroup("Level 2"), SerializeField]
    AssistLevelData assistLevelData2;

    [ShowIf("IsAssist"), BoxGroup("Level 3"), SerializeField]
    AssistLevelData assistLevelData3;

    [ShowIf("IsAssist"), BoxGroup("Level 4"), SerializeField]
    AssistLevelData assistLevelData4;

    [ShowIf("IsAssist"), BoxGroup("Level 5"), SerializeField]
    AssistLevelData assistLevelData5;


    [ShowIf("IsDisturb"), BoxGroup("Level 1"), SerializeField]
    DisturbLevelData disturbLevelData1;

    [ShowIf("IsDisturb"), BoxGroup("Level 2"), SerializeField]
    DisturbLevelData disturbLevelData2;

    [ShowIf("IsDisturb"), BoxGroup("Level 3"), SerializeField]
    DisturbLevelData disturbLevelData3;

    [ShowIf("IsDisturb"), BoxGroup("Level 4"), SerializeField]
    DisturbLevelData disturbLevelData4;

    [ShowIf("IsDisturb"), BoxGroup("Level 5"), SerializeField]
    DisturbLevelData disturbLevelData5;
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