using System.Collections;
using System.Linq;
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

    private Coroutine startMatchRoutine;

    /// <summary>
    /// Called whenever the queue changes. First value is ready players, second is total required.
    /// </summary>
    public static event System.Action<int, int> QueueStatusChanged;

    public override void OnRoomServerConnect(NetworkConnectionToClient conn)
    {
        base.OnRoomServerConnect(conn);
        EvaluateMatchReadiness();
    }

    public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnRoomServerDisconnect(conn);
        EvaluateMatchReadiness();
    }

    public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnRoomServerAddPlayer(conn);
        EvaluateMatchReadiness();
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        EvaluateMatchReadiness();
    }

    public override void ReadyStatusChanged()
    {
        base.ReadyStatusChanged();
        EvaluateMatchReadiness();
    }

    public override void OnStopServer()
    {
        StopCountdown();
        base.OnStopServer();
    }

    public override void OnRoomServerPlayersReady()
    {
        EvaluateMatchReadiness();
    }

    public override void OnRoomServerPlayersNotReady()
    {
        base.OnRoomServerPlayersNotReady();
        StopCountdown();
        EvaluateMatchReadiness();
    }

    IEnumerator BeginMatchAfterDelay()
    {
        float remaining = Mathf.Max(0f, matchStartDelaySeconds);
        while (remaining > 0f)
        {
            RpcUpdateCountdown(Mathf.CeilToInt(remaining));
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        if (AllRequirementsStillMet())
        {
            RpcUpdateCountdown(0);
            base.OnRoomServerPlayersReady();
        }

        startMatchRoutine = null;
    }

    void EvaluateMatchReadiness()
    {
        int readyCount = roomSlots.Count(player => player != null && player.readyToBegin);
        int totalConnected = roomSlots.Count(player => player != null);

        if (totalConnected == 0)
        {
            StopCountdown();
            QueueStatusChanged?.Invoke(0, playersPerMatch);
            return;
        }

        int requiredPlayers = requireAllPlayersReady ? totalConnected : playersPerMatch;
        requiredPlayers = Mathf.Clamp(requiredPlayers, 0, Mathf.Max(totalConnected, playersPerMatch));

        QueueStatusChanged?.Invoke(readyCount, requiredPlayers);

        bool enoughReady = readyCount >= playersPerMatch;
        bool everyoneReady = readyCount == totalConnected;

        if (!enoughReady || (requireAllPlayersReady && !everyoneReady))
        {
            StopCountdown();
            return;
        }

        if (startMatchRoutine == null)
        {
            startMatchRoutine = StartCoroutine(BeginMatchAfterDelay());
        }
    }

    bool AllRequirementsStillMet()
    {
        int readyCount = roomSlots.Count(player => player != null && player.readyToBegin);
        int totalConnected = roomSlots.Count(player => player != null);
        bool enoughReady = readyCount >= playersPerMatch;
        bool everyoneReady = readyCount == totalConnected;
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
            StopCoroutine(startMatchRoutine);
            startMatchRoutine = null;
            RpcCancelCountdown();
        }
    }

    [ClientRpc]
    void RpcUpdateCountdown(int secondsRemaining)
    {
        MatchmakingRoomPlayer.RaiseCountdown(secondsRemaining);
    }

    [ClientRpc]
    void RpcCancelCountdown()
    {
        MatchmakingRoomPlayer.CancelCountdown();
    }
}
