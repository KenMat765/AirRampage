using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    public abstract string Name { get; protected set; }
    public abstract int Weight { get; protected set; }
    public abstract string Explanation { get; protected set; }
    public abstract void Introducer(FighterCondition condition);
}
