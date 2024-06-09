using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpawnPointZako))]
public class Terminal1 : Terminal
{
    public SpawnPointZako spawnPoint { get; private set; }

    public override void SetupTerminal()
    {
        // Assign spawnPoint first, because this is used in base.Start().
        spawnPoint = GetComponent<SpawnPointZako>();
        base.SetupTerminal();
    }

    protected override void ChangeTerminalTeam(Team new_team)
    {
        base.ChangeTerminalTeam(new_team);
        spawnPoint.team = new_team;
    }
}
