using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class PlayerNameplate : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;
    [SerializeField] private Color waitingColor = Color.white;
    [SerializeField] private Color attackersColor = new(1f, 0.25f, 0.2f);
    [SerializeField] private Color defendersColor = new(0.2f, 0.55f, 1f);
    [FormerlySerializedAs("godModeColor")]
    [SerializeField] private Color observerColor = new(1f, 0.85f, 0.15f);

    private VRNetworkPlayer player;

    private void Awake()
    {
        player = GetComponentInParent<VRNetworkPlayer>();
        if (label == null)
            label = GetComponent<TextMeshPro>();
    }

    public void Refresh()
    {
        if (player == null)
            player = GetComponentInParent<VRNetworkPlayer>();
        if (player == null || label == null)
            return;

        label.text = player.PlayerName.Value.IsEmpty
            ? $"Player {player.OwnerClientId + 1}"
            : player.PlayerName.Value.ToString();
        label.color = player.Team.Value switch
        {
            GameTeam.Attackers => attackersColor,
            GameTeam.Defenders => defendersColor,
            GameTeam.Observer => observerColor,
            _ => waitingColor
        };
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera != null)
            transform.rotation = Quaternion.LookRotation(
                transform.position - camera.transform.position,
                Vector3.up);
    }
}
