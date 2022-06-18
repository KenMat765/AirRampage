using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void FPSSetup()
    {
        Application.targetFrameRate = 60;
    }
}
