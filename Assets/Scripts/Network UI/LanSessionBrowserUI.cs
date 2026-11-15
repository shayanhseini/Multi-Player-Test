using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LanSessionBrowserUI : MonoBehaviour
{
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button[] sessionButtons;
    [SerializeField] private TextMeshProUGUI[] sessionLabels;

    private readonly List<ConnectionManager.LanSessionInfo> sessions = new();

    private void Awake()
    {
        refreshButton?.onClick.AddListener(RefreshSessions);
        backButton?.onClick.AddListener(GoBack);
        for (int i = 0; i < sessionButtons.Length; i++)
        {
            int index = i;
            sessionButtons[i]?.onClick.AddListener(() => Join(index));
        }
        if (connectionManager != null)
            connectionManager.SessionsDiscovered += OnSessionsDiscovered;
        ClearRows();
    }

    private void OnDestroy()
    {
        refreshButton?.onClick.RemoveListener(RefreshSessions);
        backButton?.onClick.RemoveListener(GoBack);
        if (connectionManager != null)
            connectionManager.SessionsDiscovered -= OnSessionsDiscovered;
    }

    public void RefreshSessions()
    {
        if (connectionManager == null) return;
        sessions.Clear();
        ClearRows();
        if (statusText != null) statusText.text = "SCANNING LOCAL NETWORK...";
        if (refreshButton != null) refreshButton.interactable = false;
        connectionManager.DiscoverLanSessions();
    }

    private void OnSessionsDiscovered(
        IReadOnlyList<ConnectionManager.LanSessionInfo> discovered)
    {
        sessions.Clear();
        sessions.AddRange(discovered);
        if (refreshButton != null) refreshButton.interactable = true;
        if (statusText != null)
            statusText.text = sessions.Count == 0
                ? "NO LAN ROOMS FOUND  •  CHECK WI-FI AND REFRESH"
                : $"{sessions.Count} LAN ROOM{(sessions.Count == 1 ? string.Empty : "S")} FOUND";

        for (int i = 0; i < sessionButtons.Length; i++)
        {
            bool available = i < sessions.Count;
            if (sessionButtons[i] != null)
                sessionButtons[i].gameObject.SetActive(available);
            if (!available || i >= sessionLabels.Length || sessionLabels[i] == null)
                continue;
            ConnectionManager.LanSessionInfo room = sessions[i];
            sessionLabels[i].text =
                $"{room.Name}     {room.Players}/{room.Capacity}\n" +
                $"<color=#7E94AA>{room.Endpoint}</color>";
        }
    }

    private void Join(int index)
    {
        if (index < 0 || index >= sessions.Count || connectionManager == null) return;
        uiManager?.ShowConnecting();
        connectionManager.JoinSession(sessions[index].Endpoint);
    }

    private void GoBack() => uiManager?.ShowMainMenu();

    private void ClearRows()
    {
        foreach (Button button in sessionButtons)
            if (button != null) button.gameObject.SetActive(false);
    }
}
