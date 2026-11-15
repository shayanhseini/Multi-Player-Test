using System.Linq;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.Serialization;

public sealed class MatchLobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private GameObject scoreboardPanel;
    [SerializeField] private UIManager connectionUI;

    [Header("Team Selection")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TextMeshProUGUI teamStatus;
    [SerializeField] private Button attackersButton;
    [SerializeField] private Button defendersButton;
    [FormerlySerializedAs("godModeButton")]
    [SerializeField] private Button observerButton;

    [Header("Pre-Match Lobby")]
    [SerializeField] private GameObject preMatchPanel;
    [SerializeField] private TextMeshProUGUI attackersRosterText;
    [SerializeField] private TextMeshProUGUI defendersRosterText;
    [SerializeField] private TextMeshProUGUI preMatchStatusText;
    [SerializeField] private TextMeshProUGUI preMatchObserverText;
    [SerializeField] private Button switchAttackersButton;
    [SerializeField] private Button switchDefendersButton;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button preMatchLeaveButton;

    [Header("Scoreboard")]
    [SerializeField] private TextMeshProUGUI scoreboardText;
    [SerializeField] private TextMeshProUGUI attackersScoreboardText;
    [SerializeField] private TextMeshProUGUI defendersScoreboardText;
    [SerializeField] private TextMeshProUGUI observersScoreboardText;
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;
    [SerializeField, Min(0.5f)] private float scoreboardDistance = 1.5f;
    [SerializeField] private Vector3 scoreboardViewOffset = new(0f, -0.08f, 0f);

    private float nextRefresh;
    private bool scoreboardVisible;
    private bool primaryButtonWasPressed;
    private bool secondaryButtonWasPressed;
    private MatchPhase previousPhase = MatchPhase.Lobby;
    private int lastCountdownSecond = -1;

    private void Awake()
    {
        attackersButton?.onClick.AddListener(SelectAttackers);
        defendersButton?.onClick.AddListener(SelectDefenders);
        observerButton?.onClick.AddListener(SelectObserver);
        switchAttackersButton?.onClick.AddListener(SelectAttackers);
        switchDefendersButton?.onClick.AddListener(SelectDefenders);
        startMatchButton?.onClick.AddListener(StartMatch);
        preMatchLeaveButton?.onClick.AddListener(LeaveSession);
        selectionPanel?.SetActive(false);
        preMatchPanel?.SetActive(false);
        scoreboardPanel?.SetActive(false);
        if (connectionUI == null)
            connectionUI = FindFirstObjectByType<UIManager>();
    }

    private void OnDestroy()
    {
        attackersButton?.onClick.RemoveListener(SelectAttackers);
        defendersButton?.onClick.RemoveListener(SelectDefenders);
        observerButton?.onClick.RemoveListener(SelectObserver);
        switchAttackersButton?.onClick.RemoveListener(SelectAttackers);
        switchDefendersButton?.onClick.RemoveListener(SelectDefenders);
        startMatchButton?.onClick.RemoveListener(StartMatch);
        preMatchLeaveButton?.onClick.RemoveListener(LeaveSession);
    }

    private void Update()
    {
        HandleScoreboardInput();
        HandleSessionMenuInput();
        if (scoreboardVisible)
            UpdateScoreboardPose();

        if (Time.unscaledTime < nextRefresh)
            return;
        nextRefresh = Time.unscaledTime + refreshInterval;

        NetworkManager manager = NetworkManager.Singleton;
        bool connected = manager != null && manager.IsConnectedClient;
        VRNetworkPlayer localPlayer = connected ? FindLocalPlayer() : null;
        LanMatchController match = LanMatchController.Instance;
        MatchPhase phase = match != null ? match.Phase.Value : MatchPhase.Lobby;
        UpdateCountdownAudio(match, phase);

        if (selectionPanel != null)
            selectionPanel.SetActive(localPlayer != null &&
                                     localPlayer.Team.Value == GameTeam.Waiting &&
                                     phase == MatchPhase.Lobby);
        if (preMatchPanel != null)
            preMatchPanel.SetActive(localPlayer != null &&
                                    localPlayer.Team.Value != GameTeam.Waiting &&
                                    match != null && match.RoundNumber.Value == 0 &&
                                    (phase == MatchPhase.Lobby || phase == MatchPhase.Countdown));
        bool isPlaying = localPlayer != null &&
                         phase != MatchPhase.Lobby && phase != MatchPhase.Countdown;
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(isPlaying && scoreboardVisible);
        if (observerButton != null)
            observerButton.gameObject.SetActive(manager != null && manager.IsHost);
        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(manager != null && manager.IsHost);
            startMatchButton.interactable = manager != null && manager.IsHost &&
                                            match != null && match.CanStartMatch();
        }

        if (localPlayer == null)
            return;

        if (previousPhase != MatchPhase.Playing && phase == MatchPhase.Playing &&
            match != null && match.RoundNumber.Value == 1)
        {
            connectionUI?.EnterGameplay();
            scoreboardVisible = false;
        }
        previousPhase = phase;

        RefreshTeamStatus();
        RefreshPreMatchLobby(localPlayer, match, phase);
        RefreshScoreboard();
    }

    private void HandleSessionMenuInput()
    {
        bool keyboardPressed = Keyboard.current != null &&
                               Keyboard.current.escapeKey.isPressed;
        bool controllerPressed = false;
        UnityEngine.XR.InputDevice rightController =
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid)
            rightController.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.secondaryButton,
                out controllerPressed);

        bool pressed = keyboardPressed || controllerPressed;
        if (pressed && !secondaryButtonWasPressed)
        {
            VRNetworkPlayer local = FindLocalPlayer();
            if (local != null && LanMatchController.Instance != null &&
                LanMatchController.Instance.Phase.Value == MatchPhase.Playing)
                connectionUI?.ToggleInGameSessionMenu();
        }
        secondaryButtonWasPressed = pressed;
    }

    private void HandleScoreboardInput()
    {
        bool pressed = false;
        UnityEngine.XR.InputDevice rightController =
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid)
            rightController.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primaryButton,
                out pressed);

        if (pressed && !primaryButtonWasPressed)
            ToggleScoreboard();
        primaryButtonWasPressed = pressed;
    }

    private void ToggleScoreboard()
    {
        VRNetworkPlayer localPlayer = FindLocalPlayer();
        if (localPlayer == null || LanMatchController.Instance == null ||
            LanMatchController.Instance.Phase.Value != MatchPhase.Playing)
            return;

        scoreboardVisible = !scoreboardVisible;
        if (!scoreboardVisible || scoreboardPanel == null)
            return;

        UpdateScoreboardPose();
    }

    private void UpdateScoreboardPose()
    {
        if (scoreboardPanel == null)
            return;

        Transform viewer = Camera.main != null ? Camera.main.transform : null;
        if (viewer == null)
            return;

        Transform canvasTransform = scoreboardPanel.transform.parent;
        Vector3 flatForward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.01f)
            flatForward = Vector3.forward;
        Quaternion yawRotation = Quaternion.LookRotation(flatForward, Vector3.up);
        Vector3 horizontalOffset = yawRotation * new Vector3(
            scoreboardViewOffset.x, 0f, scoreboardViewOffset.z);
        Vector3 position = viewer.position + flatForward * scoreboardDistance + horizontalOffset;
        position.y = viewer.position.y + scoreboardViewOffset.y;
        canvasTransform.SetPositionAndRotation(position, yawRotation);
    }

    private void UpdateCountdownAudio(LanMatchController match, MatchPhase phase)
    {
        if (match == null || phase != MatchPhase.Countdown)
        {
            lastCountdownSecond = -1;
            return;
        }

        int seconds = match.GetCountdownSeconds();
        if (seconds <= 0 || seconds == lastCountdownSecond)
            return;
        lastCountdownSecond = seconds;
        TacticalAudioFeedback.Instance?.PlayCountdownTick();
    }

    private static VRNetworkPlayer FindLocalPlayer()
    {
        foreach (VRNetworkPlayer player in
                 FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None))
            if (player.IsOwner)
                return player;
        return null;
    }

    private void SelectAttackers() => SelectTeam(GameTeam.Attackers);
    private void SelectDefenders() => SelectTeam(GameTeam.Defenders);
    private void SelectObserver() => SelectTeam(GameTeam.Observer);
    private void StartMatch() => LanMatchController.Instance?.RequestStartMatch();
    private void LeaveSession() => connectionUI?.DisconnectFromSession();

    private void SelectTeam(GameTeam team)
    {
        VRNetworkPlayer player = FindLocalPlayer();
        if (player == null)
            return;
        player.RequestTeam(team, nameInput != null ? nameInput.text : string.Empty);
        KeyboardManager.Instance?.HideKeyboard();
    }

    private void RefreshTeamStatus()
    {
        if (teamStatus == null)
            return;
        VRNetworkPlayer[] players =
            FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None);
        int attackers = players.Count(p => p.Team.Value == GameTeam.Attackers);
        int defenders = players.Count(p => p.Team.Value == GameTeam.Defenders);
        int capacity = players.Length > 0 ? players[0].TeamCapacity : 6;
        bool teamChangesAllowed = LanMatchController.Instance == null ||
                                  LanMatchController.Instance.AllowsTeamChanges;
        if (attackersButton != null)
            attackersButton.interactable = teamChangesAllowed && attackers < capacity;
        if (defendersButton != null)
            defendersButton.interactable = teamChangesAllowed && defenders < capacity;
        teamStatus.text =
            $"ATTACKERS  {attackers}/{capacity}     DEFENDERS  {defenders}/{capacity}";
    }

    private void RefreshPreMatchLobby(
        VRNetworkPlayer localPlayer,
        LanMatchController match,
        MatchPhase phase)
    {
        VRNetworkPlayer[] players =
            FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None);
        int capacity = players.Length > 0 ? players[0].TeamCapacity : 6;
        int attackers = players.Count(p => p.Team.Value == GameTeam.Attackers);
        int defenders = players.Count(p => p.Team.Value == GameTeam.Defenders);

        if (attackersRosterText != null)
            attackersRosterText.text = BuildLobbyRoster(
                players, GameTeam.Attackers, localPlayer.OwnerClientId);
        if (defendersRosterText != null)
            defendersRosterText.text = BuildLobbyRoster(
                players, GameTeam.Defenders, localPlayer.OwnerClientId);
        if (preMatchObserverText != null)
            preMatchObserverText.text = BuildObserverRow(players);

        bool canSwitch = phase == MatchPhase.Lobby;
        if (switchAttackersButton != null)
            switchAttackersButton.interactable = canSwitch &&
                localPlayer.Team.Value != GameTeam.Attackers && attackers < capacity;
        if (switchDefendersButton != null)
            switchDefendersButton.interactable = canSwitch &&
                localPlayer.Team.Value != GameTeam.Defenders && defenders < capacity;

        if (preMatchStatusText == null)
            return;
        if (phase == MatchPhase.Countdown)
        {
            int seconds = match != null ? match.GetCountdownSeconds() : 0;
            preMatchStatusText.text =
                $"TEAMS LOCKED  •  MATCH STARTS IN {seconds}";
            preMatchStatusText.color = new Color(1f, 0.72f, 0.18f, 1f);
        }
        else
        {
            bool isHost = NetworkManager.Singleton != null &&
                          NetworkManager.Singleton.IsHost;
            preMatchStatusText.text = isHost
                ? "TEAMS OPEN  •  START WHEN EVERYONE IS READY"
                : "TEAMS OPEN  •  WAITING FOR HOST TO START";
            preMatchStatusText.color = new Color(0.42f, 0.88f, 0.7f, 1f);
        }
    }

    private static string BuildLobbyRoster(
        VRNetworkPlayer[] players,
        GameTeam team,
        ulong localClientId)
    {
        VRNetworkPlayer[] members = players
            .Where(p => p.Team.Value == team)
            .OrderBy(p => p.OwnerClientId)
            .ToArray();
        if (members.Length == 0)
            return "<color=#71859A>OPEN SLOT</color>";

        StringBuilder builder = new();
        foreach (VRNetworkPlayer player in members)
        {
            string playerName = player.PlayerName.Value.IsEmpty
                ? $"Player {player.OwnerClientId + 1}"
                : player.PlayerName.Value.ToString();
            string prefix = player.OwnerClientId == localClientId ? "● " : "  ";
            builder.AppendLine(prefix + playerName);
        }
        return builder.ToString();
    }

    private void RefreshScoreboard()
    {
        VRNetworkPlayer[] players =
            FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None)
                .OrderBy(p => p.Team.Value)
                .ThenByDescending(p => p.Kills.Value)
                .ToArray();

        if (attackersScoreboardText != null && defendersScoreboardText != null)
        {
            attackersScoreboardText.text = BuildTeamRows(players, GameTeam.Attackers);
            defendersScoreboardText.text = BuildTeamRows(players, GameTeam.Defenders);
            if (observersScoreboardText != null)
                observersScoreboardText.text = BuildObserverRow(players);
            return;
        }

        if (scoreboardText == null)
            return;

        StringBuilder builder = new();
        builder.AppendLine("SCOREBOARD                 K     D");
        AppendTeam(builder, players, GameTeam.Attackers, "ATTACKERS");
        AppendTeam(builder, players, GameTeam.Defenders, "DEFENDERS");
        AppendTeam(builder, players, GameTeam.Observer, "OBSERVER");
        scoreboardText.text = builder.ToString();
    }

    private static string BuildTeamRows(VRNetworkPlayer[] players, GameTeam team)
    {
        VRNetworkPlayer[] members = players.Where(p => p.Team.Value == team).ToArray();
        if (members.Length == 0)
            return "<color=#71859A>NO PLAYERS ASSIGNED</color>";

        StringBuilder builder = new();
        for (int i = 0; i < members.Length; i++)
        {
            VRNetworkPlayer player = members[i];
            string playerName = player.PlayerName.Value.IsEmpty
                ? $"Player {player.OwnerClientId + 1}"
                : player.PlayerName.Value.ToString();
            if (playerName.Length > 16)
                playerName = playerName[..16];
            string state = player.IsAlive.Value ? "ALIVE" : "DEAD";
            string stateColor = player.IsAlive.Value ? "#6DE3B3" : "#EF5B62";
            builder.AppendLine(
                $"<mspace=0.62em>{i + 1:00}</mspace>" +
                $"<pos=45>{playerName}" +
                $"<pos=345>{player.Kills.Value}" +
                $"<pos=390>{player.Deaths.Value}" +
                $"<pos=430><color={stateColor}>{state}</color>");
        }
        return builder.ToString();
    }

    private static string BuildObserverRow(VRNetworkPlayer[] players)
    {
        string[] observers = players
            .Where(p => p.Team.Value == GameTeam.Observer)
            .Select(p => p.PlayerName.Value.IsEmpty
                ? $"Player {p.OwnerClientId + 1}"
                : p.PlayerName.Value.ToString())
            .ToArray();
        return observers.Length == 0
            ? "OBSERVER  •  NONE"
            : $"OBSERVER  •  {string.Join("  /  ", observers)}";
    }

    private static void AppendTeam(
        StringBuilder builder,
        VRNetworkPlayer[] players,
        GameTeam team,
        string title)
    {
        VRNetworkPlayer[] members = players.Where(p => p.Team.Value == team).ToArray();
        if (members.Length == 0)
            return;

        builder.AppendLine($"\n{title}");
        foreach (VRNetworkPlayer player in members)
        {
            string playerName = player.PlayerName.Value.IsEmpty
                ? $"Player {player.OwnerClientId + 1}"
                : player.PlayerName.Value.ToString();
            builder.AppendLine($"{playerName,-24} {player.Kills.Value,3} {player.Deaths.Value,5}");
        }
    }
}
