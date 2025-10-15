using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Custom NetworkManager that coordinates quick matchmaking using LAN discovery.
/// </summary>
public class MatchmakingNetworkManager : NetworkManager
{
    [Header("Match Settings")]
    [Tooltip("Name of the gameplay scene that should load when everyone is connected.")]
    public string gameplaySceneName = "Round";

    [Tooltip("How many players are required before the gameplay scene loads.")]
    public int requiredPlayers = 2;

    [Header("Discovery")]
    public MatchmakingDiscovery discovery;
    [Tooltip("Seconds spent searching for a host before we start hosting ourselves.")]
    public float discoveryWindowSeconds = 3f;

    [Header("Events")]
    public UnityEvent onMatchSearchStarted;
    public UnityEvent onHostingStarted;
    public UnityEvent onClientConnecting;
    public UnityEvent onMatchmakingCancelled;
    public UnityEvent onMatchReady;

    Coroutine searchRoutine;
    bool matchReadyInvoked;
    UnityAction<DiscoveryResponse> activeSearchListener;

    public bool IsSearching => searchRoutine != null;

    public void BeginMatchmaking()
    {
        if (IsSearching || NetworkClient.isConnected || NetworkServer.active)
        {
            return;
        }

        if (discovery == null)
        {
            Debug.LogError("MatchmakingNetworkManager requires a MatchmakingDiscovery reference.");
            return;
        }

        matchReadyInvoked = false;
        searchRoutine = StartCoroutine(SearchForMatch());
    }

    public void CancelMatchmaking()
    {
        if (searchRoutine != null)
        {
            StopCoroutine(searchRoutine);
            searchRoutine = null;
        }

        if (activeSearchListener != null)
        {
            discovery.onServerFound.RemoveListener(activeSearchListener);
            activeSearchListener = null;
        }

        if (NetworkClient.isConnected && !NetworkServer.active)
        {
            StopClient();
        }
        else if (NetworkServer.active)
        {
            StopHost();
        }

        discovery.StopDiscovery();
        discovery.StopAdvertising();
        onMatchmakingCancelled?.Invoke();
        matchReadyInvoked = false;
    }

    IEnumerator SearchForMatch()
    {
        onMatchSearchStarted?.Invoke();

        bool foundServer = false;
        activeSearchListener = response =>
        {
            if (foundServer)
            {
                return;
            }

            foundServer = true;
            discovery.StopDiscovery();
            onClientConnecting?.Invoke();

            if (!StartClient(response.uri))
            {
                Debug.LogWarning("Failed to connect to discovered server, falling back to hosting.");
                foundServer = false;
                discovery.StartDiscovery();
            }
        };

        discovery.onServerFound.AddListener(activeSearchListener);
        discovery.StartDiscovery();

        float timer = 0f;
        while (!foundServer && timer < discoveryWindowSeconds)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (activeSearchListener != null)
        {
            discovery.onServerFound.RemoveListener(activeSearchListener);
            activeSearchListener = null;
        }

        if (!foundServer)
        {
            StartHost();
            discovery.AdvertiseServer();
            onHostingStarted?.Invoke();
        }

        searchRoutine = null;
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        CancelMatchmaking();
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        discovery.StopAdvertising();
        matchReadyInvoked = false;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        matchReadyInvoked = false;
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        CheckPlayerCount();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        CheckPlayerCount();
    }

    void CheckPlayerCount()
    {
        if (!NetworkServer.active)
        {
            return;
        }

        if (numPlayers >= requiredPlayers)
        {
            if (!matchReadyInvoked)
            {
                matchReadyInvoked = true;
                onMatchReady?.Invoke();
            }

            if (!string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                ServerChangeScene(gameplaySceneName);
            }
        }
        else
        {
            matchReadyInvoked = false;
        }
    }
}
