using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ゲーム内で不変の値を保持
public class GameInfo : MonoBehaviour
{
    // Number of players.
    public static int max_player_count { get; } = 8;
    public static int team_member_count { get; } = max_player_count / 2;

    // Skills.
    public static int max_skill_count { get; } = 5;
    public static int deck_count { get; } = 4;

    // Ability.
    public static int max_weight { get; } = 100;

    // Coins.
    public static int s_generate_coin { get; } = 300;
    public static int a_generate_coin { get; } = 100;
    public static int[] upgrade_coin { get; } = { 100, 200, 300, 400 };

    // Game Time.
    public static int max_time_sec { get; } = 600;  // seconds
    public static int min_time_sec { get; } = 180;  // seconds

    // LayerMask.
    public static LayerMask terrainMask { get; } = 1 << 6;
}

public enum Team { RED, BLUE, NONE }

public enum Rule { CRYSTALHUNTER, BATTLEROYAL, TERMINALCONQUEST, }

public enum Stage { SPACE, CANYON, SNOWPEAK }