using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenceTuner : SkillAssist
{
    // スキルレベルによって変更の可能性があるパラメータ
    int defence_grade = 2;
    float duration = 20;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        defence_grade = levelData.DefenceGrade;
        duration = levelData.DefenceDuration;
    }

    ParticleSystem effect;

    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
        SetPrefabLocalTransform(Vector3.zero, new Vector3(-90, 0, 0), new Vector3(0.035f, 0.035f, 0.035f));
        GeneratePrefab();
        effect = prefabs[0].GetComponent<ParticleSystem>();
    }

    public override void Activator(string infoCode = null)
    {
        base.Activator();
        MeterDecreaser(duration);
        effect.Play();

        // Grader must be called from the owner of this fighter only.
        if(BattleInfo.isMulti && !attack.IsOwner) return;
        attack.fighterCondition.DefenceGrader(defence_grade, duration);
    }

    public override void ForceTermination()
    {
        base.ForceTermination();
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
