using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConnectionPayload
{
    // public string password;
    public string playerName;
    public string skillCode;

    // public ConnectionPayload(string password, string playerName, string skillCode)
    // {
    //     this.password = password;
    //     this.playerName = playerName;
    //     this.skillCode = skillCode;
    // }

    public ConnectionPayload(string playerName, string skillCode)
    {
        this.playerName = playerName;
        this.skillCode = skillCode;
    }
}
