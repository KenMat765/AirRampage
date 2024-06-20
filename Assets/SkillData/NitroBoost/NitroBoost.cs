using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

class NitroBoost : SkillAssist
{
    // スキルレベルによって変更の可能性があるパラメータ
    float speed_magnif;
    float boost_duration;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        speed_magnif = levelData.FreeFloat1;
        boost_duration = levelData.FreeFloat2;
    }

    BurnerController burner;
    WindController wind;

    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
        SetPrefabLocalTransform(Vector3.zero, Vector3.zero, new Vector3(1, 1, 1));
        GeneratePrefab();

        burner = attack.fighterCondition.body.GetComponentInChildren<BurnerController>();
        wind = prefabs[0].GetComponent<WindController>();
    }

    public override void Activator(int[] transfer = null)
    {
        // Play effect.
        const float accel_duration = 0.2f;
        burner.PlaySpark();
        wind.WindGenerator(2, 15);

        if (BattleInfo.isMulti && !attack.IsOwner) return;

        base.Activator();
        MeterDecreaser(boost_duration, EndProccess);
        attack.fighterCondition.PauseGradingSpeed(attack.fighterCondition.defaultSpeed * speed_magnif, accel_duration);

        if (attack.IsLocalPlayer)
        {
            CameraController.I.ChangeView(100, accel_duration);
        }

        if (BattleInfo.isMulti)
        {
            if (IsHost) attack.SkillActivatorClientRpc(NetworkManager.Singleton.LocalClientId, skillNo);
            else attack.SkillActivatorServerRpc(NetworkManager.Singleton.LocalClientId, skillNo);
        }
    }

    public override void EndProccess()
    {
        // Stop effect.
        const float decelerate_duration = 1.5f;
        burner.StopSpark();
        wind.ResetWind();

        if (BattleInfo.isMulti && !attack.IsOwner) return;

        // Reset fighter condition.
        base.EndProccess();
        attack.fighterCondition.ResumeGradingSpeed();

        if (attack.IsLocalPlayer)
        {
            CameraController.I.ResetView(decelerate_duration);
        }

        // Send RPC to all clones to end this skill.
        if (BattleInfo.isMulti) attack.SkillEndProccessServerRpc(NetworkManager.Singleton.LocalClientId, skillNo);
    }

    public override void ForceTermination()
    {
        burner.StopSpark();
        wind.ResetWind();

        if (BattleInfo.isMulti && !attack.IsOwner) return;

        base.ForceTermination();
        attack.fighterCondition.ResumeGradingSpeed();

        if (attack.IsLocalPlayer)
        {
            CameraController.I.ResetView();
        }
    }
}