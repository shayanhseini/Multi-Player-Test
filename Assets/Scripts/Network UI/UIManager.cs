using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // =========================
    // Roles
    // =========================

    public static PlayerRole SelectedRole { get; private set; }


    // =========================
    // PANELS
    // =========================

    [Header("Panels")]
    public GameObject mainMenu;
    public GameObject presenterPassword;
    public GameObject presenterMenu;
    public GameObject createSession;
    public GameObject joinSession;
    public GameObject sessionCreated;
    public GameObject joinedSession;
    public GameObject connecting;


    // =========================
    // MAIN MENU BUTTONS
    // =========================

    [Header("Buttons & Input")]
    public Button presenterButton;
    public Button guestButton;


    // =========================
    // PRESENTER PASSWORD
    // =========================

    public TMP_InputField passwordInput;
    public Button passwordConfirmButton;
    public Button passwordBackButton;


    // =========================
    // PRESENTER MENU BUTTONS
    // =========================

    public Button createSessionButton;
    public Button joinSessionButton;
    public Button presenterMenuBackButton;


    // =========================
    // CREATE SESSION
    // =========================

    public Button createButton;
    public Button createSessionBackButton;


    // =========================
    // JOIN SESSION
    // =========================

    public TMP_InputField sessionCodeInput;
    public Button joinButton;
    public Button joinSessionBackButton;


    // =========================
    // JOINED SESSION
    // =========================

    public Button disconnectButton;


    // =========================
    // CONNECTION
    // =========================

    public ConnectionManager connectionManager;


    // =========================
    // SESSION CODE
    // =========================

    [Header("Text")]
    public TextMeshProUGUI sessionCreatedCodeText;
    public TextMeshProUGUI sessionJoinCodeText;


    // =========================
    // START
    // =========================

    private void Start()
    {
        // =========================
        // Main Menu
        // =========================

        presenterButton.onClick.AddListener(ShowPresenterPassword);
        guestButton.onClick.AddListener(ShowJoinSession);

        // Set Role
        presenterButton.onClick.AddListener(SetPresenterRole);
        guestButton.onClick.AddListener(SetGuestRole);


        // =========================
        // Presenter Password
        // =========================

        passwordConfirmButton.onClick.AddListener(CheckPresenterPassword);

        passwordBackButton.onClick.AddListener(OnPasswordBack);


        // =========================
        // Presenter Menu
        // =========================

        createSessionButton.onClick.AddListener(ShowCreateSession);
        joinSessionButton.onClick.AddListener(ShowJoinSession);

        presenterMenuBackButton.onClick.AddListener(OnPresenterMenuBack);


        // =========================
        // Create Session
        // =========================

        createButton.onClick.AddListener(OnCreateSession);

        createSessionBackButton.onClick.AddListener(OnCreateSessionBack);


        // =========================
        // Join Session
        // =========================

        joinButton.onClick.AddListener(OnJoinSession);

        joinSessionBackButton.onClick.AddListener(OnJoinSessionBack);


        // =========================
        // Joined Session
        // =========================

        disconnectButton.onClick.AddListener(OnDisconnect);


        // =========================
        // Connection Events
        // =========================

        connectionManager.onConnectionEvent.AddListener(OnConnectionStateChanged);

        connectionManager.onSessionCreated.AddListener(ShowSessionCreated);

        connectionManager.onSessionJoined.AddListener(ShowJoinedSession);

        connectionManager.onSessionCodeCreated.AddListener(SetSessionCode);


        // =========================
        // Initial Panel
        // =========================

        ShowMainMenu();
    }


    // =========================================================
    // KEYBOARD
    // =========================================================

    private void HideKeyboard()
    {
        if (KeyboardManager.Instance == null)
            return;

        KeyboardManager.Instance.HideKeyboard();

        KeyboardManager.Instance.ClearActiveInputField();
    }


    // =========================================================
    // PRESENTER PASSWORD
    // =========================================================

    private void CheckPresenterPassword()
    {
        string password = passwordInput.text;

        if (password == "1234")
        {
            Debug.Log("Presenter password correct.");

            passwordInput.text = "";

            HideKeyboard();

            ShowPresenterMenu();
        }
        else
        {
            Debug.Log("Wrong presenter password.");

            passwordInput.text = "";
        }
    }


    // =========================================================
    // PASSWORD BACK
    // =========================================================

    private void OnPasswordBack()
    {
        HideKeyboard();

        ShowMainMenu();
    }


    // =========================================================
    // CREATE SESSION
    // =========================================================

    private void OnCreateSession()
    {
        HideKeyboard();

        connectionManager.CreateSession();
    }


    private void SetSessionCode(string sessionCode)
    {
        sessionCreatedCodeText.text = sessionCode;
        sessionJoinCodeText.text = sessionCode;
    }


    // =========================================================
    // JOIN SESSION
    // =========================================================

    private void OnJoinSession()
    {
        string sessionCode = sessionCodeInput.text;

        if (string.IsNullOrWhiteSpace(sessionCode))
        {
            Debug.Log("Session Code is empty.");
            return;
        }

        HideKeyboard();

        connectionManager.JoinSession(sessionCode);
    }


    // =========================================================
    // JOIN SESSION BACK
    // =========================================================

    private void OnJoinSessionBack()
    {
        HideKeyboard();

        ShowMainMenu();
    }


    // =========================================================
    // PRESENTER MENU BACK
    // =========================================================

    private void OnPresenterMenuBack()
    {
        HideKeyboard();

        ShowMainMenu();
    }


    // =========================================================
    // CREATE SESSION BACK
    // =========================================================

    private void OnCreateSessionBack()
    {
        HideKeyboard();

        ShowPresenterMenu();
    }


    // =========================================================
    // DISCONNECT
    // =========================================================

    private void OnDisconnect()
    {
        HideKeyboard();

        connectionManager.Disconnect();
    }


    // =========================================================
    // CONNECTION STATE
    // =========================================================

    private void OnConnectionStateChanged(
        ConnectionManager.ConnectionState state)
    {
        switch (state)
        {
            case ConnectionManager.ConnectionState.Connecting:

                HideKeyboard();

                ShowConnecting();

                break;


            case ConnectionManager.ConnectionState.Success:

                break;


            case ConnectionManager.ConnectionState.Failed:

                Debug.Log("Connection Failed.");

                HideKeyboard();

                ShowMainMenu();

                break;


            case ConnectionManager.ConnectionState.Disconnecting:

                HideKeyboard();

                ShowConnecting();

                break;


            case ConnectionManager.ConnectionState.Disconnected:

                HideKeyboard();

                ShowMainMenu();

                break;
        }
    }


    // =========================================================
    // ROLE SELECTION
    // =========================================================

    private void SetPresenterRole()
    {
        SelectedRole = PlayerRole.Presenter;
    }


    private void SetGuestRole()
    {
        SelectedRole = PlayerRole.Guest;
    }


    // =========================================================
    // PANEL CONTROL
    // =========================================================

    public void HideAllPanels()
    {
        mainMenu.SetActive(false);

        presenterPassword.SetActive(false);

        presenterMenu.SetActive(false);

        createSession.SetActive(false);

        joinSession.SetActive(false);

        sessionCreated.SetActive(false);

        joinedSession.SetActive(false);

        connecting.SetActive(false);
    }


    public void ShowMainMenu()
    {
        HideAllPanels();

        mainMenu.SetActive(true);
    }


    public void ShowPresenterPassword()
    {
        HideAllPanels();

        presenterPassword.SetActive(true);
    }


    public void ShowPresenterMenu()
    {
        HideAllPanels();

        presenterMenu.SetActive(true);
    }


    public void ShowCreateSession()
    {
        HideAllPanels();

        createSession.SetActive(true);
    }


    public void ShowJoinSession()
    {
        HideAllPanels();

        joinSession.SetActive(true);
    }


    public void ShowSessionCreated()
    {
        HideAllPanels();

        sessionCreated.SetActive(true);
    }


    public void ShowJoinedSession()
    {
        HideAllPanels();

        joinedSession.SetActive(true);
    }


    public void ShowConnecting()
    {
        HideAllPanels();

        connecting.SetActive(true);
    }
}