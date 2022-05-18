using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillAssist : Skill
{
    protected AssistLevelData levelData {get; private set;}
    public override void LevelDataSetter(LevelData levelData) { this.levelData = (AssistLevelData)levelData; }
    protected override void ParameterUpdater() { charge_time = levelData.ChargeTime; }
}
