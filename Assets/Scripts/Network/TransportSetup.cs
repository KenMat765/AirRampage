using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;

public class TransportSetup : MonoBehaviour
{
    [SerializeField] UNetTransport uNet;
    void Awake()
    {
        uNet.Initialize(NetworkManager.Singleton);
        Debug.Log("UNet Initialized");
    }

    void OnDestroy()
    {
        uNet.Shutdown();
        Debug.Log("UNet Destroyed");
    }
}
