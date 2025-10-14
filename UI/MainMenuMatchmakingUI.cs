using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the ready up button on the main menu and displays matchmaking status updates.
/// </summary>
public class MainMenuMatchmakingUI : MonoBehaviour
{
    private const string LogPrefix = "[MatchmakingUI]";

    [Header("UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Text readyButtonLabel;
    [SerializeField] private Text statusLabel;
    [SerializeField] private Text countdownLabel;
    [Header("UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text readyButtonLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text countdownLabel;

    [Header("Copy")]
    [SerializeField] private string readyText = "Ready";
    [SerializeField] private string cancelReadyText = "Cancel";
    [SerializeField] private string idleStatusText = "Press Ready to search for a match.";
    [SerializeField] private string searchingStatusText = "Searching for players...";
    [SerializeField] private string waitingStatusFormat = "Waiting for players ({0}/{1})";
    [SerializeField] private string waitingForCountdownText = "Match starting soon";

    [Header("Behaviour")]
    [Tooltip("If true the client will automatically host a match when no server is running.")]
    [SerializeField] private bool autoHostWhenAlone = true;
    [SerializeField] private float connectionTimeoutSeconds = 3f;

    [SerializeField] private MatchmakingNetworkManager matchmakingManager;
    private MatchmakingRoomPlayer localRoomPlayer;
    private bool requestedMatchmaking;
    private Coroutine connectionTimeoutRoutine;
    private bool autoReadyWhenRoomPlayerAvailable;

    void Awake()
    {
        TryResolveMatchmakingManager();
        Debug.Log($"{LogPrefix} Awake - autoHostWhenAlone={autoHostWhenAlone}, connectionTimeoutSeconds={connectionTimeoutSeconds}");
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        MatchmakingRoomPlayer.LocalRoomPlayerSpawned += HandleLocalRoomPlayerSpawned;
        MatchmakingRoomPlayer.LocalReadyStateChanged += HandleLocalReadyStateChanged;
        MatchmakingRoomPlayer.CountdownUpdated += HandleCountdownUpdated;
        MatchmakingRoomPlayer.CountdownCleared += HandleCountdownCleared;
        MatchmakingNetworkManager.QueueStatusChanged += HandleQueueStatusChanged;

        ResetUI();
    }

    void OnDestroy()
    {
        Debug.Log($"{LogPrefix} OnDestroy - cleaning up listeners");
        StopConnectionTimeout();
        autoReadyWhenRoomPlayerAvailable = false;
        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnReadyClicked);
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(OnCancelClicked);

        MatchmakingRoomPlayer.LocalRoomPlayerSpawned -= HandleLocalRoomPlayerSpawned;
        MatchmakingRoomPlayer.LocalReadyStateChanged -= HandleLocalReadyStateChanged;
        MatchmakingRoomPlayer.CountdownUpdated -= HandleCountdownUpdated;
        MatchmakingRoomPlayer.CountdownCleared -= HandleCountdownCleared;
        MatchmakingNetworkManager.QueueStatusChanged -= HandleQueueStatusChanged;
    }

    void OnReadyClicked()
    {
        Debug.Log($"{LogPrefix} OnReadyClicked - hasLocalRoomPlayer={localRoomPlayer != null}, serverActive={NetworkServer.active}, clientActive={NetworkClient.active}, requestedMatchmaking={requestedMatchmaking}");
        if (localRoomPlayer != null)
        {
            Debug.Log($"{LogPrefix} OnReadyClicked - toggling ready on local room player");
        if (localRoomPlayer != null)
        {
            localRoomPlayer.ToggleReady();
            return;
        }

        if (NetworkServer.active)
        {
            Debug.Log($"{LogPrefix} OnReadyClicked - server already active, waiting for room player");
            return; // hosting already, wait for the local room player
        }

        if (NetworkClient.active)
        {
            Debug.Log($"{LogPrefix} OnReadyClicked - client already active, waiting for room player");
            return; // waiting for room player to be spawned
        }
            return; // hosting already, wait for the local room player

        if (NetworkClient.active)
            return; // waiting for room player to be spawned

        if (!TryResolveMatchmakingManager())
        {
            Debug.LogError("MainMenuMatchmakingUI could not locate a MatchmakingNetworkManager.");
            return;
        }

        Debug.Log($"{LogPrefix} OnReadyClicked - starting matchmaking client");
        if (statusLabel != null)
            statusLabel.text = searchingStatusText;
        SetReadyButtonInteractable(false);
        requestedMatchmaking = true;
        autoReadyWhenRoomPlayerAvailable = true;
        matchmakingManager.StartClient();
        BeginConnectionTimeout();
    }

    void OnCancelClicked()
    {
        Debug.Log($"{LogPrefix} OnCancelClicked - hasLocalRoomPlayer={localRoomPlayer != null}, clientActive={NetworkClient.active}, serverActive={NetworkServer.active}");
        requestedMatchmaking = false;
        autoReadyWhenRoomPlayerAvailable = false;
        StopConnectionTimeout();

        if (localRoomPlayer != null && localRoomPlayer.readyToBegin)
        {
            Debug.Log($"{LogPrefix} OnCancelClicked - toggling ready off on local room player");
            localRoomPlayer.ToggleReady();
            return;
        }

        if (NetworkClient.isConnected || NetworkClient.active)
        {
            Debug.Log($"{LogPrefix} OnCancelClicked - stopping active client session");
            StopActiveClient();
        }

        ResetUI();
    }

    void HostMatch()
    {
        if (!TryResolveMatchmakingManager())
            return;

        if (NetworkServer.active)
            return;

        Debug.Log($"{LogPrefix} HostMatch - starting new host instance");
        Debug.Log("[MatchmakingUI] Hosting a new match - no existing host was found.");
        matchmakingManager.StartHost();
        if (statusLabel != null)
            statusLabel.text = "Hosting match...";
        SetReadyButtonInteractable(true);
    }

    void HandleLocalRoomPlayerSpawned(MatchmakingRoomPlayer roomPlayer)
    {
        Debug.Log($"{LogPrefix} HandleLocalRoomPlayerSpawned - netId={roomPlayer?.netId}, autoReady={autoReadyWhenRoomPlayerAvailable}");
        localRoomPlayer = roomPlayer;
        StopConnectionTimeout();
        SetReadyButtonInteractable(true);
        UpdateReadyButton(roomPlayer.readyToBegin);
        if (statusLabel != null)
            statusLabel.text = roomPlayer.readyToBegin ? waitingForCountdownText : idleStatusText;

        if (autoReadyWhenRoomPlayerAvailable)
        {
            autoReadyWhenRoomPlayerAvailable = false;
            if (!roomPlayer.readyToBegin)
                roomPlayer.ToggleReady();
        }

        requestedMatchmaking = false;
    }

    void HandleLocalReadyStateChanged(bool ready)
    {
        Debug.Log($"{LogPrefix} HandleLocalReadyStateChanged - ready={ready}");
        UpdateReadyButton(ready);
        if (statusLabel != null)
            statusLabel.text = ready ? waitingForCountdownText : idleStatusText;
    }

    void HandleCountdownUpdated(int seconds)
    {
        Debug.Log($"{LogPrefix} HandleCountdownUpdated - seconds={seconds}");
        if (countdownLabel == null)
            return;

        countdownLabel.gameObject.SetActive(seconds > 0);
        countdownLabel.text = seconds > 0 ? $"Match starting in {seconds}" : string.Empty;
    }

    void HandleCountdownCleared()
    {
        Debug.Log($"{LogPrefix} HandleCountdownCleared");
        if (countdownLabel == null)
            return;

        countdownLabel.gameObject.SetActive(false);
    }

    void HandleQueueStatusChanged(int readyPlayers, int required)
    {
        Debug.Log($"{LogPrefix} HandleQueueStatusChanged - ready={readyPlayers}, required={required}, clientActive={NetworkClient.active}");
        if (statusLabel == null)
            return;

        if (!NetworkClient.active)
            return;

        statusLabel.text = string.Format(waitingStatusFormat, readyPlayers, required);
    }

    void ResetUI()
    {
        Debug.Log($"{LogPrefix} ResetUI");
        localRoomPlayer = null;
        if (statusLabel != null)
            statusLabel.text = idleStatusText;
        StopConnectionTimeout();
        autoReadyWhenRoomPlayerAvailable = false;
        if (countdownLabel != null)
        {
            countdownLabel.gameObject.SetActive(false);
            countdownLabel.text = string.Empty;
        }
        UpdateReadyButton(false);
        SetReadyButtonInteractable(true);
    }

    void UpdateReadyButton(bool ready)
    {
        Debug.Log($"{LogPrefix} UpdateReadyButton - ready={ready}");
        if (readyButtonLabel != null)
            readyButtonLabel.text = ready ? cancelReadyText : readyText;
    }

    void SetReadyButtonInteractable(bool interactable)
    {
        Debug.Log($"{LogPrefix} SetReadyButtonInteractable - interactable={interactable}");
        if (readyButton != null)
            readyButton.interactable = interactable;
    }

    bool TryResolveMatchmakingManager()
    {
        if (matchmakingManager != null)
        {
            Debug.Log($"{LogPrefix} TryResolveMatchmakingManager - already have reference");
            return true;
        }

        matchmakingManager = NetworkManager.singleton as MatchmakingNetworkManager;
        if (matchmakingManager != null)
        {
            Debug.Log($"{LogPrefix} TryResolveMatchmakingManager - resolved via NetworkManager.singleton");
            return true;
        }
            return true;

        matchmakingManager = NetworkManager.singleton as MatchmakingNetworkManager;
        if (matchmakingManager != null)
            return true;

#if UNITY_2023_1_OR_NEWER
        matchmakingManager = UnityEngine.Object.FindFirstObjectByType<MatchmakingNetworkManager>();
#else
        matchmakingManager = UnityEngine.Object.FindObjectOfType<MatchmakingNetworkManager>();
#endif

        Debug.Log($"{LogPrefix} TryResolveMatchmakingManager - resolved via scene search={matchmakingManager != null}");
        return matchmakingManager != null;
    }

    void BeginConnectionTimeout()
    {
        if (!autoHostWhenAlone)
        {
            Debug.Log($"{LogPrefix} BeginConnectionTimeout - autoHost disabled");
            return;
        }
            return;

        StopConnectionTimeout();

        if (connectionTimeoutSeconds <= 0f)
        {
            Debug.Log($"{LogPrefix} BeginConnectionTimeout - timeout disabled");
            return;
        }

        Debug.Log($"{LogPrefix} BeginConnectionTimeout - starting timer for {connectionTimeoutSeconds} seconds");
            return;

        connectionTimeoutRoutine = StartCoroutine(WaitForConnectionTimeout());
    }

    void StopConnectionTimeout()
    {
        if (connectionTimeoutRoutine != null)
        {
            Debug.Log($"{LogPrefix} StopConnectionTimeout - stopping active routine");
            StopCoroutine(connectionTimeoutRoutine);
            connectionTimeoutRoutine = null;
        }
        else if (requestedMatchmaking)
        {
            Debug.Log($"{LogPrefix} StopConnectionTimeout - no routine to stop");
        }
            StopCoroutine(connectionTimeoutRoutine);
            connectionTimeoutRoutine = null;
        }
    }

    IEnumerator WaitForConnectionTimeout()
    {
        float elapsed = 0f;
        while (requestedMatchmaking && NetworkClient.active && !NetworkClient.isConnected && elapsed < connectionTimeoutSeconds)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        connectionTimeoutRoutine = null;

        if (!requestedMatchmaking || NetworkClient.isConnected)
            yield break;

        if (!TryResolveMatchmakingManager())
        {
            Debug.LogError("MainMenuMatchmakingUI could not locate a MatchmakingNetworkManager for fallback hosting.");
            yield break;
        }

        Debug.Log($"{LogPrefix} WaitForConnectionTimeout - no host found after {elapsed:F2} seconds, starting host");
        Debug.Log("[MatchmakingUI] No host found in time. Starting a new host instance.");
        requestedMatchmaking = false;
        StopActiveClient();
        HostMatch();
    }

    void StopActiveClient()
    {
        if (matchmakingManager == null)
            TryResolveMatchmakingManager();

        if (matchmakingManager != null)
        {
            Debug.Log($"{LogPrefix} StopActiveClient - using matchmaking manager, serverActive={NetworkServer.active}");
            if (NetworkServer.active)
                matchmakingManager.StopHost();
            else
                matchmakingManager.StopClient();
            return;
        }

        NetworkManager fallbackManager = NetworkManager.singleton;
        if (fallbackManager != null)
        {
            Debug.Log($"{LogPrefix} StopActiveClient - using fallback NetworkManager, serverActive={NetworkServer.active}");
            if (NetworkServer.active)
                fallbackManager.StopHost();
            else
                fallbackManager.StopClient();
        }
        else
        {
            Debug.LogWarning($"{LogPrefix} StopActiveClient - no NetworkManager available to stop");
        }
    }
}
