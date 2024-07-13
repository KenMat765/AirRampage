using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public abstract class Skill : MonoBehaviour
{
    public int skillNo { get; private set; }  // Used to identify which skill to activate when received skill activator RPCs.
    public string skillName { get; private set; }
    public int skillId { get; private set; }
    public SkillType skillType { get; private set; }

    public float charge_time { get; set; }
    public float elapsed_time { get; set; }
    public bool isCharged { get; private set; } = false;
    protected bool ready2Charge { get; private set; } = true;
    public bool isUsing { get; private set; } = false;
    public bool isLocked { get; set; } = false;
    Tweener meter_tweener;

    protected GameObject original_prefab;
    protected List<GameObject> prefabs { get; private set; }

    /// <Summary>
    /// Prefabを生成する際に参照する位置情報。
    /// </Summary>
    protected Vector3 local_position { get; set; }

    /// <Summary>
    /// Prefabを生成する際に参照する角度情報。
    /// </Summary>
    protected Vector3 local_eulerAngle { get; set; }

    /// <Summary>
    /// Prefabを生成する際に参照するスケール情報。
    /// </Summary>
    protected Vector3 local_scale { get; set; }

    /// <Summary>
    /// Prefabを生成する際に参照する位置情報。Prefabごとに位置が異なる場合に使用する。
    /// </Summary>
    protected Vector3[] local_positions { get; set; }

    /// <Summary>
    /// Prefabを生成する際に参照する位置情報。Prefabごとに角度が異なる場合に使用する。
    /// </Summary>
    protected Vector3[] local_eulerAngles { get; set; }

    /// <Summary>
    /// Prefabを生成する際に参照する位置情報。Prefabごとにスケールが異なる場合に使用する。
    /// </Summary>
    protected Vector3[] local_scales { get; set; }

    // Fighter Properties.
    protected SkillExecuter skillExecuter { get; private set; }
    protected Team fighterTeam;

    public abstract void LevelDataSetter(LevelData levelData);
    protected abstract void ParameterUpdater();

    protected virtual void Update()
    {
        Charger();
    }

    public virtual void Generator(int skill_no, SkillData skill_data)
    {
        skillNo = skill_no;

        // Get skill properties from input skill data.
        skillName = skill_data.GetName();
        skillId = skill_data.GetId();
        skillType = skill_data.GetSkillType();

        prefabs = new List<GameObject>();
        skillExecuter = GetComponent<SkillExecuter>();
        fighterTeam = skillExecuter.fighterCondition.fighterTeam.Value;
        ParameterUpdater(); // charge_time is set here.
    }

    public virtual void Activator(int[] data = null)
    {
        if (!isCharged || skillExecuter.fighterCondition.isDead || isLocked)
        {
            return;
        }
        isCharged = false;
        ready2Charge = false;
    }

    void Charger()
    {
        if (isLocked)
        {
            return;
        }

        if (!isCharged && ready2Charge)
        {
            elapsed_time += Time.deltaTime;
            if (elapsed_time >= charge_time)
            {
                isCharged = true;
            }
        }
    }

    /// <Summary>
    /// 終了時の後片付け。
    /// </Summary>
    public virtual void EndProccess() { }

    /// <Summary>
    /// 起動中に死亡した時に呼ばれる。基本的にはEndProcessと同様の処理(一部例外のスキルあり)。
    /// </Summary>
    public virtual void ForceTermination(bool maintain_charge)
    {
        if (meter_tweener.IsActive())
        {
            meter_tweener.Kill();
        }
        // Reset charge of skills.
        if (!maintain_charge || isUsing)
        {
            elapsed_time = 0;
            isCharged = false;
        }
        isUsing = false;
        ready2Charge = true;
    }

    /// <Summary>
    /// メーター(elapsed_time)を時間経過により減少させる。デフォルトでは一瞬で０になる。
    /// </Summary>
    public void MeterDecreaser(float duration = 0, System.Action OnCompleteCallback = null)
    {
        isUsing = true;
        meter_tweener = DOTween.To(() => elapsed_time, (value) => elapsed_time = value, 0, duration)
            .OnComplete(() =>
            {
                ready2Charge = true;
                isUsing = false;
                if (OnCompleteCallback != null) OnCompleteCallback();
            });
    }

    /// <Summary>
    /// メーター(elapsed_time)を手動で変化させる。
    /// シールドなど、メーターの減少に時間経過以外の要因がある場合に使用する。
    /// </Summary>
    public void MeterDecreaserManual(float end_value)
    {
        if (!isUsing) isUsing = true;
        elapsed_time = end_value;
        if (elapsed_time <= 0)
        {
            ready2Charge = true;
            isUsing = false;
        }
    }

    /// <Summary>
    /// Teamに応じてSkillData内のPrefabデータを返す。基本的にはoriginal_prefabに代入する(一部例外のスキルあり)。
    /// </Summary>
    protected GameObject TeamPrefabGetter(Team team)
    {
        switch (team)
        {
            case Team.RED:
                return SkillDatabase.I.SearchSkillByName(this.GetType().Name).GetPrefabRed();
            case Team.BLUE:
                return SkillDatabase.I.SearchSkillByName(this.GetType().Name).GetPrefabBlue();
            default:
                Debug.LogError("Argument team was null", gameObject);
                return null;
        }
    }

    /// <Summary>
    /// Prefab生成時に参照するlocal_transformをセットする。
    /// </Summary>
    protected void SetPrefabLocalTransform(Vector3 localPosition, Vector3 localEulerAngle, Vector3 localScale)
    {
        local_position = localPosition;
        local_eulerAngle = localEulerAngle;
        local_scale = localScale;
    }

    /// <Summary>
    /// local_transformsの各Listを初期化する。
    /// </Summary>
    protected void InitTransformsLists(int count)
    {
        local_positions = new Vector3[count];
        local_eulerAngles = new Vector3[count];
        local_scales = new Vector3[count];
    }

    /// <Summary>
    /// Prefab生成時に参照するlocal_transformsをセットする。
    /// </Summary>
    protected void SetPrefabLocalTransforms(int index, Vector3 localPosition, Vector3 localEulerAngle, Vector3 localScale)
    {
        local_positions[index] = localPosition;
        local_eulerAngles[index] = localEulerAngle;
        local_scales[index] = localScale;
    }

    /// <Summary>
    /// original_prefabを元にprefabを１つだけ生成し、prefabsに追加する。
    /// ！！注意：このメソッドを呼び出す前に、original_prefab、local_position、local_eulerAngle、local_scaleをセットしておくこと！！
    /// </Summary>
    protected virtual GameObject GeneratePrefab(Transform parent = null)
    {
        if (parent == null)
        {
            // If parent is not specified, set parent to fighter_body (= transform)
            parent = transform;
        }
        GameObject prefab = Instantiate(original_prefab, parent);
        prefab.transform.localPosition = local_position;
        prefab.transform.localRotation = Quaternion.Euler(local_eulerAngle);
        prefab.transform.localScale = local_scale;
        prefabs.Add(prefab);
        return prefab;
    }

    /// <Summary>
    /// original_prefabを元にprefabを引数個だけ生成し、prefabsに追加する。
    /// ！！注意：このメソッドを呼び出す前に、original_prefab、local_position、local_eulerAngle、local_scaleをセットしておくこと！！
    /// </Summary>
    protected virtual GameObject[] GeneratePrefabs(int count, Transform parent = null)
    {
        if (parent == null)
        {
            // If parent is not specified, set parent to fighter_body (= transform)
            parent = transform;
        }
        GameObject[] generated_prefabs = new GameObject[count];
        for (int k = 0; k < count; k++)
        {
            GameObject prefab = Instantiate(original_prefab, parent);
            prefab.transform.localPosition = local_positions[k];
            prefab.transform.localRotation = Quaternion.Euler(local_eulerAngles[k]);
            prefab.transform.localScale = local_scales[k];
            prefabs.Add(prefab);
            generated_prefabs[k] = prefab;
        }
        return generated_prefabs;
    }
}