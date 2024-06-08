using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

// CrystalManager
//  - Crystals
//  - CrystalAreas
public class CrystalManager : Singleton<CrystalManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    public const int crystal_count = 6; // All crystals in scene.
    public Crystal[] crystals = new Crystal[crystal_count];
    public int[] carrierNos = new int[crystal_count];   // Carrier fighter's number of each crystals.
    public CrystalArea red_crystalArea, blue_crystalArea;

    [Button]
    public void InitCrystals()
    {
        // Crystals
        for (int id = 0; id < crystal_count; id++)
        {
            Crystal crystal = transform.GetChild(id).GetComponent<Crystal>();
            // crystal.id = id; // id resets when the scene changes (?)
            crystals[id] = crystal;
        }

        // Crystal Areas
        red_crystalArea = transform.GetChild(crystal_count).GetComponent<CrystalArea>();
        blue_crystalArea = transform.GetChild(crystal_count + 1).GetComponent<CrystalArea>();
    }

    public void SetupCrystals()
    {
        if (BattleInfo.isMulti && !BattleInfo.isHost) return;

        int red_score = 0;
        int blue_score = 0;
        for (int id = 0; id < crystal_count; id++)
        {
            Crystal crystal = crystals[id];
            // Red
            if (id % 2 == 0)
            {
                crystal.ChangeTeam(Team.RED);
                red_crystalArea.SetNewPlacementPos(crystal);
                crystal.transform.position = crystal.placement_pos;
                red_score++;
            }
            // Blue
            else
            {
                crystal.ChangeTeam(Team.BLUE);
                blue_crystalArea.SetNewPlacementPos(crystal);
                crystal.transform.position = crystal.placement_pos;
                blue_score++;
            }

            // Initialize carrier numbers.
            carrierNos[id] = -1;
        }
        BattleConductor.I.RedScore = red_score;
        BattleConductor.I.BlueScore = blue_score;
    }

    public void OnCrystalMoved(CrystalArea get_crystalArea, Crystal get_crystal)
    {
        int crystal_id = get_crystal.id;
        if (get_crystalArea == red_crystalArea)
        {
            BattleConductor.I.RedScore++;
            BattleConductor.I.BlueScore--;
            blue_crystalArea.placed[crystal_id] = false;
        }
        else if (get_crystalArea == blue_crystalArea)
        {
            BattleConductor.I.RedScore--;
            BattleConductor.I.BlueScore++;
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
