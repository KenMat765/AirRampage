using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalHolder : MonoBehaviour
{
    /// <summary>Does not hold any crystals when negative.</summary>
    public int crystalId { get; private set; } = -1;
    public Vector3 position { get; private set; }

    void Awake()
    {
        // Cache position of this holder here, because it does not move during game.
        position = transform.position;
    }

    public bool IsOccupied()
    {
        return crystalId >= 0;
    }

    public void GetCrystal(Crystal crystal)
    {
        if (IsOccupied())
        {
            Debug.LogError("CrystalHolder already occupied");
            return;
        }
        crystalId = crystal.id;
    }

    public void ReleaseCrystal()
    {
        if (!IsOccupied())
        {
            Debug.LogError("CrystalHolder does not hold any crystals");
            return;
        }
        crystalId = -1;
    }
}
