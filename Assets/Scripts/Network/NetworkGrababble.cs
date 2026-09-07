using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkGrababble : NetworkBehaviour
{
    private XRGrabInteractable grabInteractable;
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(SetOwnershipOnGrab);
    }
    
    private void SetOwnershipOnGrab(SelectEnterEventArgs args)
    {
        if (NetworkManager.Singleton.IsConnectedClient && !IsOwner)
        {
            NetworkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
            // The Same
            // NetworkObject.RequestOwnership();
        }
    }
}
