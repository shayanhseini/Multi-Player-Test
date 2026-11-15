using TMPro;
using UnityEngine;

public sealed class ObjectiveWorldMarker : MonoBehaviour
{
    public enum MarkerKind { Flag, Extraction, FlagHome }

    [SerializeField] private MarkerKind markerKind;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameObject visualRoot;

    private void LateUpdate()
    {
        VRNetworkPlayer local = FindLocalPlayer();
        bool playing = LanMatchController.Instance != null &&
                       LanMatchController.Instance.Phase.Value == MatchPhase.Playing;
        bool show = playing && local != null && local.IsAlive.Value &&
                    local.Team.Value != GameTeam.Observer &&
                    (markerKind == MarkerKind.Flag
                        ? !local.HasFlag.Value
                        : markerKind == MarkerKind.Extraction
                            ? local.Team.Value == GameTeam.Attackers && local.HasFlag.Value
                            : local.Team.Value == GameTeam.Defenders && local.HasFlag.Value);
        if (visualRoot != null && visualRoot.activeSelf != show)
            visualRoot.SetActive(show);
        if (!show) return;

        if (label != null)
            label.text = markerKind switch
            {
                MarkerKind.Flag => local.Team.Value == GameTeam.Attackers
                    ? "CAPTURE FLAG"
                    : "RETURN FLAG",
                MarkerKind.Extraction => "EXTRACTION ZONE",
                _ => "FLAG HOME"
            };
        Camera camera = Camera.main;
        if (camera != null)
            transform.rotation = Quaternion.LookRotation(
                transform.position - camera.transform.position, Vector3.up);
    }

    private static VRNetworkPlayer FindLocalPlayer()
    {
        foreach (VRNetworkPlayer player in
                 FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None))
            if (player.IsOwner) return player;
        return null;
    }
}
