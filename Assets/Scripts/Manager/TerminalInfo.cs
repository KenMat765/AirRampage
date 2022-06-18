using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.NetworkInformation;

public class TerminalInfo : MonoBehaviour
{
    public static string ipAddress;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    void GetInfo()
    {
        NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface ni in nis)
        {
            if (ni.Name == "en0")
            {
                IPInterfaceProperties ipip = ni.GetIPProperties();
                UnicastIPAddressInformationCollection uipaic = ipip.UnicastAddresses;
                foreach (var uipai in uipaic)
                {
                    string address = uipai.Address.ToString();
                    if (address.Length < 16)
                    {
                        ipAddress = address;
                        break;
                    }
                }
            }
        }
    }
}
