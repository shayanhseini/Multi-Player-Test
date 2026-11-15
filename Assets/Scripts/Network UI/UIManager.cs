using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Connection Panels")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject joinSession;
    [SerializeField] private GameObject sessionCreated;
    [SerializeField] private GameObject joinedSession;
    [SerializeField] private GameObject connecting;
    [SerializeField] private GameObject sessionBrowser;

    [Header("Buttons & Input")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField sessionCodeInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button joinSessionBackButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button hostDisconnectButton;

    [Header("Connection")]
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private TextMeshProUGUI sessionCreatedCodeText;
    [SerializeField] private LanSessionBrowserUI sessionBrowserController;

    private void Awake()
    {
        hostButton?.onClick.AddListener(CreateSession);
        clientButton?.onClick.AddListener(ShowSessionBrowser);
        joinButton?.onClick.AddListener(JoinSession);
        joinSessionBackButton?.onClick.AddListener(ShowMainMenu);
        disconnectButton?.onClick.AddListener(Disconnect);
        hostDisconnectButton?.onClick.AddListener(Disconnect);

        if (connectionManager != null)
        {
            connectionManager.onConnectionEvent.AddListener(OnConnectionStateChanged);
            connectionManager.onSessionCodeCreated.AddListener(SetSessionAddress);
            connectionManager.onSessionCreated.AddListener(ShowSessionCreated);
            connectionManager.onSessionJoined.AddListener(ShowJoinedSession);
        }

        ShowMainMenu();
    }

    private void OnDestroy()
    {
        hostButton?.onClick.RemoveListener(CreateSession);
        clientButton?.onClick.RemoveListener(ShowSessionBrowser);
        joinButton?.onClick.RemoveListener(JoinSession);
        joinSessionBackButton?.onClick.RemoveListener(ShowMainMenu);
        disconnectButton?.onClick.RemoveListener(Disconnect);
        hostDisconnectButton?.onClick.RemoveListener(Disconnect);

        if (connectionManager == null)
            return;

        connectionManager.onConnectionEvent.RemoveListener(OnConnectionStateChanged);
        connectionManager.onSessionCodeCreated.RemoveListener(SetSessionAddress);
        connectionManager.onSessionCreated.RemoveListener(ShowSessionCreated);
        connectionManager.onSessionJoined.RemoveListener(ShowJoinedSession);
    }

    private void CreateSession()
    {
        HideKeyboard();
        ShowConnecting();

        if (connectionManager != null)
            connectionManager.CreateSession();
        else
            ShowMainMenu();
    }

    private void JoinSession()
    {
        HideKeyboard();
        ShowConnecting();

        if (connectionManager != null)
            connectionManager.JoinFirstLanHost();
        else
            ShowMainMenu();
    }

    private void Disconnect()
    {
        HideKeyboard();
        ShowConnecting();

        if (connectionManager != null)
            connectionManager.Disconnect();
        else
            ShowMainMenu();
    }

    public void DisconnectFromSession() => Disconnect();

    private void SetSessionAddress(string address)
    {
        if (sessionCreatedCodeText != null)
            sessionCreatedCodeText.text = address;
    }

    private void OnConnectionStateChanged(ConnectionManager.ConnectionState state)
    {
        switch (state)
        {
            case ConnectionManager.ConnectionState.Connecting:
            case ConnectionManager.ConnectionState.Reconnecting:
            case ConnectionManager.ConnectionState.Disconnecting:
                ShowConnecting();
                break;

            case ConnectionManager.ConnectionState.Failed:
            case ConnectionManager.ConnectionState.Disconnected:
                HideKeyboard();
                ShowMainMenu();
                break;
        }
    }

    private static void HideKeyboard()
    {
        if (KeyboardManager.Instance == null)
            return;

        KeyboardManager.Instance.HideKeyboard();
        KeyboardManager.Instance.ClearActiveInputField();
    }

    private void HideAllPanels()
    {
        SetActive(mainMenu, false);
        SetActive(joinSession, false);
        SetActive(sessionCreated, false);
        SetActive(joinedSession, false);
        SetActive(connecting, false);
        SetActive(sessionBrowser, false);
    }

    public void ShowMainMenu()
    {
        SetLobbyChrome(true);
        HideAllPanels();
        SetActive(mainMenu, true);
    }

    public void ShowJoinSession()
    {
        SetLobbyChrome(true);
        HideAllPanels();
        SetActive(joinSession, true);
    }

    public void ShowSessionBrowser()
    {
        HideKeyboard();
        SetLobbyChrome(true);
        HideAllPanels();
        SetActive(sessionBrowser, true);
        sessionBrowserController?.RefreshSessions();
    }

    public void ShowSessionCreated()
    {
        SetLobbyChrome(true);
        HideAllPanels();
        PositionRoomCard(sessionCreated, new Vector2(-330f, -35f));
        SetActive(sessionCreated, true);
    }

    public void ShowJoinedSession()
    {
        SetLobbyChrome(true);
        HideAllPanels();
        PositionRoomCard(joinedSession, new Vector2(-330f, -35f));
        SetActive(joinedSession, true);
    }

    public void ShowConnecting()
    {
        SetLobbyChrome(true);
        HideAllPanels();
        SetActive(connecting, true);
    }

    public void EnterGameplay()
    {
        HideKeyboard();
        HideAllPanels();
        SetLobbyChrome(false);
    }

    public void ToggleInGameSessionMenu()
    {
        Transform root = GetLobbyRoot();
        if (root == null)
            return;

        bool opening = !IsLobbyChromeVisible();
        HideAllPanels();
        SetLobbyChrome(opening);
        if (!opening)
            return;

        GameObject page = NetworkManager.Singleton != null &&
                          NetworkManager.Singleton.IsHost
            ? sessionCreated
            : joinedSession;
        SetActive(page, true);
        PositionRoomCard(page, new Vector2(0f, -35f));
    }

    private Transform GetLobbyRoot() =>
        mainMenu != null ? mainMenu.transform.parent : null;

    private bool IsLobbyChromeVisible()
    {
        Transform root = GetLobbyRoot();
        Transform backdrop = root != null ? root.Find("Backdrop") : null;
        return backdrop != null && backdrop.gameObject.activeSelf;
    }

    private void SetLobbyChrome(bool visible)
    {
        Transform root = GetLobbyRoot();
        if (root == null)
            return;
        Transform backdrop = root.Find("Backdrop");
        Transform header = root.Find("Header");
        if (backdrop != null)
            backdrop.gameObject.SetActive(visible);
        if (header != null)
            header.gameObject.SetActive(visible);
    }

    private static void PositionRoomCard(GameObject page, Vector2 position)
    {
        Transform card = page != null ? page.transform.Find("Room Information") : null;
        if (card is RectTransform cardRect)
            cardRect.anchoredPosition = position;
    }

    private static void SetActive(GameObject panel, bool value)
    {
        if (panel != null)
            panel.SetActive(value);
    }
}
