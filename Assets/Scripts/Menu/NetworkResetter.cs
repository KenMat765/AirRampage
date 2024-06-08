using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// This class just shutdowns NetworkManager when entered Menu scene.
public class NetworkResetter : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
