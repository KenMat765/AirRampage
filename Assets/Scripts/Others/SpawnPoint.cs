using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class SpawnPoint : MonoBehaviour
{
    // What number of which team?
    [ReadOnly] public int pointNo;
    public Team team;
    public bool spawnZako;
    [ShowIf("spawnZako")] public int zakoCount;
}
