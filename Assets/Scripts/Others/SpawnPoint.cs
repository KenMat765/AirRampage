using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class SpawnPoint : MonoBehaviour
{
    // What number of which team?
    // red:0,1,...; blue:0,1,...; zako:0,1,...
    [ReadOnly] public int pointNo;

    [OnValueChanged("SpawnZakoValueChanged")]
    public bool spawnZako;
    void SpawnZakoValueChanged() { if (spawnZako) team = Team.NONE; }

    // Zako spawn point dose not have team.
    [ShowIf("!spawnZako")] public Team team;
    [ShowIf("spawnZako")] public int zakoCount;

    // From which index to which index of ParticipantManager.fighterInfos dose this spawn point occupy.
    // This should be assigned from ParticipantManager at the timing of fighter spawning.
    public int from_inclusive { get; set; }
    public int to_exclusive
    {
        get
        {
            if (spawnZako) { return from_inclusive + zakoCount; }
            else { return from_inclusive + 1; }
        }
    }



    void ChangeZakoTeam(Team new_team)
    {

    }
}
