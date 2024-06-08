using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    // All Spawn Point Fighter should be children of Spawn Points.
    [SerializeField] SpawnPointFighter[] fighterSpawnPoints;
    [SerializeField] SpawnPointZako[] zakoSpawnPoints;
    public int zakoCountAll;

    // Called from Editor.
    [ContextMenu("Initialize Spawn Points")]
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
                Debug.LogError("SpawnPointFighterのTeamが<color=red>RED</color>か<color=blue>BLUE</color>以外になっています!!");
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

    public SpawnPointFighter GetSpawnPointFighter(int fighterNo)
    {
        SpawnPointFighter result = null;
        Team team = GameInfo.GetTeamFromNo(fighterNo);
        int blockNo = GameInfo.GetBlockNoFromNo(fighterNo);
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
