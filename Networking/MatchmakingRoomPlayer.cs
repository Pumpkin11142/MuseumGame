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

    public static void RaiseCountdown(int seconds)
    {
        CountdownUpdated?.Invoke(seconds);
    }

    public static void CancelCountdown()
    {
        CountdownCleared?.Invoke();
    }
}
