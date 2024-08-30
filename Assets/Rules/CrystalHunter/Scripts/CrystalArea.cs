using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalArea : MonoBehaviour
{
    // This is set in CrystalManager.InitCrystals
    CrystalManager crystalManager;

    public Team team;
    public CrystalHolder[] holders { get; private set; }
    public bool acceptCrystal { get; set; }


    // Called in CrystalManager.InitCrystals
    public void Init(CrystalManager manager)
    {
        crystalManager = manager;
        holders = GetComponentsInChildren<CrystalHolder>();
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
            Team crystal_team = crystal.GetTeam();
            if (crystal_team != team)
            {
                GetCrystal(crystal);
            }
        }
        else
        {
            Debug.LogError("Could not get Crystal component!!", col_obj);
        }
    }


    void GetCrystal(Crystal crystal)
    {
        crystal.SetTeam(team);
        CrystalHolder vacant_holder = GetVacantHolder();
        if (vacant_holder)
        {
            vacant_holder.GetCrystal(crystal);
            Vector3 new_homePos = vacant_holder.position;
            crystal.SetHome(new_homePos);
        }

        // Release crystal from carrier fighter. Call this AFTER new placement postion is set.
        crystal.ReleaseCrystal();

        crystalManager.OnCrystalMoved(this, crystal);
    }

    public void ReleaseCrystal(Crystal crystal)
    {
        foreach (CrystalHolder holder in holders)
        {
            if (holder.crystalId == crystal.id)
            {
                holder.ReleaseCrystal();
            }
        }
    }


    public CrystalHolder GetVacantHolder()
    {
        foreach (CrystalHolder holder in holders)
        {
            if (!holder.IsOccupied())
            {
                return holder;
            }
        }
        Debug.LogWarning("All holders where occupied");
        return null;
    }
}
