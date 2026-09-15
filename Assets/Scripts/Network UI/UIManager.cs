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

    void Start()
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
        passwordBackButton.onClick.AddListener(ShowMainMenu);


        // =========================
        // Presenter Menu
        // =========================

        createSessionButton.onClick.AddListener(ShowCreateSession);
        joinSessionButton.onClick.AddListener(ShowJoinSession);
        presenterMenuBackButton.onClick.AddListener(ShowMainMenu);


        // =========================
        // Create Session
        // =========================

        createButton.onClick.AddListener(OnCreateSession);
        createSessionBackButton.onClick.AddListener(ShowPresenterMenu);


        // =========================
        // Join Session
        // =========================

        joinButton.onClick.AddListener(OnJoinSession);
        joinSessionBackButton.onClick.AddListener(ShowMainMenu);


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


    // =========================
    // PRESENTER PASSWORD
    // =========================

    void CheckPresenterPassword()
    {
        string password = passwordInput.text;

        if (password == "1234")
        {
            Debug.Log("Presenter password correct.");

            passwordInput.text = "";

            ShowPresenterMenu();
        }
        else
        {
            Debug.Log("Wrong presenter password.");

            passwordInput.text = "";
        }
    }


    // =========================
    // CREATE SESSION
    // =========================

    void OnCreateSession()
    {
        connectionManager.CreateSession();
    }
    
    void SetSessionCode(string sessionCode)
    {
        sessionCreatedCodeText.text = sessionCode;
        sessionJoinCodeText.text = sessionCode;
    }


    // =========================
    // JOIN SESSION
    // =========================

    void OnJoinSession()
    {
        string sessionCode = sessionCodeInput.text;

        if (string.IsNullOrWhiteSpace(sessionCode))
        {
            Debug.Log("Session Code is empty.");
            return;
        }

        connectionManager.JoinSession(sessionCode);
    }


    // =========================
    // DISCONNECT
    // =========================

    void OnDisconnect()
    {
        connectionManager.Disconnect();
    }


    // =========================
    // CONNECTION STATE
    // =========================

    void OnConnectionStateChanged(ConnectionManager.ConnectionState state)
    {
        switch (state)
        {
            case ConnectionManager.ConnectionState.Connecting:

                ShowConnecting();

                break;


            case ConnectionManager.ConnectionState.Success:

                // Session Created / Joined
                // از Eventهای جداگانه مدیریت می‌شوند.

                break;


            case ConnectionManager.ConnectionState.Failed:

                Debug.Log("Connection Failed.");

                break;


            case ConnectionManager.ConnectionState.Disconnecting:

                ShowConnecting();

                break;


            case ConnectionManager.ConnectionState.Disconnected:

                ShowMainMenu();

                break;
        }
    }
    
    // =========================
    // ROLE SELECTION
    // =========================
    
    void SetPresenterRole()
    {
        SelectedRole = PlayerRole.Presenter;
    }

    void SetGuestRole()
    {
        SelectedRole = PlayerRole.Guest;
    }
    
    // =========================
    // PANEL CONTROL
    // =========================

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