using UnityEngine;
using Unity.Netcode;

using UnityEngine.XR.Interaction.Toolkit.Filtering;

public class NetworkInteractionPermission : MonoBehaviour, IXRSelectFilter
{
    [Header("Interaction Permission")]
    [SerializeField]
    private InteractionPermission permission = InteractionPermission.Everyone;

    public bool canProcess => isActiveAndEnabled;

    public bool CanInteract(PlayerRole role)
    {
        switch (permission)
        {
            case InteractionPermission.Everyone:
                return true;

            case InteractionPermission.PresenterOnly:
                return role == PlayerRole.Presenter;

            default:
                return false;
        }
    }

    public bool Process(
        UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor,
        UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        if (!NetworkManager.Singleton.IsConnectedClient)
            return false;

        NetworkObject playerObject =
            NetworkManager.Singleton.LocalClient.PlayerObject;

        if (playerObject == null)
            return false;

        VRNetworkPlayer localPlayer =
            playerObject.GetComponent<VRNetworkPlayer>();

        if (localPlayer == null)
            return false;

        return CanInteract(localPlayer.role.Value);
    }
}