using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Cysharp.Threading.Tasks;

public class RelayAllocation : MonoBehaviour
{
    ///<Summary> Join Code necessary to join Relay Server. </Summary>
    public static string joinCode;



    ///<Summary> Sign in to UnityServices & AuthenticationService. </Summary>
    public static async UniTask<string> SignInPlayerAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log("Sign In Complete");

            return AuthenticationService.Instance.PlayerId;
        }
        catch (Exception e)
        {
            Debug.Log($"Failed to sign in. Exception : {e.Message}");
            return null;
        }
    }



    ///<Summary> Allocate Relay Server. </Summary>
    public static async UniTask<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] key)>
    AllocateRelayServer(int maxConnections, string region = null)
    {
        Allocation allocation;

        // Try to create Allocation on relay server.
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation failed {e.Message}");
            throw;
        }

        Debug.Log($"ConnectionData[0], [1]: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"AllocationId: {allocation.AllocationId}");

        // Try to get join code from relay server.
        try
        {
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");

        Debug.Log($"IPAddress : {dtlsEndpoint.Host}");
        Debug.Log($"Port : {(ushort)dtlsEndpoint.Port}");

        return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.Key);
    }



    ///<Summary> Join existing Relay Server Allocation by join code, </Summary>
    public static async UniTask<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] hostConnectionData, byte[] key)>
    JoinRelayServerAllocation(string joinCode)
    {
        JoinAllocation allocation;

        // Try to join allocation.
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }

        Debug.Log($"Host Connection Data[0], [1]: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"Client Connection Data[0], [1]: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"Client Allocation Id: {allocation.AllocationId}");

        var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.HostConnectionData, allocation.Key);
    }



    ///<Summary> Allocate Relay Server & Configure transport as host & set host relay data to Unity Transport driver & Start Host. </Summary>
    ///<param name="mono"> MonoBehaviour is necessary to call StartCoroutine() in static method. Simply put "this" as argument. </param>
    public static void AllocateRelayAndConfigureTransportAsHost(MonoBehaviour mono, int maxConnections)
    {
        mono.StartCoroutine(AllocateRelayAndConfigureTransportAsHostAsync(maxConnections));
    }
    static IEnumerator AllocateRelayAndConfigureTransportAsHostAsync(int maxConnections)
    {
        // Try to Allocate Relay Server.
        var serverRelayUtilityTask = AllocateRelayServer(maxConnections);
        while (serverRelayUtilityTask.Status == UniTaskStatus.Pending)
        {
            yield return null;
        }
        if (serverRelayUtilityTask.Status == UniTaskStatus.Faulted)
        {
            Debug.LogError("Exception thrown when attempting to allocate Relay Server. Server not allocated.");
            yield break;
        }

        var (ipv4address, port, allocationIdBytes, connectionData, key) = serverRelayUtilityTask.GetAwaiter().GetResult();

        // Configure Transport.
        // The .GetComponent method returns a UTP NetworkDriver (or a proxy to it)
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(ipv4address, port, allocationIdBytes, key, connectionData, true);
        NetworkManager.Singleton.StartHost();
    }



    ///<Summary> Configure transport as client by join code & set client relay data to Unity Transport driver & Start Client. </Summary>
    ///<param name="mono"> MonoBehaviour is necessary to call StartCoroutine() in static method. Simply put "this" as argument. </param>
    public static void ConfigureTransportAsClient(MonoBehaviour mono, string joinCode)
    {
        mono.StartCoroutine(ConfigureTransportAsClientAsync(joinCode));
    }
    static IEnumerator ConfigureTransportAsClientAsync(string joinCode)
    {
        // Try to Join Relay Server.
        var clientRelayUtilityTask = JoinRelayServerAllocation(joinCode);
        while (clientRelayUtilityTask.Status == UniTaskStatus.Pending)
        {
            yield return null;
        }
        if (clientRelayUtilityTask.Status == UniTaskStatus.Faulted)
        {
            // When join code is wrong, this part is called.
            Debug.LogError("Exception thrown when attempting to connect to Relay Server. Exception.");
            yield break;
        }

        var (ipv4address, port, allocationIdBytes, connectionData, hostConnectionData, key) = clientRelayUtilityTask.GetAwaiter().GetResult();

        // Configure Transport.
        // The .GetComponent method returns a UTP NetworkDriver (or a proxy to it)
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(ipv4address, port, allocationIdBytes, key, connectionData, hostConnectionData, true);
        NetworkManager.Singleton.StartClient();
    }
}
