using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public sealed class LanMatchController : NetworkBehaviour
{
    public static LanMatchController Instance { get; private set; }

    [Header("Team Match Start Points")]
    [SerializeField] private Transform attackersStartPoint;
    [SerializeField] private Transform defendersStartPoint;

    [Header("Match Settings")]
    [SerializeField, Min(1f)] private float countdownDuration = 5f;
    [SerializeField, Min(30f)] private float roundDuration = 180f;
    [SerializeField, Min(1f)] private float roundEndDuration = 6f;
    [SerializeField, Min(1f)] private float matchEndDuration = 10f;
    [SerializeField, Min(1)] private int roundsToWin = 4;
    [SerializeField] private bool requireBothTeams = true;

    public readonly NetworkVariable<MatchPhase> Phase = new(MatchPhase.Lobby,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<double> PhaseEndServerTime = new(0d,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> RoundNumber = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> AttackersScore = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> DefendersScore = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<FixedString128Bytes> Announcement = new(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<FixedString512Bytes> KillFeed = new(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool AllowsTeamChanges => IsSpawned && Phase.Value == MatchPhase.Lobby;
    public int RoundsToWin => roundsToWin;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer) ResetMatchServer();
    }

    private void Update()
    {
        if (!IsServer || NetworkManager == null) return;
        double now = NetworkManager.ServerTime.Time;
        switch (Phase.Value)
        {
            case MatchPhase.Countdown when now >= PhaseEndServerTime.Value:
                BeginRoundServer();
                break;
            case MatchPhase.Playing:
                if (now >= PhaseEndServerTime.Value)
                    EndRoundServer(GameTeam.Defenders, "TIME EXPIRED");
                else
                    CheckEliminationServer();
                break;
            case MatchPhase.RoundEnd when now >= PhaseEndServerTime.Value:
                if (AttackersScore.Value >= roundsToWin || DefendersScore.Value >= roundsToWin)
                    BeginMatchEndServer();
                else
                    BeginCountdownServer();
                break;
            case MatchPhase.MatchEnd when now >= PhaseEndServerTime.Value:
                ResetMatchServer();
                break;
        }
    }

    public void RequestStartMatch()
    {
        if (IsSpawned && NetworkManager != null && NetworkManager.IsHost)
            RequestStartMatchRpc();
    }

    public bool CanStartMatch()
    {
        if (Phase.Value != MatchPhase.Lobby) return false;
        return !requireBothTeams ||
               (CountTeam(GameTeam.Attackers) > 0 && CountTeam(GameTeam.Defenders) > 0);
    }

    public int GetPhaseSeconds()
    {
        if (Phase.Value == MatchPhase.Lobby || NetworkManager == null) return 0;
        return Mathf.Max(0, Mathf.CeilToInt(
            (float)(PhaseEndServerTime.Value - NetworkManager.ServerTime.Time)));
    }

    public int GetCountdownSeconds() => Phase.Value == MatchPhase.Countdown
        ? GetPhaseSeconds()
        : 0;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestStartMatchRpc(RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId || !CanStartMatch())
            return;
        AttackersScore.Value = 0;
        DefendersScore.Value = 0;
        RoundNumber.Value = 0;
        KillFeed.Value = default;
        BeginCountdownServer();
    }

    private void BeginCountdownServer()
    {
        Phase.Value = MatchPhase.Countdown;
        PhaseEndServerTime.Value = NetworkManager.ServerTime.Time + countdownDuration;
        Announcement.Value = new FixedString128Bytes(
            RoundNumber.Value == 0 ? "MATCH STARTING" : "NEXT ROUND STARTING");
    }

    private void BeginRoundServer()
    {
        RoundNumber.Value++;
        Phase.Value = MatchPhase.Playing;
        PhaseEndServerTime.Value = NetworkManager.ServerTime.Time + roundDuration;
        Announcement.Value = new FixedString128Bytes($"ROUND {RoundNumber.Value}  •  FIGHT");
        KillFeed.Value = default;
        LanMatchObjective.Instance?.ResetFlagHomeServer();
        foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
        {
            if (client.PlayerObject == null ||
                !client.PlayerObject.TryGetComponent(out VRNetworkPlayer player)) continue;
            player.ResetForRoundServer();
            Transform target = player.Team.Value switch
            {
                GameTeam.Attackers => attackersStartPoint,
                GameTeam.Defenders => defendersStartPoint,
                _ => null
            };
            if (target != null)
                player.TeleportToMatchStartServer(target.position, target.rotation);
        }
    }

    private void CheckEliminationServer()
    {
        int attackers = 0, attackersAlive = 0;
        foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
        {
            if (client.PlayerObject == null ||
                !client.PlayerObject.TryGetComponent(out VRNetworkPlayer player)) continue;
            if (player.Team.Value == GameTeam.Attackers)
            {
                attackers++;
                if (player.IsAlive.Value) attackersAlive++;
            }
        }
        if (attackers > 0 && attackersAlive == 0)
            EndRoundServer(GameTeam.Defenders, "ATTACKERS ELIMINATED");
        // Eliminating every defender does not complete the attackers' objective.
        // Attackers still have to capture and extract the flag before time expires.
    }

    public void NotifyFlagCapturedServer(FixedString64Bytes playerName)
    {
        if (IsServer && Phase.Value == MatchPhase.Playing)
            EndRoundServer(GameTeam.Attackers, $"{playerName} EXTRACTED THE FLAG");
    }

    public void NotifyFlagReturnedServer(FixedString64Bytes playerName)
    {
        if (IsServer && Phase.Value == MatchPhase.Playing)
            Announcement.Value = new FixedString128Bytes($"FLAG RETURNED  •  {playerName}");
    }

    public void NotifyKillServer(FixedString64Bytes killer, FixedString64Bytes victim)
    {
        if (!IsServer) return;
        string line = $"{killer}  ▸  {victim}";
        string existing = KillFeed.Value.ToString();
        string[] rows = (existing + "\n" + line).Trim().Split('\n');
        int start = Mathf.Max(0, rows.Length - 5);
        KillFeed.Value = new FixedString512Bytes(
            string.Join("\n", rows, start, rows.Length - start));
    }

    private void EndRoundServer(GameTeam winner, string reason)
    {
        if (Phase.Value != MatchPhase.Playing) return;
        if (winner == GameTeam.Attackers) AttackersScore.Value++;
        else if (winner == GameTeam.Defenders) DefendersScore.Value++;
        Phase.Value = MatchPhase.RoundEnd;
        PhaseEndServerTime.Value = NetworkManager.ServerTime.Time + roundEndDuration;
        Announcement.Value = new FixedString128Bytes(
            $"{winner.ToString().ToUpperInvariant()} WIN  •  {reason}");
        LanMatchObjective.Instance?.ResetFlagHomeServer();
    }

    private void BeginMatchEndServer()
    {
        Phase.Value = MatchPhase.MatchEnd;
        PhaseEndServerTime.Value = NetworkManager.ServerTime.Time + matchEndDuration;
        string winner = AttackersScore.Value > DefendersScore.Value ? "ATTACKERS" : "DEFENDERS";
        Announcement.Value = new FixedString128Bytes($"MATCH OVER  •  {winner} WIN");
    }

    private void ResetMatchServer()
    {
        Phase.Value = MatchPhase.Lobby;
        PhaseEndServerTime.Value = 0d;
        RoundNumber.Value = 0;
        AttackersScore.Value = 0;
        DefendersScore.Value = 0;
        Announcement.Value = default;
        KillFeed.Value = default;
        LanMatchObjective.Instance?.ResetFlagHomeServer();
    }

    private int CountTeam(GameTeam team)
    {
        int count = 0;
        if (NetworkManager == null) return 0;
        foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
            if (client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent(out VRNetworkPlayer player) &&
                player.Team.Value == team) count++;
        return count;
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }
}
