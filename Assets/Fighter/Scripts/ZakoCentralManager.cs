using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ZakoCentralManager : Singleton<ZakoCentralManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    public List<SpawnPointZako> spawnPointZakos { get; set; } = new List<SpawnPointZako>();
    public List<int> standbyZakoNos { get; set; } = new List<int>();

    float sortie_timer = 0;
    const float sortie_interval = 5;


    void Start()
    {
        MakeFighterArrays(4);
    }


    // Sorties zakos when standby zako count is over fighter_in_array.
    void FixedUpdate()
    {
        if (!BattleConductor.gameInProgress) return;

        if (!BattleInfo.isHost) return;

        sortie_timer += Time.deltaTime;
        if (sortie_timer < sortie_interval) return;

        foreach (SpawnPointZako spawnPointZako in spawnPointZakos)
        {
            if (spawnPointZako.ready_for_sortie)
            {
                StartCoroutine(SortieProcess(spawnPointZako));
                spawnPointZako.standbyCount -= FighterArray.fighter_in_array;
                sortie_timer = 0;
            }
        }
    }


    IEnumerator SortieProcess(SpawnPointZako spawnPoint)
    {
        Team team = spawnPoint.team;

        // Get fighter array and activate.
        FighterArray fighterArray = GetFighterArray();
        fighterArray.Activate(team, spawnPoint.transform.position);

        // Pick up zakos to sortie.
        int[] sortie_zako_nos = standbyZakoNos.Take(FighterArray.fighter_in_array).ToArray();

        // Setup zakos.
        int[] angles = { 30, 90, 150, 210, 270, 330 };    // Angles of exits of termial1.
        int angle = angles[Random.Range(0, angles.Length)]; // Select random exits.
        int fighter_array_index = 0;
        foreach (int zako_no in sortie_zako_nos)
        {
            GameObject fighter = ParticipantManager.I.fighterInfos[zako_no].fighter;
            ZakoCondition condition = (ZakoCondition)ParticipantManager.I.fighterInfos[zako_no].fighterCondition;
            ZakoMovement movement = (ZakoMovement)condition.movement;

            // Tell zako where you were spawned.
            condition.spawnPointNo.Value = spawnPoint.pointNo;

            // Move fighter.
            fighter.transform.position = spawnPoint.transform.position;

            // Rotate fighter.
            angle = (angle + 60) % 360;
            fighter.transform.Rotate(new Vector3(0, angle, 0), Space.World);

            // Remove zako from standbys.
            standbyZakoNos.Remove(zako_no);

            // Activate fighter.
            ParticipantManager.I.FighterActivationHandlerClientRpc(zako_no, true);

            // Check fighter team before sortie.
            if (condition.fighterTeam.Value != team)
            {
                condition.ChangeTeamClientRpc(team);
            }

            // Send FighterArray and point_index to ZakoCondition.
            condition.fighterArray = fighterArray;
            movement.array_point = fighterArray.points[fighter_array_index];
            fighter_array_index++;
        }

        yield return new WaitForSeconds(4);

        if (!BattleConductor.gameInProgress) yield break;

        foreach (int zako_no in sortie_zako_nos)
        {
            // Enable controll & attack after waiting for few seconds.
            ParticipantManager.I.FighterControllHandlerClientRpc(zako_no, true);
            ParticipantManager.I.FighterAttackHandlerClientRpc(zako_no, true);
        }
    }



    // ================================================================================================ //
    // === Fighter Array === //
    // ================================================================================================ //

    [SerializeField] GameObject fighterArrayPrefab;
    List<FighterArray> fighterArrays = new List<FighterArray>();

    List<FighterArray> MakeFighterArrays(int count)
    {
        List<FighterArray> new_fighterArrays = new List<FighterArray>();
        for (int k = 0; k < count; k++)
        {
            GameObject fighter_array_obj = Instantiate(fighterArrayPrefab, transform.position, Quaternion.identity);
            FighterArray fighter_array = fighter_array_obj.GetComponent<FighterArray>();
            fighter_array.Setup();
            fighter_array.gameObject.SetActive(false);
            new_fighterArrays.Add(fighter_array);
        }
        fighterArrays.AddRange(new_fighterArrays);
        return new_fighterArrays;
    }

    FighterArray GetFighterArray()
    {
        foreach (FighterArray fighterArray in fighterArrays)
        {
            if (fighterArray.standby) return fighterArray;
        }
        FighterArray new_fighterArray = MakeFighterArrays(1)[0];
        return new_fighterArray;
    }
}
