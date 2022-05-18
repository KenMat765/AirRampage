using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NMDestroyer : MonoBehaviour
{
    void Start()
    {
        NetworkManager networkManager = GetComponent<NetworkManager>();
        if(NetworkManager.Singleton != networkManager)
        {
            Destroy(this.gameObject);
        }
    }
}
