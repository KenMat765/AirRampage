using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ゲーム内で不変の値を保持
public class GameInfo : MonoBehaviour
{
    // Number of players.
    public const int MAX_PLAYER_COUNT = 8;
    public const int TEAM_MEMBER_COUNT = MAX_PLAYER_COUNT / 2;

    // Skills.
    public const int MAX_SKILL_COUNT = 5;
    public const int DECK_COUNT = 4;

    // Ability.
    public const int MAX_WEIGHT = 100;

    // Coins.
    public const int S_GENERATE_COIN = 300;
    public const int A_GENERATE_COIN = 100;
    public static int[] upgrade_coin { get; } = { 100, 200, 300, 400 };

    // Game Time.
    public const int MAX_TIME_SEC = 600;  // seconds
    public const int MIN_TIME_SEC = 180;  // seconds

    // LayerMask.
    public static LayerMask terrainMask { get; } = 1 << 6;
    public static LayerMask structureMask { get; } = 1 << 22;
}

public enum Team { RED, BLUE, NONE }

public enum Rule { CRYSTALHUNTER, BATTLEROYAL, TERMINALCONQUEST, }

public enum Stage { SPACE, CANYON, SNOWPEAK }