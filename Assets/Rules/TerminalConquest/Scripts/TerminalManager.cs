using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TerminalManager : RuleManager
{
    Terminal[] terminals;

    public static int allTerminalCount { get; private set; }
    public static int redTerminalCount { get; private set; }
    public static int blueTerminalCount { get; private set; }

    public static float redPoint_per_second;
    public static float bluePoint_per_second;



    public override void Setup()
    {
        terminals = GetComponentsInChildren<Terminal>();

        allTerminalCount = terminals.Length;
        redTerminalCount = 0;
        blueTerminalCount = 0;

        redPoint_per_second = 0;
        bluePoint_per_second = 0;

        foreach (Terminal terminal in terminals)
        {
            terminal.SetupTerminal();
            if (terminal.team == Team.RED) redTerminalCount++;
            else if (terminal.team == Team.BLUE) blueTerminalCount++;
        }
    }

    public override void OnGameStart()
    {
        TerminalsAcceptDamageHandler(true);
    }

    public override void OnGameEnd()
    {
        TerminalsAcceptDamageHandler(false);
    }



    void FixedUpdate()
    {
        if (!BattleConductor.gameInProgress) return;
        if (!NetworkManager.Singleton.IsHost) return;

        float delta_red_score = redPoint_per_second * Time.deltaTime;
        float delta_blue_score = bluePoint_per_second * Time.deltaTime;
        ScoreManager.I.AddScore(delta_red_score, Team.RED);
        ScoreManager.I.AddScore(delta_blue_score, Team.BLUE);
    }


    public void TerminalsAcceptDamageHandler(bool accept)
    {
        foreach (Terminal terminal in terminals) terminal.acceptDamage = accept;
    }


    public void OnTerminalFallEvent(Team old_team, Team new_team)
    {
        switch (old_team)
        {
            case Team.RED: redTerminalCount--; break;
            case Team.BLUE: blueTerminalCount--; break;
        }
        switch (new_team)
        {
            case Team.RED: redTerminalCount++; break;
            case Team.BLUE: blueTerminalCount++; break;
        }
    }


    public Terminal GetTerminalByNo(int no)
    {
        Terminal result = null;
        foreach (Terminal terminal in terminals)
        {
            if (terminal.No == no)
            {
                result = terminal;
                break;
            }
        }
        return result;
    }


    ///<summary> Returns list of ally terminals </summary>
    public List<Terminal> GetAllyTerminals(Team my_team)
    {
        List<Terminal> result = new List<Terminal>();
        foreach (Terminal terminal in terminals)
        {
            if (terminal.team != my_team) continue;
            result.Add(terminal);
        }
        return result;
    }


    ///<summary> Returns list of opponent (including default) terminals </summary>
    public List<Terminal> GetOpponentTerminals(Team my_team)
    {
        List<Terminal> result = new List<Terminal>();
        foreach (Terminal terminal in terminals)
        {
            if (terminal.team == my_team) continue;
            result.Add(terminal);
        }
        return result;
    }



    ///<summary> Returns nearest opponent terminal. </summary>
    public Terminal GetNearestOpponentTerminal(Team my_team, Vector3 my_position)
    {
        List<Terminal> opponent_terminals = GetOpponentTerminals(my_team);

        // If there are no opponent terminal.
        if (opponent_terminals.Count < 1) return null;

        Terminal result = opponent_terminals[0];
        Vector3 terminal_position = opponent_terminals[0].transform.position;
        float min_distance = Vector3.SqrMagnitude(terminal_position - my_position);
        foreach (Terminal terminal in opponent_terminals)
        {
            terminal_position = terminal.transform.position;
            float distance = Vector3.SqrMagnitude(terminal_position - my_position);
            if (distance < min_distance)
            {
                min_distance = distance;
                result = terminal;
            }
        }

        return result;
    }


    ///<summary> Try to get owner terminals. </summary>
    ///<return> Boolean whether owner terminals were found. </return>
    public bool TryGetOwnerTerminals(int fighter_no, out List<Terminal> result)
    {
        bool found = false;
        result = new List<Terminal>();
        foreach (Terminal terminal in terminals)
        {
            if (terminal.ownerFighterNo == fighter_no)
            {
                result.Add(terminal);
                if (!found) found = true;
            }
        }
        return found;
    }
}
