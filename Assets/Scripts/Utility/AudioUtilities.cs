using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioUtilities
{
    /// <summary>音量の倍率をdB値に変換する</summary>
    public static float Magnif2DB(float magnif) => 20 * Mathf.Log10(magnif);
}
