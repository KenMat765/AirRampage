using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ゲーム内で不変の値を保持
public class GameInfo : MonoBehaviour
{
    public static int max_skill_count { get; } = 5;
    public static int deck_count { get; } = 4;
    public static int max_player_count { get; } = 8;
    public static Team GetTeamFromNo(int No)
    {
        // If No is even, team is Red.
        if (No % 2 == 0) return Team.RED;
        else return Team.BLUE;

        // if(No < 4) return Team.Red;
        // else return Team.Blue;
    }
    public static int[] GetNosFromTeam(Team team)
    {
        if (team == Team.RED)
        {
            int[] Nos = { 0, 2, 4, 6 };
            // int[] Nos = {0, 1, 2, 3};
            return Nos;
        }
        else
        {
            int[] Nos = { 1, 3, 5, 7 };
            // int[] Nos = {4, 5, 6, 7};
            return Nos;
        }
    }
    public static int GetNoFromTeam(Team team, int number)
    {
        if (team == Team.RED)
        {
            int[] Nos = { 0, 2, 4, 6 };
            // int[] Nos = {0, 1, 2, 3};
            return Nos[number];
        }
        else
        {
            int[] Nos = { 1, 3, 5, 7 };
            // int[] Nos = {4, 5, 6, 7};
            return Nos[number];
        }
    }
    public static int GetBlockNoFromNo(int No)
    {
        Team team = GetTeamFromNo(No);
        int[] Nos = GetNosFromTeam(team);
        for (int k = 0; k < max_player_count / 2; k++)
        {
            if (Nos[k] == No) return k;
        }
        return 0;
    }
}

public enum Team { RED, BLUE }

public enum Rule { BATTLEROYAL, TERMINAL }