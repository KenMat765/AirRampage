using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerminalManager : Singleton<TerminalManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    Terminal[] terminals;
    Terminal1[] terminal1s;
    Terminal2[] terminal2s;

    public static int allTerminalCount { get; private set; }
    public static int redTerminalCount { get; private set; }
    public static int blueTerminalCount { get; private set; }

    public static float redPoint_per_second;
    public static float bluePoint_per_second;


    public void SetupTerminals()
    {
        terminals = GetComponentsInChildren<Terminal>();
        terminal1s = GetComponentsInChildren<Terminal1>();
        terminal2s = GetComponentsInChildren<Terminal2>();

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


    void FixedUpdate()
    {
        if (!BattleConductor.gameInProgress) return;
        if (BattleInfo.isMulti && !BattleInfo.isHost) return;
        BattleConductor.I.RedScore += redPoint_per_second * Time.deltaTime;
        BattleConductor.I.BlueScore += bluePoint_per_second * Time.deltaTime;
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

        // Finish game if all terminal was occupated by either team.
        // if (redTerminalCount == allTerminalCount)
        // {
        //     if (BattleInfo.isMulti) BattleConductor.I.FinishGameClientRpc(Team.RED);
        //     else BattleConductor.I.FinishGame(Team.RED);
        // }
        // else if (blueTerminalCount == allTerminalCount)
        // {
        //     if (BattleInfo.isMulti) BattleConductor.I.FinishGameClientRpc(Team.BLUE);
        //     else BattleConductor.I.FinishGame(Team.BLUE);
        // }
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
