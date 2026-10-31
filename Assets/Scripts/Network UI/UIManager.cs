using TMPro;
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

    [Header("Buttons & Input")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField sessionCodeInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button joinSessionBackButton;
    [SerializeField] private Button disconnectButton;

    [Header("Connection")]
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private TextMeshProUGUI sessionCreatedCodeText;

    private void Awake()
    {
        hostButton?.onClick.AddListener(CreateSession);
        clientButton?.onClick.AddListener(JoinSession);
        joinButton?.onClick.AddListener(JoinSession);
        joinSessionBackButton?.onClick.AddListener(ShowMainMenu);
        disconnectButton?.onClick.AddListener(Disconnect);

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
        clientButton?.onClick.RemoveListener(JoinSession);
        joinButton?.onClick.RemoveListener(JoinSession);
        joinSessionBackButton?.onClick.RemoveListener(ShowMainMenu);
        disconnectButton?.onClick.RemoveListener(Disconnect);

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
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        SetActive(mainMenu, true);
    }

    public void ShowJoinSession()
    {
        HideAllPanels();
        SetActive(joinSession, true);
    }

    public void ShowSessionCreated()
    {
        HideAllPanels();
        SetActive(sessionCreated, true);
    }

    public void ShowJoinedSession()
    {
        HideAllPanels();
        SetActive(joinedSession, true);
    }

    public void ShowConnecting()
    {
        HideAllPanels();
        SetActive(connecting, true);
    }

    private static void SetActive(GameObject panel, bool value)
    {
        if (panel != null)
            panel.SetActive(value);
    }
}
