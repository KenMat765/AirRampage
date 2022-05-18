using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : SkillAssist
{
    // スキルレベルによって変更の可能性があるパラメータ
    public float shield_durability {get; private set;}
    float exhaust_speed;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        shield_durability = levelData.FreeFloat1;
        exhaust_speed = levelData.FreeFloat2;
    }

    Collider col;
    ShieldHitDetector hit_detector;

    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
        SetPrefabLocalTransform(new Vector3(0, 0, 0.025f), Vector3.zero, new Vector3(10, 10, 10));
        GeneratePrefab();
        col = GetComponent<Collider>();
        hit_detector = prefabs[0].GetComponent<ShieldHitDetector>();
    }

    public override void Activator(string infoCode = null)
    {
        base.Activator();
        col.enabled = false;
        hit_detector.ShieldActivator(shield_durability, exhaust_speed);
    }

    public override void EndProccess() { col.enabled = true; }

    public override void ForceTermination()
    {
        EndProccess();
        hit_detector.TerminateShield();
    }
}
