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

    System.Guid guid;
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
        // Play effects.
        burner.PlaySpark();
        wind.WindGenerator(2, 15);

        if (!attack.IsOwner) return;

        base.Activator();
        MeterDecreaser(boost_duration, EndProccess);
        float tmp_speed = attack.fighterCondition.defaultSpeed * speed_magnif;
        guid = attack.fighterCondition.speed.ApplyTempStatus(tmp_speed);

        if (attack.IsLocalPlayer)
        {
            const float ACCEL_DURATION = 0.2f;
            const float FIELD_OF_VIEW = 100;
            CameraController.I.ChangeView(FIELD_OF_VIEW, ACCEL_DURATION);
        }

        if (IsHost)
        {
            attack.SkillActivatorClientRpc(NetworkManager.Singleton.LocalClientId, skillNo);
        }
        else
        {
            attack.SkillActivatorServerRpc(NetworkManager.Singleton.LocalClientId, skillNo);
        }
    }

    public override void EndProccess()
    {
        // Stop effects.
        const float decelerate_duration = 1.5f;
        burner.StopSpark();
        wind.ResetWind();

        if (!attack.IsOwner) return;

        // Reset fighter condition.
        base.EndProccess();
        attack.fighterCondition.speed.RemoveTempStatus(guid);

        if (attack.IsLocalPlayer)
        {
            CameraController.I.ResetView(decelerate_duration);
        }

        // Send RPC to all clones to end this skill.
        attack.SkillEndProccessServerRpc(NetworkManager.Singleton.LocalClientId, skillNo);
    }

    public override void ForceTermination(bool maintain_charge)
    {
        burner.StopSpark();
        wind.ResetWind();

        if (BattleInfo.isMulti && !attack.IsOwner) return;

        base.ForceTermination(maintain_charge);
        attack.fighterCondition.speed.RemoveTempStatus(guid);

        if (attack.IsLocalPlayer)
        {
            CameraController.I.ResetView();
        }
    }
}