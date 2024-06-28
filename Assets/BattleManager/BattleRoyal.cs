using System.Collections;
using System.Collections.Generic;

public class BattleRoyal : BattleRule
{
    // Points of each players + zakos
    public static int[] points = new int[GameInfo.MAX_PLAYER_COUNT + 2];
    public const int point_player = 1000;
    public const int point_zako = 200;
}
