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

    public override void ReadyToBeginCheck()
    {
        if (roomPlayers.Count == 0)
        {
            StopCountdown();
            QueueStatusChanged?.Invoke(0, playersPerMatch);
            return;
        }

        int readyCount = roomPlayers.Count(player => player.readyToBegin);
        int totalConnected = roomPlayers.Count;

        QueueStatusChanged?.Invoke(readyCount, Mathf.Max(playersPerMatch, totalConnected));

        bool enoughReady = readyCount >= playersPerMatch;
        bool everyoneReady = readyCount == totalConnected;

        if (!enoughReady)
        {
            StopCountdown();
            return;
        }

        if (requireAllPlayersReady && !everyoneReady)
        {
            StopCountdown();
            return;
        }

        if (startMatchRoutine == null)
        {
            startMatchRoutine = StartCoroutine(BeginMatchAfterDelay());
        }
    }

    public override void OnRoomServerPlayersNotReady()
    {
        base.OnRoomServerPlayersNotReady();
        StopCountdown();
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

    bool AllRequirementsStillMet()
    {
        int readyCount = roomPlayers.Count(player => player.readyToBegin);
        int totalConnected = roomPlayers.Count;
        if (readyCount < playersPerMatch)
            return false;
        if (requireAllPlayersReady && readyCount < totalConnected)
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
