using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cysharp.Threading.Tasks.Triggers;

public class CrystalManager : RuleManager
{
    public const int CRYSTAL_COUNT = 6; // All crystals in game.

    Crystal[] crystals;
    CrystalArea red_crystalArea, blue_crystalArea;


    void Init()
    {
        // ===== Crystal Areas ===== //
        CrystalArea[] crystal_areas = GetComponentsInChildren<CrystalArea>();

        if (crystal_areas.Length != 2)
        {
            Debug.LogError($"There are more or less crystal areas than 2");
            return;
        }

        foreach (CrystalArea area in crystal_areas)
        {
            area.Init(this);
            switch (area.team)
            {
                case Team.RED: red_crystalArea = area; break;
                case Team.BLUE: blue_crystalArea = area; break;
                default: Debug.LogError($"Team of CrystalArea is invalid", area.gameObject); return;
            }
        }

        // Initialize Crystals AFTER CrystalAreas, because home position is necessary to init crystals.
        // ===== Crystals ===== //
        crystals = GetComponentsInChildren<Crystal>();

        if (crystals.Length != CRYSTAL_COUNT)
        {
            Debug.LogError($"There are more or less crystals than {CRYSTAL_COUNT}");
            return;
        }

        for (int id = 0; id < CRYSTAL_COUNT; id++)
        {
            Crystal crystal = crystals[id];
            Team crystal_team = crystal.GetTeam();
            CrystalHolder vacant_holder;
            switch (crystal_team)
            {
                case Team.RED: vacant_holder = red_crystalArea.GetVacantHolder(); break;
                case Team.BLUE: vacant_holder = blue_crystalArea.GetVacantHolder(); break;
                default: Debug.LogError("Invalid crystal team", crystal.gameObject); return;
            }
            if (!vacant_holder)
            {
                Debug.LogError($"Could not find vacant crystal holder for team: {crystal_team}");
                return;
            }
            Vector3 home_pos = vacant_holder.position;
            crystal.Init(this, id, home_pos);
            vacant_holder.GetCrystal(crystal);
        }
    }


    public override void Setup()
    {
        Init(); // All clients must initialize crystals.

        if (!NetworkManager.Singleton.IsHost) return;

        int red_score = 0;
        int blue_score = 0;
        foreach (Crystal crystal in crystals)
        {
            Team crystal_team = crystal.GetTeam();
            int crystal_score = crystal.GetScore();
            switch (crystal_team)
            {
                case Team.RED: red_score += crystal_score; break;
                case Team.BLUE: blue_score += crystal_score; break;
                default: Debug.LogError("Crystal Team was NONE", crystal.gameObject); break;
            }
        }
        ScoreManager.I.SetScore(red_score, Team.RED);
        ScoreManager.I.SetScore(blue_score, Team.BLUE);
    }

    public override void OnGameStart()
    {
        AcceptCrystalHandler(true);
    }

    public override void OnGameEnd()
    {
        AcceptCrystalHandler(false);
    }



    public void OnCrystalMoved(CrystalArea get_crystalArea, Crystal get_crystal)
    {
        int score = get_crystal.GetScore();
        if (get_crystalArea == red_crystalArea)
        {
            ScoreManager.I.AddScore(score, Team.RED);
            ScoreManager.I.AddScore(-score, Team.BLUE);
            blue_crystalArea.ReleaseCrystal(get_crystal);
        }
        else if (get_crystalArea == blue_crystalArea)
        {
            ScoreManager.I.AddScore(-score, Team.RED);
            ScoreManager.I.AddScore(score, Team.BLUE);
            red_crystalArea.ReleaseCrystal(get_crystal);
        }
        else
        {
            Debug.LogError("Called from unknown crystal area!!");
            return;
        }
    }

    public void AcceptCrystalHandler(bool accept)
    {
        red_crystalArea.acceptCrystal = accept;
        blue_crystalArea.acceptCrystal = accept;
    }


    /// <summary>Checks whether fighter is currently carrying crystal</summary>
    public bool IsFighterCarryingCrystal(int fighter_no)
    {
        foreach (Crystal crystal in crystals)
        {
            int carrier_no = crystal.GetCarrierNo();
            if (carrier_no == fighter_no)
            {
                return true;
            }
        }
        return false;
    }
}
