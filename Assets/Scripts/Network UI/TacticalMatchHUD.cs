using TMPro;
using UnityEngine;

public sealed class TacticalMatchHUD : MonoBehaviour
{
    public static TacticalMatchHUD Instance { get; private set; }

    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TextMeshProUGUI roundTimerText;
    [SerializeField] private TextMeshProUGUI teamScoreText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI announcementText;
    [SerializeField] private TextMeshProUGUI killFeedText;
    [SerializeField] private TextMeshProUGUI spectatorText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private VRNetworkPlayer localPlayer;
    private float feedbackUntil;
    private Canvas hudCanvas;
    private string lastAnnouncement;

    private void Awake()
    {
        Instance = this;
        hudCanvas = GetComponent<Canvas>();
    }

    private void Update()
    {
        LanMatchController match = LanMatchController.Instance;
        if (localPlayer == null || !localPlayer.IsSpawned || !localPlayer.IsOwner)
            localPlayer = FindLocalPlayer();

        bool visible = match != null && match.IsSpawned &&
                       match.RoundNumber.Value > 0;
        if (hudCanvas != null)
            hudCanvas.enabled = visible;
        if (!visible) return;

        int seconds = match.GetPhaseSeconds();
        if (roundTimerText != null)
            roundTimerText.text = match.Phase.Value == MatchPhase.Playing
                ? $"R{match.RoundNumber.Value:00}   {seconds / 60:00}:{seconds % 60:00}"
                : $"{match.Phase.Value.ToString().ToUpperInvariant()}   {seconds}";
        if (teamScoreText != null)
            teamScoreText.text =
                $"ATTACKERS  {match.AttackersScore.Value}   :   {match.DefendersScore.Value}  DEFENDERS";
        if (announcementText != null)
            announcementText.text = match.Announcement.Value.ToString();
        string announcement = match.Announcement.Value.ToString();
        if (!string.IsNullOrEmpty(announcement) && announcement != lastAnnouncement)
        {
            if (announcement.Contains("WIN") || announcement.Contains("MATCH OVER"))
                TacticalAudioFeedback.Instance?.PlayRoundWin();
            else if (announcement.Contains("FIGHT"))
                TacticalAudioFeedback.Instance?.PlayRoundStart();
            lastAnnouncement = announcement;
        }
        if (killFeedText != null)
            killFeedText.text = match.KillFeed.Value.ToString();

        bool dead = localPlayer != null && !localPlayer.IsAlive.Value;
        bool observer = localPlayer != null && localPlayer.Team.Value == GameTeam.Observer;
        if (spectatorText != null)
            spectatorText.text = dead
                ? "SPECTATOR  •  FREE FLIGHT  •  NEXT ROUND RESPAWN"
                : observer ? "ADMIN OBSERVER  •  FREE FLIGHT" : string.Empty;

        if (objectiveText != null)
            objectiveText.text = BuildObjective(localPlayer);
        if (ammoText != null)
            ammoText.text = "AMMO  ∞";

        if (feedbackText != null && Time.unscaledTime >= feedbackUntil)
            feedbackText.text = string.Empty;
    }

    public void ShowHitConfirmed(bool killed)
    {
        if (feedbackText == null) return;
        feedbackText.text = killed ? "✕  ELIMINATED" : "+  HIT";
        feedbackText.color = killed ? new Color(1f, 0.32f, 0.26f) : Color.white;
        feedbackUntil = Time.unscaledTime + 0.45f;
        TacticalAudioFeedback.Instance?.PlayHitConfirm();
    }

    public void ShowDamageTaken(int amount)
    {
        if (feedbackText == null) return;
        feedbackText.text = $"DAMAGE  -{amount}";
        feedbackText.color = new Color(1f, 0.2f, 0.18f);
        feedbackUntil = Time.unscaledTime + 0.6f;
        TacticalAudioFeedback.Instance?.PlayDamageTaken();
    }

    private static string BuildObjective(VRNetworkPlayer player)
    {
        if (player == null) return "CONNECTING TO MATCH DATA";
        if (player.Team.Value == GameTeam.Observer) return "OBSERVE MATCH";
        if (player.HasFlag.Value)
            return player.Team.Value == GameTeam.Attackers
                ? "OBJECTIVE  •  REACH EXTRACTION ZONE"
                : "OBJECTIVE  •  RETURN FLAG TO HOME";
        return player.Team.Value == GameTeam.Attackers
            ? "OBJECTIVE  •  CAPTURE AND EXTRACT THE FLAG"
            : "OBJECTIVE  •  DEFEND THE FLAG";
    }

    private static VRNetworkPlayer FindLocalPlayer()
    {
        foreach (VRNetworkPlayer player in
                 FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None))
            if (player.IsOwner) return player;
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
