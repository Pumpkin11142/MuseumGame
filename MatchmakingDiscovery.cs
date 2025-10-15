using System;
using System.Net;
using System.Security.Cryptography;
using Mirror;
using Mirror.Discovery;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DiscoveryRequest : NetworkMessage
{
}

[Serializable]
public class DiscoveryResponse : NetworkMessage
{
    public long serverId;
    public Uri uri;
    public string serverName;
}

[Serializable]
public class DiscoveryResponseEvent : UnityEvent<DiscoveryResponse>
{
}

/// <summary>
/// Helper that uses Mirror's LAN discovery to locate open hosts and advertise our own.
/// </summary>
public class MatchmakingDiscovery : NetworkDiscoveryBase<DiscoveryRequest, DiscoveryResponse>
{
    [SerializeField]
    string advertisedServerName = "Museum Match";

    public long ServerId { get; private set; }

    public DiscoveryResponseEvent onServerFound = new DiscoveryResponseEvent();

    void Awake()
    {
        ServerId = GenerateServerId();
    }

    protected override DiscoveryRequest GetRequest()
    {
        return new DiscoveryRequest();
    }

    protected override DiscoveryResponse ProcessRequest(DiscoveryRequest request, IPEndPoint endpoint)
    {
        UriBuilder builder = new UriBuilder(Transport.activeTransport.ServerUri())
        {
            Host = endpoint.Address.ToString()
        };

        return new DiscoveryResponse
        {
            serverId = ServerId,
            uri = builder.Uri,
            serverName = advertisedServerName
        };
    }

    protected override void ProcessResponse(DiscoveryResponse response, IPEndPoint endpoint)
    {
        response.uri = new UriBuilder(response.uri)
        {
            Host = endpoint.Address.ToString()
        }.Uri;

        onServerFound.Invoke(response);
    }

    public void AdvertiseServer()
    {
        base.AdvertiseServer();
    }

    public void StopAdvertising()
    {
        base.StopDiscovery();
    }

    static long GenerateServerId()
    {
        byte[] buffer = new byte[8];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }

        long value = BitConverter.ToInt64(buffer, 0);
        return value == 0 ? 1 : value;
    }
}
