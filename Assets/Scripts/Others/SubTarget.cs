using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubTarget : MonoBehaviour
{
    public static Vector3[] subTargetPositions { get; private set; }
    public static int subTargetCount { get; private set; }
    public static LayerMask mask { get; private set; } = (1 << 7);

    void Awake()
    {
        GameObject[] all_subtargets = gameObject.GetAllChildren();
        subTargetCount = all_subtargets.Length;
        subTargetPositions = new Vector3[subTargetCount];
        for (int k = 0; k < subTargetCount; k++) subTargetPositions[k] = all_subtargets[k].transform.position;
    }

    public static Vector3 GetRandomPosition()
    {
        Vector3 random_position = subTargetPositions[Random.Range(0, subTargetCount)];
        return random_position;
    }
}
