using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class SpawnPoints : MonoBehaviour
{
    [SerializeField] SpawnPoint[] spawnPoints;
    [ReadOnly] public int zakoCountAll;

    // Called from Editor.
    [ContextMenu("Initialize Spawn Points")]
    void InitializeSpawnPoints()
    {
        int red = 0, blue = 0, zako = 0;
        foreach (SpawnPoint point in spawnPoints)
        {
            if (point.spawnZako)
            {
                zakoCountAll += point.zakoCount;
                point.pointNo = zako;
                zako++;
            }
            else
            {
                if (point.team == Team.RED)
                {
                    point.pointNo = red;
                    red++;
                }
                else
                {
                    point.pointNo = blue;
                    blue++;
                }
            }
        }
    }

    public SpawnPoint GetSpawnPoint(int fighterNo)
    {
        SpawnPoint result = null;
        Team team = GameInfo.GetTeamFromNo(fighterNo);
        int blockNo = GameInfo.GetBlockNoFromNo(fighterNo);
        foreach (SpawnPoint point in spawnPoints)
        {
            if (point.team == team && point.pointNo == blockNo)
            {
                result = point;
                break;
            }
        }
        return result;
    }

    public SpawnPoint GetSpawnPointZako(int pointNo)
    {
        SpawnPoint result = null;
        foreach (SpawnPoint point in spawnPoints)
        {
            if (point.spawnZako && point.pointNo == pointNo)
            {
                result = point;
                break;
            }
        }
        return result;
    }
}
