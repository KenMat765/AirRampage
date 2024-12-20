using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillDisturb : Skill
{
    protected List<Weapon> weapons { get; private set; }
    protected DisturbLevelData levelData { get; private set; }
    public override void LevelDataSetter(LevelData levelData) { this.levelData = (DisturbLevelData)levelData; }
    protected override void ParameterUpdater() { charge_time = levelData.ChargeTime; }
    protected virtual System.Func<float> StayMotionGenerator(GameObject prefab) { return null; }
    protected Attack attack;

    public override void Generator(int skill_no, SkillData skill_data)
    {
        base.Generator(skill_no, skill_data);
        weapons = new List<Weapon>();
        attack = GetComponent<Attack>();
    }

    public override void ForceTermination(bool maintain_charge)
    {
        base.ForceTermination(maintain_charge);
        foreach (Weapon weapon in weapons) weapon.TerminateWeapon();
    }


    /// <Summary>
    /// prefab、weaponを新たに１つだけ生成し、Listに追加する。
    /// 生成したprefabを返す。
    /// </Summary>
    protected override GameObject GeneratePrefab(Transform parent = null)
    {
        if (parent == null) parent = transform;

        GameObject prefab = Instantiate(original_prefab, parent);
        prefab.transform.localPosition = local_position;
        prefab.transform.localRotation = Quaternion.Euler(local_eulerAngle);
        prefab.transform.localScale = local_scale;
        prefabs.Add(prefab);

        Weapon weapon = prefab.GetComponent<Weapon>();
        FighterCondition fighter_condition = skillController.fighterCondition;
        int fighter_no = fighter_condition.fighterNo.Value;
        Team fighter_team = fighter_condition.fighterTeam.Value;
        bool is_owner = fighter_condition.IsOwner;
        weapon.WeaponSetter(gameObject, fighter_no, fighter_team, is_owner, true, GetType().Name, StayMotionGenerator(prefab));
        weapon.WeaponParameterSetter(levelData);
        weapons.Add(weapon);

        return prefab;
    }

    /// <Summary>
    /// 現在未使用のPrefabのIndexを１つだけ返す。
    /// </Summary>
    protected int GetPrefabIndex()
    {
        // 未使用のものがあればそのIndexを返す
        foreach (GameObject prefab in prefabs) if (!prefab.activeSelf) return prefabs.IndexOf(prefab);

        // なかった場合は新たにprefabを生成
        GameObject new_prefab = GeneratePrefab();
        return prefabs.IndexOf(new_prefab);
    }

    /// <Summary>
    /// prefab、weaponを新たに引数個だけ生成し、Listに追加する。
    /// 生成したprefab(配列)を返す。
    /// </Summary>
    protected override GameObject[] GeneratePrefabs(int count, Transform parent = null)
    {
        if (parent == null) parent = transform;

        GameObject[] generated_prefabs = new GameObject[count];

        for (int k = 0; k < count; k++)
        {
            GameObject prefab = Instantiate(original_prefab, parent);
            prefab.transform.localPosition = local_positions[k];
            prefab.transform.localRotation = Quaternion.Euler(local_eulerAngles[k]);
            prefab.transform.localScale = local_scales[k];
            prefabs.Add(prefab);

            Weapon weapon = prefab.GetComponent<Weapon>();
            FighterCondition fighter_condition = skillController.fighterCondition;
            int fighter_no = fighter_condition.fighterNo.Value;
            Team fighter_team = fighter_condition.fighterTeam.Value;
            bool is_owner = fighter_condition.IsOwner;
            weapon.WeaponSetter(gameObject, fighter_no, fighter_team, is_owner, true, GetType().Name, StayMotionGenerator(prefab));
            weapon.WeaponParameterSetter(levelData);
            weapons.Add(weapon);

            generated_prefabs[k] = prefab;
        }

        return generated_prefabs;
    }

    /// <Summary>
    /// 現在未使用のPrefabのIndexを引数個だけ返す。
    /// </Summary>
    protected int[] GetPrefabIndexes(int count)
    {
        int[] indexes = new int[count];

        // 発射準備が完了しているセットがあるか検索
        int? final_index_in_ready_set = null;
        int total_set_count = prefabs.Count / count;
        for (int set_count = 0; set_count < total_set_count; set_count++)
        {
            int final_index_in_set = count * set_count + (count - 1);
            if (!prefabs[final_index_in_set].activeSelf)
            {
                final_index_in_ready_set = final_index_in_set;
                break;
            }
        }

        // 発射準備が完了しているセットがなかった場合は、新たにprefabを生成
        if (final_index_in_ready_set == null)
        {
            GameObject[] new_prefabs = GeneratePrefabs(count);
            for (int k = 0; k < count; k++)
            {
                int prefab_index = prefabs.IndexOf(new_prefabs[k]);
                indexes[k] = prefab_index;
            }
        }
        // 発射準備が完了しているセットがあった場合は、それを使う
        else
        {
            for (int k = 0; k < count; k++)
            {
                int prefab_index = (int)final_index_in_ready_set - k;
                indexes[count - 1 - k] = prefab_index;
            }
        }

        return indexes;
    }
}
