using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuleSwich : MonoBehaviour
{
    [SerializeField] BattleRoyal battleRoyal;
    [SerializeField] TerminalConquest terminalConquest;

    void Awake()
    {
        switch (BattleInfo.rule)
        {
            case Rule.BATTLEROYAL:
                battleRoyal.enabled = true;
                Destroy(terminalConquest);
                Destroy(this);
                break;

            case Rule.TERMINAL:
                terminalConquest.enabled = true;
                Destroy(battleRoyal);
                Destroy(this);
                break;
        }
    }
}
