using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkGrababble : NetworkBehaviour
{
    private XRGrabInteractable grabInteractable;

    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.selectEntered.AddListener(SetOwnershipOnGrab);
    }

    private void SetOwnershipOnGrab(SelectEnterEventArgs args)
    {
        if (!NetworkManager.Singleton.IsConnectedClient)
            return;

        if (IsOwner)
            return;

        NetworkObject.ChangeOwnership(
            NetworkManager.Singleton.LocalClientId
        );
    }
}