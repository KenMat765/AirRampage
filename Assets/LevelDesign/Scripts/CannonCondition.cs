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

    // Necessary overrides (Cannon does not have CP and revivalTime)
    public override float my_cp { get; set; } = 0;
    public override float revivalTime { get; set; } = 0;
}
