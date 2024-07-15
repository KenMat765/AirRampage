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

    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        original_prefab = TeamPrefabGetter(fighterTeam);
        SetPrefabLocalTransform(Vector3.zero, Vector3.zero, new Vector3(1, 1, 1));
        GeneratePrefab();

        burner = skillController.fighterCondition.GetComponentInChildren<BurnerController>();
        wind = prefabs[0].GetComponent<WindController>();
    }

    public override int[] Activator(int[] received_data = null)
    {
        // Play effects.
        burner.PlaySpark();
        wind.WindGenerator(2, 15);

        if (skillController.IsOwner)
        {
            base.Activator();
            MeterDecreaser(boost_duration, EndProcess);
            float tmp_speed = skillController.fighterCondition.defaultSpeed * speed_magnif;
            guid = skillController.fighterCondition.speed.ApplyTempStatus(tmp_speed);

            if (skillController.IsLocalPlayer)
            {
                const float ACCEL_DURATION = 0.2f;
                const float FIELD_OF_VIEW = 100;
                CameraController.I.ChangeView(FIELD_OF_VIEW, ACCEL_DURATION);
            }
        }

        return null;
    }

    public override void EndProcess()
    {
        // Stop effects.
        const float decelerate_duration = 1.5f;
        burner.StopSpark();
        wind.ResetWind();

        if (skillController.IsOwner)
        {
            // Reset fighter condition.
            base.EndProcess();
            skillController.fighterCondition.speed.RemoveTempStatus(guid);

            if (skillController.IsLocalPlayer)
            {
                CameraController.I.ResetView(decelerate_duration);
            }

            // Send RPC to all clones to end this skill.
            ulong owner_id = NetworkManager.Singleton.LocalClientId;
            if (skillController.IsHost)
                skillController.SkillEndProcessClientRpc(owner_id, skillNo);
            else
                skillController.SkillEndProcessServerRpc(owner_id, skillNo);
        }
    }

    public override void ForceTermination(bool maintain_charge)
    {
        burner.StopSpark();
        wind.ResetWind();

        if (skillController.IsOwner)
        {
            base.ForceTermination(maintain_charge);
            skillController.fighterCondition.speed.RemoveTempStatus(guid);
            if (skillController.IsLocalPlayer)
            {
                CameraController.I.ResetView();
            }
        }
    }
}