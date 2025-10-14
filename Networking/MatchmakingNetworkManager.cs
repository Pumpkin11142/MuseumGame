using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Room-style network manager that pairs players together once enough ready players are queued.
/// </summary>
public class MatchmakingNetworkManager : NetworkRoomManager
{
    [Header("Matchmaking")]
    [SerializeField]
    [Tooltip("Number of ready players required before a match is started.")]
    private int playersPerMatch = 2;

    [SerializeField]
    [Tooltip("Delay between the last player readying and the match starting. Gives clients a chance to update UI.")]
    private float matchStartDelaySeconds = 3f;

    [SerializeField]
    [Tooltip("If true every connected player must be ready, otherwise only the required number of players is needed.")]
    private bool requireAllPlayersReady = false;

    private const string LogPrefix = "[MatchmakingManager]";

    private Coroutine startMatchRoutine;
    private int readinessCheckCounter;

    /// <summary>
    /// Called whenever the queue changes. First value is ready players, second is total required.
    /// </summary>
    public static event System.Action<int, int> QueueStatusChanged;

    public override void OnValidate()
    {
        base.OnValidate();

        ApplyPlayerCountConstraints();
    }

    void Awake()
    {
        ApplyPlayerCountConstraints();
        Debug.Log($"{LogPrefix} Awake - playersPerMatch={playersPerMatch}, minPlayers={minPlayers}, maxConnections={maxConnections}");
    }

    void ApplyPlayerCountConstraints()
    {
        int maxAllowed = maxConnections > 0 ? maxConnections : Mathf.Max(1, playersPerMatch);
        playersPerMatch = Mathf.Clamp(playersPerMatch < 1 ? 1 : playersPerMatch, 1, maxAllowed);
        minPlayers = playersPerMatch;
        Debug.Log($"{LogPrefix} ApplyPlayerCountConstraints - playersPerMatch={playersPerMatch}, minPlayers={minPlayers}, maxAllowed={maxAllowed}");
    }

    public override void OnRoomServerConnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"{LogPrefix} OnRoomServerConnect - connectionId={conn?.connectionId}");
        base.OnRoomServerConnect(conn);
        EvaluateMatchReadiness();
    }

    public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"{LogPrefix} OnRoomServerDisconnect - connectionId={conn?.connectionId}");
        base.OnRoomServerDisconnect(conn);
        EvaluateMatchReadiness();
    }

    public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"{LogPrefix} OnRoomServerAddPlayer - connectionId={conn?.connectionId}");
        base.OnRoomServerAddPlayer(conn);
        EvaluateMatchReadiness();
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        Debug.Log($"{LogPrefix} OnServerReady - connectionId={conn?.connectionId}");
        base.OnServerReady(conn);
        EvaluateMatchReadiness();
    }

    public override void ReadyStatusChanged()
    {
        Debug.Log($"{LogPrefix} ReadyStatusChanged - recalculating");
        base.ReadyStatusChanged();
        EvaluateMatchReadiness();
    }

    public override void OnStopServer()
    {
        Debug.Log($"{LogPrefix} OnStopServer - clearing countdown and state");
        StopCountdown();
        base.OnStopServer();
    }

    public override void OnRoomServerPlayersReady()
    {
        Debug.Log($"{LogPrefix} OnRoomServerPlayersReady - base countdown path");
        EvaluateMatchReadiness();
    }

    public override void OnRoomServerPlayersNotReady()
    {
        Debug.Log($"{LogPrefix} OnRoomServerPlayersNotReady - stopping countdown");
        base.OnRoomServerPlayersNotReady();
        StopCountdown();
        EvaluateMatchReadiness();
    }

    IEnumerator BeginMatchAfterDelay()
    {
        Debug.Log($"{LogPrefix} BeginMatchAfterDelay - starting countdown for {matchStartDelaySeconds} seconds");
        float remaining = Mathf.Max(0f, matchStartDelaySeconds);
        while (remaining > 0f)
        {
            BroadcastCountdown(Mathf.CeilToInt(remaining));
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        if (AllRequirementsStillMet())
        {
            Debug.Log($"{LogPrefix} BeginMatchAfterDelay - requirements still met, starting match");
            BroadcastCountdown(0);
            base.OnRoomServerPlayersReady();
        }
        else
        {
            Debug.LogWarning($"{LogPrefix} BeginMatchAfterDelay - requirements failed during countdown, aborting");
            BroadcastCountdownCancelled();
        }

        startMatchRoutine = null;
        Debug.Log($"{LogPrefix} BeginMatchAfterDelay - routine finished");
    }

    void EvaluateMatchReadiness()
    {
        int readyCount;
        int totalConnected;
        CountRoomPlayers(out readyCount, out totalConnected);

        if (totalConnected == 0)
        {
            Debug.Log($"{LogPrefix} EvaluateMatchReadiness - no connected players");
            StopCountdown();
            QueueStatusChanged?.Invoke(0, playersPerMatch);
            return;
        }

        int requiredPlayers = requireAllPlayersReady ? totalConnected : playersPerMatch;
        requiredPlayers = Mathf.Clamp(requiredPlayers, 0, Mathf.Max(totalConnected, playersPerMatch));

        QueueStatusChanged?.Invoke(readyCount, requiredPlayers);

        bool enoughReady = readyCount >= playersPerMatch;
        bool everyoneReady = readyCount == totalConnected;

        readinessCheckCounter++;
        Debug.Log($"{LogPrefix} EvaluateMatchReadiness #{readinessCheckCounter} - ready={readyCount}, total={totalConnected}, required={requiredPlayers}, playersPerMatch={playersPerMatch}, requireAll={requireAllPlayersReady}, enoughReady={enoughReady}, everyoneReady={everyoneReady}");

        if (!enoughReady || (requireAllPlayersReady && !everyoneReady))
        {
            Debug.Log($"{LogPrefix} EvaluateMatchReadiness - requirements not met, stopping countdown if running");
            StopCountdown();
            return;
        }

        if (startMatchRoutine == null)
        {
            Debug.Log($"{LogPrefix} EvaluateMatchReadiness - starting countdown coroutine");
            startMatchRoutine = StartCoroutine(BeginMatchAfterDelay());
        }
        else
        {
            Debug.Log($"{LogPrefix} EvaluateMatchReadiness - countdown already running");
        }
    }

    bool AllRequirementsStillMet()
    {
        int readyCount;
        int totalConnected;
        CountRoomPlayers(out readyCount, out totalConnected);
        bool enoughReady = readyCount >= playersPerMatch;
        bool everyoneReady = readyCount == totalConnected;
        Debug.Log($"{LogPrefix} AllRequirementsStillMet - ready={readyCount}, total={totalConnected}, enoughReady={enoughReady}, everyoneReady={everyoneReady}, requireAll={requireAllPlayersReady}");
        if (!enoughReady)
            return false;
        if (requireAllPlayersReady && !everyoneReady)
            return false;
        return true;
    }

    void StopCountdown()
    {
        if (startMatchRoutine != null)
        {
            Debug.Log($"{LogPrefix} StopCountdown - cancelling active countdown");
            StopCoroutine(startMatchRoutine);
            startMatchRoutine = null;
            BroadcastCountdownCancelled();
        }
        else
        {
            Debug.Log($"{LogPrefix} StopCountdown - no active countdown to cancel");
        }
    }

    void BroadcastCountdown(int secondsRemaining)
    {
        Debug.Log($"{LogPrefix} BroadcastCountdown - secondsRemaining={secondsRemaining}");
        foreach (MatchmakingRoomPlayer player in ActiveMatchmakingPlayers())
        {
            if (player.connectionToClient != null)
            {
                Debug.Log($"{LogPrefix} BroadcastCountdown - sending to connectionId={player.connectionToClient.connectionId}");
                player.TargetRpcUpdateCountdown(player.connectionToClient, secondsRemaining);
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} BroadcastCountdown - player without connection skipped");
            }
        }
    }

    void BroadcastCountdownCancelled()
    {
        Debug.Log($"{LogPrefix} BroadcastCountdownCancelled - notifying clients");
        foreach (MatchmakingRoomPlayer player in ActiveMatchmakingPlayers())
        {
            if (player.connectionToClient != null)
            {
                Debug.Log($"{LogPrefix} BroadcastCountdownCancelled - sending to connectionId={player.connectionToClient.connectionId}");
                player.TargetRpcCancelCountdown(player.connectionToClient);
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} BroadcastCountdownCancelled - player without connection skipped");
            }
                player.TargetRpcCancelCountdown(player.connectionToClient);
            }
        }
    }

    void CountRoomPlayers(out int readyCount, out int totalConnected)
    {
        readyCount = 0;
        totalConnected = 0;

        if (!NetworkServer.active)
        {
            Debug.LogWarning($"{LogPrefix} CountRoomPlayers - NetworkServer not active");
            return;
        }
            return;

        foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
        {
            if (connection == null || connection.identity == null)
            {
                Debug.LogWarning($"{LogPrefix} CountRoomPlayers - skipping null connection or identity");
                continue;
            }

            if (!connection.identity.TryGetComponent(out NetworkRoomPlayer player) || player == null)
            {
                Debug.LogWarning($"{LogPrefix} CountRoomPlayers - connection {connection.connectionId} missing NetworkRoomPlayer");
                continue;
            }
                continue;

            if (!connection.identity.TryGetComponent(out NetworkRoomPlayer player) || player == null)
                continue;

            totalConnected++;

            if (player.readyToBegin)
            {
                Debug.Log($"{LogPrefix} CountRoomPlayers - connectionId={connection.connectionId} READY");
                readyCount++;
            }
            else
            {
                Debug.Log($"{LogPrefix} CountRoomPlayers - connectionId={connection.connectionId} not ready");
            }
        }

        Debug.Log($"{LogPrefix} CountRoomPlayers - totals ready={readyCount}, totalConnected={totalConnected}");
                readyCount++;
        }
    }

    System.Collections.Generic.IEnumerable<MatchmakingRoomPlayer> ActiveMatchmakingPlayers()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning($"{LogPrefix} ActiveMatchmakingPlayers - NetworkServer not active");
            yield break;
        }
            yield break;

        foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
        {
            if (connection == null || connection.identity == null)
            {
                Debug.LogWarning($"{LogPrefix} ActiveMatchmakingPlayers - skipping null connection or identity");
                continue;
            }

            if (!connection.identity.TryGetComponent(out MatchmakingRoomPlayer player) || player == null)
            {
                Debug.LogWarning($"{LogPrefix} ActiveMatchmakingPlayers - connection {connection.connectionId} missing MatchmakingRoomPlayer");
                continue;
            }
                continue;

            if (!connection.identity.TryGetComponent(out MatchmakingRoomPlayer player) || player == null)
                continue;

            yield return player;
        }
    }
}
