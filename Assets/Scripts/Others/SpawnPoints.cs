using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Has to be called before ParticipantManager.
[DefaultExecutionOrder(-1)]
public class SpawnPoints : MonoBehaviour
{
    public static SpawnPoint[] spawnPoints { get; private set; }
    public static int zakoCountAll { get; private set; }
    public static int zakoCountPerTeam { get { return zakoCountAll / 2;} }

    [ContextMenu("Distribute Point Numbers")]
    void DistributePointNumbers()
    {
        spawnPoints = GetComponentsInChildren<SpawnPoint>();
        int red = 0, blue = 0, redZako = 0, blueZako = 0;
        foreach(SpawnPoint point in spawnPoints)
        {
            if(point.spawnZako)
            {
                if(point.team == Team.Red)
                {
                    point.pointNo = redZako;
                    redZako ++;
                }
                else
                {
                    point.pointNo = blueZako;
                    blueZako ++;
                }
            }
            else
            {
                if(point.team == Team.Red)
                {
                    point.pointNo = red;
                    red ++;
                }
                else
                {
                    point.pointNo = blue;
                    blue ++;
                }
            }
        }
    }

    void Awake()
    {
        spawnPoints = GetComponentsInChildren<SpawnPoint>();
        foreach(SpawnPoint point in spawnPoints)
        {
            if(point.spawnZako) zakoCountAll += point.zakoCount;
        }
    }

    public static SpawnPoint GetSpawnPoint(int fighterNo)
    {
        SpawnPoint result = null;
        Team team = GameInfo.GetTeamFromNo(fighterNo);
        int blockNo = GameInfo.GetBlockNoFromNo(fighterNo);
        foreach(SpawnPoint point in spawnPoints)
        {
            if(point.team == team && point.pointNo == blockNo)
            {
                result = point;
                break;
            }
        }
        return result;
    }

    public static SpawnPoint GetSpawnPointZako(Team team, int pointNo)
    {
        SpawnPoint result = null;
        foreach(SpawnPoint point in spawnPoints)
        {
            if(point.spawnZako && point.team == team && point.pointNo == pointNo)
            {
                result = point;
                break;
            }
        }
        return result;
    }
}
