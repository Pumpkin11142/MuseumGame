using Mirror;
using UnityEngine;

/// <summary>
/// Room player used during matchmaking to expose events for UI.
/// </summary>
public class MatchmakingRoomPlayer : NetworkRoomPlayer
{
    private const string LogPrefix = "[MatchmakingRoomPlayer]";

    public static event System.Action<MatchmakingRoomPlayer> LocalRoomPlayerSpawned;
    public static event System.Action<bool> LocalReadyStateChanged;
    public static event System.Action<int> CountdownUpdated;
    public static event System.Action CountdownCleared;

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Debug.Log($"{LogPrefix} OnStartAuthority - netId={netId}, ready={readyToBegin}");
        LocalRoomPlayerSpawned?.Invoke(this);
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        base.ReadyStateChanged(oldReadyState, newReadyState);
        Debug.Log($"{LogPrefix} ReadyStateChanged - netId={netId}, oldReady={oldReadyState}, newReady={newReadyState}");
        if (hasAuthority)
        {
            LocalReadyStateChanged?.Invoke(newReadyState);
        }
    }

    public void ToggleReady()
    {
        if (!hasAuthority)
        {
            Debug.LogWarning($"{LogPrefix} ToggleReady ignored - no authority, netId={netId}");
            return;
        }

        Debug.Log($"{LogPrefix} ToggleReady - switching to {!readyToBegin} for netId={netId}");
            return;

        CmdChangeReadyState(!readyToBegin);
    }

    [TargetRpc]
    public void TargetRpcUpdateCountdown(NetworkConnection target, int secondsRemaining)
    {
        Debug.Log($"{LogPrefix} TargetRpcUpdateCountdown - netId={netId}, secondsRemaining={secondsRemaining}");
        CountdownUpdated?.Invoke(secondsRemaining);
    }

    [TargetRpc]
    public void TargetRpcCancelCountdown(NetworkConnection target)
    {
        Debug.Log($"{LogPrefix} TargetRpcCancelCountdown - netId={netId}");
        CountdownCleared?.Invoke();
    }
}
