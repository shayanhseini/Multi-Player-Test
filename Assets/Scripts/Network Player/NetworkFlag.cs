using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class NetworkFlag : NetworkBehaviour
{
    public const ulong NoHolder = ulong.MaxValue;

    [Header("Editable Flag References")]
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private Rigidbody flagBody;
    [SerializeField] private Transform homePoint;
    [SerializeField, Min(0.25f)] private float maxGrabDistance = 1.75f;
    [SerializeField, Min(0.1f)] private float homeReturnRadius = 0.75f;

    public readonly NetworkVariable<ulong> HolderClientId = new(
        NoHolder,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool localSelection;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();
        if (flagBody == null)
            flagBody = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        grabInteractable.enabled = false;
        grabInteractable.selectEntered.AddListener(OnSelected);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    public override void OnNetworkSpawn()
    {
        HolderClientId.OnValueChanged += OnHolderChanged;
        grabInteractable.enabled = true;
        ApplyAuthorityState();
    }

    public override void OnNetworkDespawn()
    {
        HolderClientId.OnValueChanged -= OnHolderChanged;
        grabInteractable.enabled = false;
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelected);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
        base.OnDestroy();
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        if (!IsSpawned || NetworkManager == null ||
            !NetworkManager.IsConnectedClient)
        {
            CancelLocalSelection();
            return;
        }

        VRNetworkPlayer local = FindLocalPlayer();
        if (local == null ||
            (local.Team.Value != GameTeam.Attackers &&
             local.Team.Value != GameTeam.Defenders) ||
            (local.Team.Value == GameTeam.Defenders && IsAtHome()) ||
            LanMatchController.Instance == null ||
            LanMatchController.Instance.Phase.Value != MatchPhase.Playing ||
            !local.IsAlive.Value)
        {
            CancelLocalSelection();
            return;
        }

        localSelection = true;
        RequestGrabRpc();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!localSelection)
            return;
        localSelection = false;
        if (IsSpawned && NetworkManager != null && NetworkManager.IsConnectedClient)
            RequestReleaseRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestGrabRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        VRNetworkPlayer player = FindPlayer(sender);
        Transform reference = player != null && player.rightHand != null
            ? player.rightHand
            : player != null ? player.transform : null;

        bool validTeam = player != null &&
                         (player.Team.Value == GameTeam.Attackers ||
                          (player.Team.Value == GameTeam.Defenders && !IsAtHome()));
        if (HolderClientId.Value != NoHolder || player == null || !validTeam ||
            LanMatchController.Instance == null ||
            LanMatchController.Instance.Phase.Value != MatchPhase.Playing ||
            !player.IsAlive.Value ||
            reference == null ||
            Vector3.Distance(reference.position, transform.position) > maxGrabDistance)
        {
            RejectGrabRpc(RpcTarget.Single(sender, RpcTargetUse.Temp));
            return;
        }

        HolderClientId.Value = sender;
        player.SetFlagHeldServer(true);
        NetworkObject.ChangeOwnership(sender);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestReleaseRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (sender != HolderClientId.Value) return;
        VRNetworkPlayer player = FindPlayer(sender);
        if (player != null && player.Team.Value == GameTeam.Defenders &&
            Vector3.Distance(transform.position, GetHomePosition()) <= homeReturnRadius)
        {
            FixedString64Bytes returner = player.PlayerName.Value;
            ResetHomeServer();
            LanMatchController.Instance?.NotifyFlagReturnedServer(returner);
            return;
        }
        DropServer();
    }

    [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
    private void RejectGrabRpc(RpcParams rpcParams = default)
    {
        localSelection = false;
        CancelLocalSelection();
    }

    public void DropForClientServer(ulong clientId)
    {
        if (IsServer && HolderClientId.Value == clientId)
            DropServer();
    }

    public void ResetHomeServer()
    {
        if (!IsServer)
            return;
        ClearHolderServer();
        Transform target = homePoint != null ? homePoint : null;
        transform.SetPositionAndRotation(
            target != null ? target.position : initialPosition,
            target != null ? target.rotation : initialRotation);
        if (flagBody != null)
        {
            flagBody.linearVelocity = Vector3.zero;
            flagBody.angularVelocity = Vector3.zero;
        }
    }

    private void DropServer()
    {
        ClearHolderServer();
    }

    private void ClearHolderServer()
    {
        ulong previous = HolderClientId.Value;
        if (previous != NoHolder)
        {
            VRNetworkPlayer player = FindPlayer(previous);
            player?.SetFlagHeldServer(false);
        }
        HolderClientId.Value = NoHolder;
        if (NetworkObject.OwnerClientId != NetworkManager.ServerClientId)
            NetworkObject.RemoveOwnership();
    }

    private Vector3 GetHomePosition() => homePoint != null
        ? homePoint.position
        : initialPosition;

    private bool IsAtHome() =>
        HolderClientId.Value == NoHolder &&
        Vector3.Distance(transform.position, GetHomePosition()) <= homeReturnRadius;

    private void OnHolderChanged(ulong previous, ulong current) => ApplyAuthorityState();

    public override void OnGainedOwnership() => ApplyAuthorityState();

    public override void OnLostOwnership() => ApplyAuthorityState();

    private void ApplyAuthorityState()
    {
        if (flagBody == null || !IsSpawned)
            return;
        bool simulateHere = IsOwner;
        flagBody.isKinematic = !simulateHere;
        flagBody.useGravity = simulateHere;
    }

    private void CancelLocalSelection()
    {
        if (grabInteractable != null && grabInteractable.interactionManager != null &&
            grabInteractable.isSelected)
            grabInteractable.interactionManager.CancelInteractableSelection(
                (IXRSelectInteractable)grabInteractable);
    }

    private VRNetworkPlayer FindLocalPlayer()
    {
        foreach (VRNetworkPlayer player in
                 FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None))
            if (player.IsOwner)
                return player;
        return null;
    }

    private VRNetworkPlayer FindPlayer(ulong clientId)
    {
        return NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) &&
               client.PlayerObject != null
            ? client.PlayerObject.GetComponent<VRNetworkPlayer>()
            : null;
    }
}
