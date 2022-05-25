using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    JetAudioController jetAudioController;
    BurnerController burnerController;
    WindController wind;
    CameraController cameraController;

    public override void Generator()
    {
        base.Generator();
        original_prefab = TeamPrefabGetter();
        SetPrefabLocalTransform(Vector3.zero, Vector3.zero, new Vector3(1, 1, 1));
        GeneratePrefab();

        Transform afterBurners = attack.fighterCondition.body.transform.Find("AfterBurners");
        jetAudioController = afterBurners.GetComponent<JetAudioController>();
        burnerController = afterBurners.GetComponent<BurnerController>();
        wind = prefabs[0].GetComponent<WindController>();
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if(playerMovement != null) cameraController = playerMovement.cameraController;
    }

    public override void Activator(int[] transfer = null)
    {
        // Play effect.
        const float accel_duration = 0.2f;
        jetAudioController.ChangeJetPitch(1, accel_duration);
        burnerController.Boost(0.04f, accel_duration, emit_trail : true);
        wind.WindGenerator(2, 15);

        if(BattleInfo.isMulti && !attack.IsOwner) return;

        base.Activator();
        MeterDecreaser(boost_duration, EndProccess);
        attack.fighterCondition.PauseGradingSpeed(attack.fighterCondition.defaultSpeed * speed_magnif, accel_duration);
        if(cameraController != null) cameraController.ViewChanger(90, accel_duration);
        if(BattleInfo.isMulti)
        {
            if (IsHost) attack.SkillActivatorClientRpc(OwnerClientId, skillNo);
            else attack.SkillActivatorServerRpc(OwnerClientId, skillNo);
        }
    }

    public override void EndProccess()
    {
        // Stop effect.
        const float decelerate_duration = 1.5f;
        jetAudioController.ResetJetPitch(decelerate_duration);
        burnerController.ResetBoost(decelerate_duration);
        wind.ResetWind();

        if(BattleInfo.isMulti && !attack.IsOwner) return;

        // Reset fighter condition.
        base.EndProccess();
        attack.fighterCondition.ResumeGradingSpeed();
        if(cameraController != null) cameraController.ResetView(decelerate_duration);

        // Send RPC to all clones to end this skill.
        if(BattleInfo.isMulti) attack.SkillEndProccessServerRpc(OwnerClientId, skillNo);
    }

    public override void ForceTermination()
    {
        jetAudioController.ResetJetPitch();
        burnerController.ResetBoost();
        wind.ResetWind();

        if(BattleInfo.isMulti && !attack.IsOwner) return;

        base.ForceTermination();
        attack.fighterCondition.ResumeGradingSpeed();
        if(cameraController != null) cameraController.ResetView();
    }
}