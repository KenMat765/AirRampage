using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointZako : SpawnPoint
{
    public int zakoCount;
    public int standbyCount { get; set; }
    public bool ready_for_sortie
    {
        get { return standbyCount >= FighterArray.fighter_in_array && team != Team.NONE; }
    }

    public override void SetupSpawnPoint()
    {
        standbyCount = zakoCount;
        ZakoCentralManager.I.spawnPointZakos.Add(this);
    }
}
