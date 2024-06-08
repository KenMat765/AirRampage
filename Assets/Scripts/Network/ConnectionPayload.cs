using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConnectionPayload
{
    public string playerName;
    public string skillCode;
    public string abilityCode;

    public ConnectionPayload(string playerName, string skillCode, string abilityCode)
    {
        this.playerName = playerName;
        this.skillCode = skillCode;
        this.abilityCode = abilityCode;
    }
}
