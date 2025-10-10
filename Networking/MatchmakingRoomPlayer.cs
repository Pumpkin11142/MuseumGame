using Mirror;
using UnityEngine;

/// <summary>
/// Room player used during matchmaking to expose events for UI.
/// </summary>
public class MatchmakingRoomPlayer : NetworkRoomPlayer
{
    public static event System.Action<MatchmakingRoomPlayer> LocalRoomPlayerSpawned;
    public static event System.Action<bool> LocalReadyStateChanged;
    public static event System.Action<int> CountdownUpdated;
    public static event System.Action CountdownCleared;

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        LocalRoomPlayerSpawned?.Invoke(this);
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        base.ReadyStateChanged(oldReadyState, newReadyState);
        if (hasAuthority)
        {
            LocalReadyStateChanged?.Invoke(newReadyState);
        }
    }

    public void ToggleReady()
    {
        if (!hasAuthority)
            return;

        CmdChangeReadyState(!readyToBegin);
    }

    [TargetRpc]
    public void TargetRpcUpdateCountdown(NetworkConnection target, int secondsRemaining)
    {
        CountdownUpdated?.Invoke(secondsRemaining);
    }

    [TargetRpc]
    public void TargetRpcCancelCountdown(NetworkConnection target)
    {
        CountdownCleared?.Invoke();
    }
}
