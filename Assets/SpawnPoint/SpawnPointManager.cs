using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class SpawnPointManager : MonoBehaviour
{
    public int zakoCountAll;

    [SerializeField] SpawnPointFighter[] fighterSpawnPoints;
    [SerializeField] SpawnPointZako[] zakoSpawnPoints;


    // Called from Editor.
    [Button("Initialize Spawn Points")]
    void InitializeSpawnPoints()
    {
        // Reset field.
        zakoCountAll = 0;

        // Spawn Point Fighter.
        int red = 0, blue = 0;
        foreach (SpawnPointFighter point in fighterSpawnPoints)
        {
            if (point.team == Team.RED)
            {
                point.pointNo = red;
                red++;
            }
            else if (point.team == Team.BLUE)
            {
                point.pointNo = blue;
                blue++;
            }
            else
            {
                Debug.LogError("SpawnPointFighterのTeamがNULLになっています!!");
            }
        }

        // Spawn Point Zako.
        int zako = 0;
        foreach (SpawnPointZako point in zakoSpawnPoints)
        {
            zakoCountAll += point.zakoCount;
            point.pointNo = zako;
            zako++;
        }
    }


    public void SetupSpawnPoints()
    {
        foreach (SpawnPointFighter sf in fighterSpawnPoints) sf.SetupSpawnPoint();
        foreach (SpawnPointZako sz in zakoSpawnPoints) sz.SetupSpawnPoint();
    }


    public SpawnPointFighter GetSpawnPointFighter(int fighterNo)
    {
        SpawnPointFighter result = null;
        Team team = BattleInfo.battleDatas[fighterNo].team;
        int blockNo = BattleInfo.battleDatas[fighterNo].memberNo;
        foreach (SpawnPointFighter point in fighterSpawnPoints)
        {
            if (point.team == team && point.pointNo == blockNo)
            {
                result = point;
                break;
            }
        }
        return result;
    }


    public SpawnPointZako GetSpawnPointZako(int pointNo)
    {
        SpawnPointZako result = null;
        foreach (SpawnPointZako point in zakoSpawnPoints)
        {
            if (point.pointNo == pointNo)
            {
                result = point;
                break;
            }
        }
        if (result == null)
        {
            Debug.LogWarning($"pointNo : {pointNo} に対応するスポーンポイントが見つかりませんでした。Nullを返します。");
        }
        return result;
    }
}
