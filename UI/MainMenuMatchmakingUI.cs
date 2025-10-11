using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the ready up button on the main menu and displays matchmaking status updates.
/// </summary>
public class MainMenuMatchmakingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Text readyButtonLabel;
    [SerializeField] private Text statusLabel;
    [SerializeField] private Text countdownLabel;

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

    void Awake()
    {
        TryResolveMatchmakingManager();
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
        StopConnectionTimeout();
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

    void Update()
    {
        if (requestedMatchmaking && autoHostWhenAlone && !NetworkClient.isConnected && !NetworkClient.active)
        {
            // We tried to connect but could not - become the host instead.
            requestedMatchmaking = false;
            HostMatch();
        }
    }

    void OnReadyClicked()
    {
        if (localRoomPlayer != null)
        {
            localRoomPlayer.ToggleReady();
            return;
        }

        if (NetworkClient.active)
            return; // waiting for room player to be spawned

        if (!TryResolveMatchmakingManager())
        {
            Debug.LogError("MainMenuMatchmakingUI could not locate a MatchmakingNetworkManager.");
            return;
        }

        statusLabel.text = searchingStatusText;
        SetReadyButtonInteractable(false);
        requestedMatchmaking = true;
        matchmakingManager.StartClient();
        BeginConnectionTimeout();
    }

    void OnCancelClicked()
    {
        requestedMatchmaking = false;
        StopConnectionTimeout();

        if (localRoomPlayer != null && localRoomPlayer.readyToBegin)
        {
            localRoomPlayer.ToggleReady();
            return;
        }

        if (NetworkClient.isConnected || NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }

        ResetUI();
    }

    void HostMatch()
    {
        if (!TryResolveMatchmakingManager())
            return;

        Debug.Log("[MatchmakingUI] Hosting a new match - no existing host was found.");
        matchmakingManager.StartHost();
        statusLabel.text = "Hosting match...";
        SetReadyButtonInteractable(true);
    }

    void HandleLocalRoomPlayerSpawned(MatchmakingRoomPlayer roomPlayer)
    {
        localRoomPlayer = roomPlayer;
        StopConnectionTimeout();
        SetReadyButtonInteractable(true);
        UpdateReadyButton(roomPlayer.readyToBegin);
        statusLabel.text = idleStatusText;
        requestedMatchmaking = false;
    }

    void HandleLocalReadyStateChanged(bool ready)
    {
        UpdateReadyButton(ready);
        statusLabel.text = ready ? waitingForCountdownText : idleStatusText;
    }

    void HandleCountdownUpdated(int seconds)
    {
        if (countdownLabel == null)
            return;

        countdownLabel.gameObject.SetActive(seconds > 0);
        countdownLabel.text = seconds > 0 ? $"Match starting in {seconds}" : string.Empty;
    }

    void HandleCountdownCleared()
    {
        if (countdownLabel == null)
            return;

        countdownLabel.gameObject.SetActive(false);
    }

    void HandleQueueStatusChanged(int readyPlayers, int required)
    {
        if (statusLabel == null)
            return;

        if (!NetworkClient.active)
            return;

        statusLabel.text = string.Format(waitingStatusFormat, readyPlayers, required);
    }

    void ResetUI()
    {
        localRoomPlayer = null;
        statusLabel.text = idleStatusText;
        StopConnectionTimeout();
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
        if (readyButtonLabel != null)
            readyButtonLabel.text = ready ? cancelReadyText : readyText;
    }

    void SetReadyButtonInteractable(bool interactable)
    {
        if (readyButton != null)
            readyButton.interactable = interactable;
    }

    bool TryResolveMatchmakingManager()
    {
        if (matchmakingManager != null)
            return true;

        matchmakingManager = NetworkManager.singleton as MatchmakingNetworkManager;
        if (matchmakingManager != null)
            return true;

#if UNITY_2023_1_OR_NEWER
        matchmakingManager = UnityEngine.Object.FindFirstObjectByType<MatchmakingNetworkManager>();
#else
        matchmakingManager = UnityEngine.Object.FindObjectOfType<MatchmakingNetworkManager>();
#endif

        return matchmakingManager != null;
    }

    void BeginConnectionTimeout()
    {
        if (!autoHostWhenAlone)
            return;

        StopConnectionTimeout();

        if (connectionTimeoutSeconds <= 0f)
            return;

        connectionTimeoutRoutine = StartCoroutine(WaitForConnectionTimeout());
    }

    void StopConnectionTimeout()
    {
        if (connectionTimeoutRoutine != null)
        {
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

        if (matchmakingManager == null && !TryResolveMatchmakingManager())
        {
            Debug.LogError("MainMenuMatchmakingUI could not locate a MatchmakingNetworkManager for fallback hosting.");
            yield break;
        }

        Debug.Log("[MatchmakingUI] No host found in time. Starting a new host instance.");
        requestedMatchmaking = false;
        NetworkManager.singleton.StopClient();
        HostMatch();
    }
}
