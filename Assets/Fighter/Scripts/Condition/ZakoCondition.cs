using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ZakoCondition : FighterCondition
{
    public FighterArray fighterArray { get; set; }

    [Header("Rader Icon")]
    [SerializeField] RadarIconController radarIcon;

    [Header("Body for each Team")]
    [SerializeField] GameObject red_body;
    [SerializeField] GameObject blue_body;

    public NetworkVariable<int> spawnPointNo { get; set; } = new NetworkVariable<int>();

    public void ChangeTeam(Team new_team)
    {
        if (new_team == Team.NONE)
        {
            Debug.LogError("ZakoのチームをNONEに変更できません!!");
            return;
        }

        // fighterTeam is synced among clients, so only the host needs to change this.
        if (IsHost) fighterTeam.Value = new_team;
        SetLayerMasks(new_team);
        radarIcon.ChangeRadarIconColor(new_team);

        if (new_team == Team.RED)
        {
            gameObject.layer = LayerMask.NameToLayer("RedFighter");
            body.layer = LayerMask.NameToLayer("RedBody");
            red_body.SetActive(true);
            blue_body.SetActive(false);
        }
        else if (new_team == Team.BLUE)
        {
            gameObject.layer = LayerMask.NameToLayer("BlueFighter");
            body.layer = LayerMask.NameToLayer("BlueBody");
            red_body.SetActive(false);
            blue_body.SetActive(true);
        }
    }

    [ClientRpc] public void ChangeTeamClientRpc(Team new_team) => ChangeTeam(new_team);

    protected override void OnDeath(int killer_no, string cause_of_death)
    {
        base.OnDeath(killer_no, cause_of_death);
        if (IsHost)
        {
            // Subtract self from zako_left in FighterArray.
            fighterArray.zako_left--;
        }
    }

    protected override void OnRevival()
    {
        base.OnRevival();
        SpawnPointZako spawnPoint = BattleConductor.spawnPointManager.GetSpawnPointZako(spawnPointNo.Value);

        // Add self to standbys in SpawnPointZako & ZakoCentralManager.
        spawnPoint.standbyCount++;
        ZakoCentralManager.I.standbyZakoNos.Add(fighterNo.Value);

        // Deactivate zako. Zako will be activated from spawn point.
        // Call this on revival (not on death), because revive count is done at condition's update.
        ParticipantManager.I.FighterActivationHandler(fighterNo.Value, false);
    }
}
