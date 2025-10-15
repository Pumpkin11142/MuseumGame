using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main menu ready button and status labels.
/// </summary>
public class MainMenuMatchmakingUI : MonoBehaviour
{
    [Header("References")]
    public MatchmakingNetworkManager matchmakingManager;
    public TMP_Text statusLabel;
    public Button readyButton;
    public Button cancelButton;

    [Header("Texts")]
    [TextArea]
    public string idleMessage = "Press Ready to find a match.";
    [TextArea]
    public string searchingMessage = "Looking for another player...";
    [TextArea]
    public string connectingMessage = "Joining a match...";
    [TextArea]
    public string hostingMessage = "Hosting a lobby...";
    [TextArea]
    public string matchReadyMessage = "Match found! Loading...";

    void Awake()
    {
        if (matchmakingManager != null)
        {
            matchmakingManager.onMatchSearchStarted.AddListener(HandleSearchStarted);
            matchmakingManager.onClientConnecting.AddListener(HandleClientConnecting);
            matchmakingManager.onHostingStarted.AddListener(HandleHostingStarted);
            matchmakingManager.onMatchmakingCancelled.AddListener(HandleCancelled);
            matchmakingManager.onMatchReady.AddListener(HandleMatchReady);
        }

        UpdateIdleState();
    }

    void OnDestroy()
    {
        if (matchmakingManager != null)
        {
            matchmakingManager.onMatchSearchStarted.RemoveListener(HandleSearchStarted);
            matchmakingManager.onClientConnecting.RemoveListener(HandleClientConnecting);
            matchmakingManager.onHostingStarted.RemoveListener(HandleHostingStarted);
            matchmakingManager.onMatchmakingCancelled.RemoveListener(HandleCancelled);
            matchmakingManager.onMatchReady.RemoveListener(HandleMatchReady);
        }
    }

    public void OnReadyPressed()
    {
        if (matchmakingManager == null)
        {
            Debug.LogWarning("MatchmakingManager reference missing.");
            return;
        }

        matchmakingManager.BeginMatchmaking();
    }

    public void OnCancelPressed()
    {
        if (matchmakingManager == null)
        {
            return;
        }

        matchmakingManager.CancelMatchmaking();
    }

    void HandleSearchStarted()
    {
        UpdateUI(searchingMessage, true);
    }

    void HandleClientConnecting()
    {
        UpdateUI(connectingMessage, true);
    }

    void HandleHostingStarted()
    {
        UpdateUI(hostingMessage, true);
    }

    void HandleCancelled()
    {
        UpdateIdleState();
    }

    void HandleMatchReady()
    {
        UpdateUI(matchReadyMessage, false);
    }

    void UpdateIdleState()
    {
        UpdateUI(idleMessage, false);
    }

    void UpdateUI(string text, bool showCancel)
    {
        if (statusLabel != null)
        {
            statusLabel.text = text;
        }

        if (readyButton != null)
        {
            readyButton.interactable = !showCancel;
        }

        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(showCancel);
            cancelButton.interactable = showCancel;
        }
    }
}
