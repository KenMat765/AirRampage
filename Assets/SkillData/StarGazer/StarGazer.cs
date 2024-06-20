using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

public class StarGazer : SkillAttack
{
    // スキルレベルによって変更の可能性があるパラメータ
    float generate_time;
    float startup_time;
    protected override void ParameterUpdater()
    {
        base.ParameterUpdater();
        generate_time = levelData.FreeFloat1;
        startup_time = levelData.FreeFloat2;
    }

    GameObject gazer_root;
    bool generating = false;
    float timer;
    Tweener root_tweener;

    protected override void Update()
    {
        base.Update();

        if (generating)
        {
            timer += Time.deltaTime;
            if (timer > generate_time) EndProccess();
        }
    }

    public override void Generator()
    {
        base.Generator();

        // gazer_rootの初期設定
        gazer_root = Instantiate(TeamPrefabGetter(), transform);
        gazer_root.transform.localPosition = Vector3.zero;
        // rotationは発射時に毎回補正する
        gazer_root.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        // prefabs、weaponsの初期設定
        original_prefab = gazer_root.transform.Find("Projectile_StarGazer").gameObject;
        SetPrefabLocalTransform(Vector3.zero, Vector3.zero, new Vector3(1.5f, 1.5f, 1.5f));
        const int gazer_default_count = 15;
        for (int k = 0; k < gazer_default_count; k++) GeneratePrefab(gazer_root.transform);
    }

    public override void Activator(int[] transfer = null)
    {
        base.Activator();
        MeterDecreaser(startup_time + generate_time);

        // rootの発射準備
        gazer_root.SetActive(true);
        gazer_root.transform.parent = null;

        // 機体が傾いている時でも上方へ行くように、rootの角度を調整
        Vector3 euler_angle = transform.rotation.eulerAngles;
        float euler_y = euler_angle.y;
        gazer_root.transform.rotation = Quaternion.Euler(-30, euler_y, 0);

        // rootを発射
        const float root_distance = 170;
        root_tweener = gazer_root.transform.DOMove(gazer_root.transform.forward * root_distance, startup_time)
            .SetRelative()
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                // 弾丸の生成開始
                generating = true;
                StartCoroutine(StartGenerateGazer());
            });

        if (BattleInfo.isMulti && attack.IsOwner)
        {
            // Send Rpc to your clones.
            if (IsHost) attack.SkillActivatorClientRpc(NetworkManager.Singleton.LocalClientId, skillNo);
            else attack.SkillActivatorServerRpc(NetworkManager.Singleton.LocalClientId, skillNo);
        }
    }

    IEnumerator StartGenerateGazer()
    {
        const float interval = 0.03f;
        while (generating)
        {
            int index = GetPrefabIndex(gazer_root.transform);

            // 発射時にrotationをランダムにセット
            prefabs[index].transform.eulerAngles = new Vector3(Random.Range(30, 90), Random.Range(-180, 180), 0);

            weapons[index].Activate(null);
            yield return new WaitForSeconds(interval);
        }
    }

    public override void EndProccess()
    {
        // 弾丸の生成終了
        generating = false;

        // timerの初期化
        timer = 0;

        // rootを元に戻す
        gazer_root.SetActive(false);
        gazer_root.transform.parent = transform;
        gazer_root.transform.localPosition = Vector3.zero;
    }

    public override void ForceTermination()
    {
        base.ForceTermination();
        EndProccess();
        if (root_tweener.IsActive()) root_tweener.Kill();
    }
}
