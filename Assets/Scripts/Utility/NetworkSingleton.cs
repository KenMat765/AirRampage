using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
    static T instance;
    public static T I
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null) { instance = new GameObject(typeof(T).ToString()).AddComponent<T>(); }
            }
            return instance;
        }
    }

    protected abstract bool dont_destroy_on_load { get; set; }

    protected virtual void Awake()
    {
        if (this != I)
        {
            Destroy(this);
            return;
        }

        if (dont_destroy_on_load) DontDestroyOnLoad(this.gameObject);
    }
}
