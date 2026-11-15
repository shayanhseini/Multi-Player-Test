using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class PlayerHolsterSockets : NetworkBehaviour
{
    [Header("Editable Holster References")]
    [SerializeField] private XRSocketInteractor gunSocket;
    [SerializeField] private XRSocketInteractor grenadeSocket;
    [SerializeField] private XRGrabInteractable reusableGrenade;
    [SerializeField] private Transform trackingReference;
    [SerializeField] private Transform holsterRoot;
    [SerializeField, Min(0.2f)] private float waistDrop = 0.55f;

    private void Awake()
    {
        if (holsterRoot == null && gunSocket != null)
            holsterRoot = gunSocket.transform.parent;
        if (trackingReference == null && holsterRoot != null)
            trackingReference = holsterRoot.parent;
    }

    public override void OnNetworkSpawn()
    {
        VRNetworkPlayer player = GetComponent<VRNetworkPlayer>();
        if (player != null)
        {
            player.Team.OnValueChanged += OnTeamChanged;
            player.IsAlive.OnValueChanged += OnAliveChanged;
        }
        RefreshInteraction();
    }

    public override void OnNetworkDespawn()
    {
        VRNetworkPlayer player = GetComponent<VRNetworkPlayer>();
        if (player != null)
        {
            player.Team.OnValueChanged -= OnTeamChanged;
            player.IsAlive.OnValueChanged -= OnAliveChanged;
        }
        SetLocalInteraction(false);
        base.OnNetworkDespawn();
    }

    private void OnTeamChanged(GameTeam previous, GameTeam current) => RefreshInteraction();
    private void OnAliveChanged(bool previous, bool current) => RefreshInteraction();

    private void RefreshInteraction()
    {
        VRNetworkPlayer player = GetComponent<VRNetworkPlayer>();
        bool combatReady = IsOwner && player != null && player.IsAlive.Value &&
                           LanMatchController.Instance != null &&
                           LanMatchController.Instance.Phase.Value == MatchPhase.Playing &&
                           (player.Team.Value == GameTeam.Attackers ||
                            player.Team.Value == GameTeam.Defenders);
        SetLocalInteraction(combatReady);
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner || trackingReference == null || holsterRoot == null)
            return;

        RefreshInteraction();
        holsterRoot.position = trackingReference.position + Vector3.down * waistDrop;
        holsterRoot.rotation = Quaternion.Euler(0f, trackingReference.eulerAngles.y, 0f);
    }

    private void SetLocalInteraction(bool enabledForOwner)
    {
        if (gunSocket != null)
            gunSocket.enabled = enabledForOwner;
        if (grenadeSocket != null)
            grenadeSocket.enabled = enabledForOwner;
        if (reusableGrenade != null)
            reusableGrenade.enabled = enabledForOwner;
    }
}
