using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkUIPermission : MonoBehaviour
{
    [Header("UI Permission")]
    [SerializeField]
    private InteractionPermission permission =
        InteractionPermission.Everyone;

    private Button button;
    private VRNetworkPlayer localPlayer;

    private void Start()
    {
        button = GetComponent<Button>();

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsConnectedClient)
        {
            FindLocalPlayer();
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
            return;

        FindLocalPlayer();
    }

    private void FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsConnectedClient)
            return;

        if (NetworkManager.Singleton.LocalClient == null)
            return;

        if (NetworkManager.Singleton.LocalClient.PlayerObject == null)
            return;

        localPlayer =
            NetworkManager.Singleton.LocalClient.PlayerObject
            .GetComponent<VRNetworkPlayer>();

        if (localPlayer == null)
            return;

        localPlayer.OnRoleReady += OnRoleReady;

        UpdatePermission(localPlayer.role.Value);
    }

    private void OnRoleReady(PlayerRole role)
    {
        UpdatePermission(role);
    }

    private void UpdatePermission(PlayerRole role)
    {
        if (button == null)
            return;

        switch (permission)
        {
            case InteractionPermission.Everyone:
                button.interactable = true;
                break;

            case InteractionPermission.PresenterOnly:
                button.interactable =
                    role == PlayerRole.Presenter;
                break;

            default:
                button.interactable = false;
                break;
        }
    }

    private void OnDestroy()
    {
        if (localPlayer != null)
        {
            localPlayer.OnRoleReady -= OnRoleReady;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;
        }
    }
}