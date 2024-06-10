using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    [SerializeField] protected string abilityName;
    public string Name
    {
        get { return abilityName; }
        set { abilityName = value; }
    }

    [SerializeField] protected int abilityWeight;
    public int Weight
    {
        get { return abilityWeight; }
        set { abilityWeight = value; }
    }

    [SerializeField] protected string abilityExplanation;
    public string Explanation
    {
        get { return abilityExplanation; }
        set { abilityExplanation = value; }
    }

    public abstract void Introducer(FighterCondition condition);
}
