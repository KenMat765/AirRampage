using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonCondition : FighterCondition
{
    // Constants.
    public const string CANNON_NAME = "Canon";
    public const int CANNON_NO = -2;

    // Set from Inspector.
    [Header("Cannon Settings")]
    public Team team;

    protected override void Start()
    {
        base.Start();
        InitStatus();
    }
}
