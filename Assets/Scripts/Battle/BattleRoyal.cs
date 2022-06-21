using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleRoyal : MonoBehaviour
{
    // Points of each players + zakos
    public static int[] points = new int[GameInfo.max_player_count + 2];
    public const int point_player = 1000;
    public const int point_zako = 200;
}
