using TMPro;
using UnityEngine;

public sealed class PlayerWristHealthUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image healthFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Color healthyColor = new(0.19f, 0.86f, 0.58f, 1f);
    [SerializeField] private Color criticalColor = new(0.86f, 0.23f, 0.28f, 1f);

    private VRNetworkPlayer localPlayer;

    private void Update()
    {
        if (localPlayer == null || !localPlayer.IsSpawned || !localPlayer.IsOwner)
            localPlayer = FindLocalPlayer();

        int current = localPlayer != null ? localPlayer.CurrentHealth.Value : 0;
        int maximum = localPlayer != null ? Mathf.Max(1, localPlayer.MaxHealth) : 100;
        float normalized = Mathf.Clamp01((float)current / maximum);

        if (healthFill != null)
        {
            healthFill.fillAmount = normalized;
            healthFill.color = Color.Lerp(criticalColor, healthyColor, normalized);
        }
        if (healthText != null)
            healthText.text = localPlayer != null ? $"{current:000}" : "---";
    }

    private static VRNetworkPlayer FindLocalPlayer()
    {
        foreach (VRNetworkPlayer player in
                 FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None))
            if (player.IsOwner)
                return player;
        return null;
    }
}
