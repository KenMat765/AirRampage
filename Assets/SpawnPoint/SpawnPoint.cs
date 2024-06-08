using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpawnPoint : MonoBehaviour
{
    public int pointNo;

    // Can not be NONE for spawn point fighter.
    // Only necessary for spawn point zako when game rule is Battle Royal.
    public Team team;

    public abstract void SetupSpawnPoint();
}
