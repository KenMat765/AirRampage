using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// CrystalManager
//  - Crystals
//  - CrystalAreas
public class CrystalManager : RuleManager
{
    public const int CRYSTAL_COUNT = 6; // All crystals in scene.
    public Crystal[] crystals;
    public CrystalArea red_crystalArea, blue_crystalArea;
    public int[] carrierNos { get; private set; } = new int[CRYSTAL_COUNT];   // Carrier fighter's number of each crystals.


    void InitCrystals()
    {
        // Init Crystals
        crystals = GetComponentsInChildren<Crystal>();

        if (crystals.Length != CRYSTAL_COUNT)
        {
            Debug.LogError($"There are more or less crystals than {CRYSTAL_COUNT}");
            return;
        }

        for (int id = 0; id < CRYSTAL_COUNT; id++)
        {
            crystals[id].Init(this, id);
        }

        // Init Crystal Areas
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
                case Team.RED:
                    red_crystalArea = area;
                    break;
                case Team.BLUE:
                    blue_crystalArea = area;
                    break;
                default:
                    Debug.LogError($"Team of CrystalArea is invalid", area.gameObject);
                    return;

            }
        }
    }



    public override void Setup()
    {
        InitCrystals(); // All clients must initialize crystals.

        if (!NetworkManager.Singleton.IsHost) return;

        int red_score = 0;
        int blue_score = 0;
        for (int id = 0; id < CRYSTAL_COUNT; id++)
        {
            Crystal crystal = crystals[id];
            // Red
            if (id % 2 == 0)
            {
                crystal.ChangeTeam(Team.RED);
                red_crystalArea.SetNewPlacementPos(crystal);
                crystal.transform.position = crystal.placementPos;
                red_score++;
            }
            // Blue
            else
            {
                crystal.ChangeTeam(Team.BLUE);
                blue_crystalArea.SetNewPlacementPos(crystal);
                crystal.transform.position = crystal.placementPos;
                blue_score++;
            }

            // Initialize carrier numbers.
            carrierNos[id] = -1;
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
        int crystal_id = get_crystal.id;
        if (get_crystalArea == red_crystalArea)
        {
            ScoreManager.I.AddScore(1, Team.RED);
            ScoreManager.I.AddScore(-1, Team.BLUE);
            blue_crystalArea.placed[crystal_id] = false;
        }
        else if (get_crystalArea == blue_crystalArea)
        {
            ScoreManager.I.AddScore(-1, Team.RED);
            ScoreManager.I.AddScore(1, Team.BLUE);
            red_crystalArea.placed[crystal_id] = false;
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
}
