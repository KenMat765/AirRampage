using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RuleManager : MonoBehaviour
{
    public abstract void Setup();

    public abstract void OnGameStart();
    public abstract void OnGameEnd();
}
