using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillHeal : Skill
{
    protected HealLevelData levelData {get; private set;}
    public override void LevelDataSetter(LevelData levelData) { this.levelData = (HealLevelData)levelData; }
    protected override void ParameterUpdater() { charge_time = levelData.ChargeTime; }
}
