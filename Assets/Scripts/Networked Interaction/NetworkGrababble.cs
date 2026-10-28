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

        if (grabInteractable != null)
            grabInteractable.selectEntered.AddListener(SetOwnershipOnGrab);
    }

    private void SetOwnershipOnGrab(SelectEnterEventArgs args)
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsConnectedClient ||
            !IsSpawned)
            return;

        if (IsOwner)
            return;

        RequestOwnershipRpc();
    }

    [Rpc(
        SendTo.Server,
        InvokePermission = RpcInvokePermission.Everyone
    )]
    private void RequestOwnershipRpc(RpcParams rpcParams = default)
    {
        if (!IsSpawned || !IsServer)
            return;

        NetworkObject.ChangeOwnership(
            rpcParams.Receive.SenderClientId
        );
    }

    public override void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(SetOwnershipOnGrab);

        base.OnDestroy();
    }
}
