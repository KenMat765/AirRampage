using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class CrystalArea : MonoBehaviour
{
    public Team team;

    public Transform[] placements = new Transform[CrystalManager.crystal_count];
    public bool[] placed = new bool[CrystalManager.crystal_count];
    public bool acceptCrystal;

    [Button]
    void InitPlacements()
    {
        for (int k = 0; k < placements.Length; k++)
        {
            placements[k] = transform.GetChild(k);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (!acceptCrystal)
        {
            return;
        }

        // Return when col was not crystal.
        GameObject col_obj = col.gameObject;
        int col_layer = col_obj.layer;
        if (col_layer != LayerMask.NameToLayer("Crystal"))
        {
            return;
        }

        // Get crystal if oppenents crystal.
        Crystal crystal;
        if (col.TryGetComponent<Crystal>(out crystal))
        {
            Team crystal_team = crystal.team;
            if (crystal_team != team)
            {
                OnGetCrystal(crystal);
            }
        }
        else
        {
            Debug.LogError("Could not get component Crystal!!", col_obj);
        }
    }

    void OnGetCrystal(Crystal crystal)
    {
        crystal.ChangeTeam(team);
        SetNewPlacementPos(crystal);
        crystal.ReleaseCrystal();   // Call this AFTER new placement postion is set.
        CrystalManager.I.OnCrystalMoved(this, crystal);
    }

    public void SetNewPlacementPos(Crystal crystal)
    {
        int id = -1;
        for (int k = 0; k < placed.Length; k++)
        {
            if (!placed[k])
            {
                id = k;
                break;
            }
        }

        if (id == -1)
        {
            Debug.LogError("All placement pos is occupied!!");
            return;
        }
        else
        {
            placed[id] = true;
            Vector3 placement_pos = placements[id].position;
            crystal.placementPos = placement_pos;
        }
    }
}
