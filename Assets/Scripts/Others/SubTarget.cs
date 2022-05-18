using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubTarget : MonoBehaviour
{
    public static GameObject[] sub_targets {get; private set;}

    void Awake()
    {
        sub_targets = gameObject.GetAllChildren();
    }
}
