using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IFighter
{
    FighterCondition fighterCondition {get; set;}
    void OnDeath();
    void OnRevival();
}
