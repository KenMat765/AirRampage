using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoyalManager : RuleManager
{
    public override void Setup()
    {
        foreach (var info in ParticipantManager.I.fighterInfos)
        {
            FighterCondition condition = info.fighterCondition;
            condition.OnDeathCallback += OnFighterDeath;
        }
    }

    public override void OnGameStart()
    {

    }

    public override void OnGameEnd()
    {

    }



    public const int SCORE_FIGHTER = 500;   // Score obtained when killed other player. (Leave it constant for now)
    public const int SCORE_ZAKO = 50;       // Score obtained when killed zako. (Leave it constant for now)

    void OnFighterDeath(int killed_no, int killer_no, Team killed_team, string cause_of_death)
    {
        bool im_zako = killed_no >= GameInfo.MAX_PLAYER_COUNT;
        int my_score = im_zako ? SCORE_ZAKO : SCORE_FIGHTER;
        ScoreManager.I.AddScoreOpponent(my_score, killed_team);

        // If specific cause of death.
        if (killer_no < 0)
        {
            // Do nothing.
        }

        // If killer is Fighter.
        else if (0 <= killer_no && killer_no < GameInfo.MAX_PLAYER_COUNT)
        {
            ScoreManager.I.individualScores[killer_no] += my_score;
        }

        // If killer is Zako.
        else
        {
            FighterCondition zako_condition = ParticipantManager.I.fighterInfos[killer_no].fighterCondition;
            Team destroyer_team = zako_condition.fighterTeam.Value;
            switch (destroyer_team)
            {
                case Team.RED:
                    ScoreManager.I.individualScores[GameInfo.MAX_PLAYER_COUNT] += my_score;
                    break;

                case Team.BLUE:
                    ScoreManager.I.individualScores[GameInfo.MAX_PLAYER_COUNT + 1] += my_score;
                    break;

                default:
                    Debug.LogError("Killer's team is NONE!!", zako_condition.gameObject);
                    return;
            }
        }

        // case Rule.TERMINAL_CONQUEST:
        //     float protection_decrease = 0.25f;
        //     List<Terminal> owner_terminals;
        //     if (TerminalManager.I.TryGetOwnerTerminals(my_no, out owner_terminals))
        //     {
        //         foreach (Terminal terminal in owner_terminals)
        //         {
        //             terminal.SkillProtection -= protection_decrease;
        //         }
        //     }
        //     break;

        // case Rule.CRYSTAL_HUNTER:
        //     for (int crystal_id = 0; crystal_id < CrystalManager.crystal_count; crystal_id++)
        //     {
        //         int carrier_no = CrystalManager.I.carrierNos[crystal_id];
        //         if (carrier_no == my_no)
        //         {
        //             Crystal crystal = CrystalManager.I.crystals[crystal_id];
        //             crystal.ReleaseCrystal();
        //         }
        //     }
        //     break;
    }
}
