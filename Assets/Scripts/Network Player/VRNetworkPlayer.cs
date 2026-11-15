using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class VRNetworkPlayer : NetworkBehaviour
{
    [Header("Teams")]
    [SerializeField, Min(1)] private int teamCapacity = 6;
    public int TeamCapacity => teamCapacity;

    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0.05f)] private float blinkInterval = 0.2f;

    [Header("Hitboxes")]
    [SerializeField, Min(0.05f)] private float headHitboxRadius = 0.16f;
    [SerializeField, Min(0.1f)] private float bodyHitboxHeight = 0.9f;
    [SerializeField, Min(0.05f)] private float bodyHitboxRadius = 0.22f;

    public readonly NetworkVariable<int> CurrentHealth = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> IsAlive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<FixedString64Bytes> PlayerName = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<GameTeam> Team = new(GameTeam.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> Kills = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> Deaths = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> HasFlag = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> Captures = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int MaxHealth => maxHealth;

    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public List<Renderer> localRendererToHide;

    private Transform bodyHitbox;
    private Collider[] combatHitboxes;
    private Renderer[] avatarRenderers;
    private bool[] rendererDefaultStates;
    private Coroutine blinkRoutine;
    private PlayerNameplate nameplate;
    private MatchPhase lastAppliedPhase = (MatchPhase)byte.MaxValue;

    private void Awake()
    {
        CreateCombatHitboxes();
        avatarRenderers = GetComponentsInChildren<Renderer>(true);
        rendererDefaultStates = new bool[avatarRenderers.Length];
        for (int i = 0; i < avatarRenderers.Length; i++)
            rendererDefaultStates[i] = avatarRenderers[i].enabled;
    }

    public override void OnNetworkSpawn()
    {
        IsAlive.OnValueChanged += OnAliveChanged;
        PlayerName.OnValueChanged += OnNameChanged;
        Team.OnValueChanged += OnTeamChanged;
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
            IsAlive.Value = true;
            Team.Value = GameTeam.Waiting;
            Kills.Value = 0;
            Deaths.Value = 0;
            HasFlag.Value = false;
            Captures.Value = 0;
        }

        nameplate = GetComponentInChildren<PlayerNameplate>(true);
        nameplate?.Refresh();
        RefreshCombatHitboxes();
        ApplyLocalControlState();

        if (IsOwner)
            foreach (Renderer item in localRendererToHide)
                if (item != null) item.enabled = false;
    }

    public override void OnNetworkDespawn()
    {
        IsAlive.OnValueChanged -= OnAliveChanged;
        PlayerName.OnValueChanged -= OnNameChanged;
        Team.OnValueChanged -= OnTeamChanged;
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);
        if (nameplate != null)
            Destroy(nameplate.gameObject);
        if (IsOwner && VRRigReferences.Instance != null)
        {
            VRRigReferences.Instance.SetObserverFlight(false);
            VRRigReferences.Instance.SetLocomotionEnabled(true);
        }
        base.OnNetworkDespawn();
    }

    public void RequestTeam(GameTeam requestedTeam, string requestedName)
    {
        if (!IsOwner || !IsSpawned)
            return;

        string cleanName = string.IsNullOrWhiteSpace(requestedName)
            ? $"Player {OwnerClientId + 1}"
            : requestedName.Trim();
        if (cleanName.Length > 24)
            cleanName = cleanName[..24];
        RequestTeamRpc(new FixedString64Bytes(cleanName), requestedTeam);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestTeamRpc(
        FixedString64Bytes requestedName,
        GameTeam requestedTeam,
        RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (LanMatchController.Instance != null &&
            !LanMatchController.Instance.AllowsTeamChanges)
            return;

        if (requestedTeam == GameTeam.Observer)
        {
            if (OwnerClientId != NetworkManager.ServerClientId)
                return;
        }
        else if (requestedTeam != GameTeam.Attackers &&
                 requestedTeam != GameTeam.Defenders)
        {
            return;
        }

        if (requestedTeam != GameTeam.Observer &&
            Team.Value != requestedTeam &&
            CountTeamMembers(requestedTeam) >= teamCapacity)
            return;

        PlayerName.Value = requestedName;
        Team.Value = requestedTeam;
    }

    private int CountTeamMembers(GameTeam team)
    {
        int count = 0;
        foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
        {
            if (client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent(out VRNetworkPlayer player) &&
                player.Team.Value == team)
                count++;
        }
        return count;
    }

    public void ApplyDamageServer(int amount, ulong attackerClientId)
    {
        if (!IsServer || !IsAlive.Value || amount <= 0 ||
            LanMatchController.Instance == null ||
            LanMatchController.Instance.Phase.Value != MatchPhase.Playing ||
            attackerClientId == OwnerClientId || Team.Value == GameTeam.Waiting ||
            Team.Value == GameTeam.Observer)
            return;

        VRNetworkPlayer attacker = FindPlayer(attackerClientId);
        if (attacker == null || attacker.Team.Value == GameTeam.Waiting ||
            attacker.Team.Value == GameTeam.Observer || attacker.Team.Value == Team.Value)
            return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);
        bool killed = CurrentHealth.Value <= 0;
        ShowDamageTakenRpc(amount,
            RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
        attacker.ShowHitConfirmedRpc(killed,
            RpcTarget.Single(attacker.OwnerClientId, RpcTargetUse.Temp));
        if (!killed) return;
        Deaths.Value++;
        attacker.Kills.Value++;
        LanMatchObjective.Instance?.DropFlagForServer(OwnerClientId);
        HasFlag.Value = false;
        IsAlive.Value = false;
        LanMatchController.Instance?.NotifyKillServer(
            attacker.PlayerName.Value, PlayerName.Value);
    }

    [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
    private void ShowDamageTakenRpc(int amount, RpcParams rpcParams = default) =>
        TacticalMatchHUD.Instance?.ShowDamageTaken(amount);

    [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
    private void ShowHitConfirmedRpc(bool killed, RpcParams rpcParams = default) =>
        TacticalMatchHUD.Instance?.ShowHitConfirmed(killed);

    public void SetFlagHeldServer(bool held)
    {
        if (IsServer)
            HasFlag.Value = held;
    }

    public void ResetForRoundServer()
    {
        if (!IsServer)
            return;
        HasFlag.Value = false;
        CurrentHealth.Value = maxHealth;
        IsAlive.Value = true;
    }

    public void TeleportToMatchStartServer(Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
            return;
        TeleportToMatchStartRpc(
            position,
            rotation,
            RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
    private void TeleportToMatchStartRpc(
        Vector3 position,
        Quaternion rotation,
        RpcParams rpcParams = default)
    {
        if (IsOwner && VRRigReferences.Instance != null)
            VRRigReferences.Instance.TeleportTo(position, rotation);
    }

    public void TryEscapeWithFlag()
    {
        if (IsOwner && IsSpawned && HasFlag.Value)
            RequestFlagEscapeRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestFlagEscapeRpc(RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId ||
            !HasFlag.Value || !IsAlive.Value ||
            Team.Value != GameTeam.Attackers ||
            LanMatchObjective.Instance == null)
            return;

        Transform reference = head != null ? head : transform;
        if (Vector3.Distance(reference.position,
                LanMatchObjective.Instance.EscapePosition) >
            LanMatchObjective.Instance.EscapeRadius)
            return;

        HasFlag.Value = false;
        Captures.Value++;
        LanMatchObjective.Instance.ResetFlagHomeServer();
        LanMatchController.Instance?.NotifyFlagCapturedServer(PlayerName.Value);
        ShowCaptureRpc(PlayerName.Value);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void ShowCaptureRpc(FixedString64Bytes scorer)
    {
        Debug.Log($"[LAN Match] {scorer} captured the flag for Attackers.");
    }

    private VRNetworkPlayer FindPlayer(ulong clientId)
    {
        return NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) &&
               client.PlayerObject != null
            ? client.PlayerObject.GetComponent<VRNetworkPlayer>()
            : null;
    }

    private void Update()
    {
        MatchPhase phase = LanMatchController.Instance != null
            ? LanMatchController.Instance.Phase.Value
            : MatchPhase.Lobby;
        if (phase != lastAppliedPhase)
        {
            lastAppliedPhase = phase;
            RefreshCombatHitboxes();
            ApplyLocalControlState();
        }

        // Every network instance must react to phase changes. In particular, the
        // server copy owns hit validation even when this client does not own the rig.
        if (!IsOwner || VRRigReferences.Instance == null) return;

        head.SetPositionAndRotation(VRRigReferences.Instance.head.position, VRRigReferences.Instance.head.rotation);
        leftHand.SetPositionAndRotation(VRRigReferences.Instance.leftHand.position, VRRigReferences.Instance.leftHand.rotation);
        rightHand.SetPositionAndRotation(VRRigReferences.Instance.rightHand.position, VRRigReferences.Instance.rightHand.rotation);
    }

    private void LateUpdate()
    {
        if (bodyHitbox == null || head == null) return;
        bodyHitbox.position = head.position - Vector3.up * (bodyHitboxHeight * 0.55f);
        bodyHitbox.rotation = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
    }

    private void CreateCombatHitboxes()
    {
        List<Collider> hitboxes = new();
        if (head != null && head.Find("Combat Head Hitbox") == null)
        {
            GameObject headObject = new("Combat Head Hitbox");
            headObject.transform.SetParent(head, false);
            SphereCollider collider = headObject.AddComponent<SphereCollider>();
            collider.radius = headHitboxRadius;
            collider.isTrigger = true;
            hitboxes.Add(collider);
        }
        else if (head != null &&
                 head.Find("Combat Head Hitbox").TryGetComponent(out Collider existingHead))
        {
            hitboxes.Add(existingHead);
        }

        GameObject bodyObject = new("Combat Body Hitbox");
        bodyHitbox = bodyObject.transform;
        bodyHitbox.SetParent(transform, false);
        CapsuleCollider bodyCollider = bodyObject.AddComponent<CapsuleCollider>();
        bodyCollider.direction = 1;
        bodyCollider.height = Mathf.Max(bodyHitboxHeight, bodyHitboxRadius * 2f);
        bodyCollider.radius = bodyHitboxRadius;
        bodyCollider.isTrigger = true;
        hitboxes.Add(bodyCollider);
        combatHitboxes = hitboxes.ToArray();
    }

    private void OnAliveChanged(bool wasAlive, bool isAlive)
    {
        RefreshCombatHitboxes();

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        if (isAlive)
        {
            RestoreRendererDefaults();
            ApplyLocalControlState();
            return;
        }

        ApplyLocalControlState();

        if (!IsOwner)
            SetRemoteRenderersVisible(false);
    }

    private void OnNameChanged(FixedString64Bytes previous, FixedString64Bytes current) =>
        nameplate?.Refresh();

    private void OnTeamChanged(GameTeam previous, GameTeam current)
    {
        nameplate?.Refresh();
        RefreshCombatHitboxes();
        ApplyLocalControlState();
    }

    private void RefreshCombatHitboxes()
    {
        bool combatant = Team.Value == GameTeam.Attackers ||
                         Team.Value == GameTeam.Defenders;
        bool playing = LanMatchController.Instance != null &&
                       LanMatchController.Instance.Phase.Value == MatchPhase.Playing;
        SetCombatHitboxesEnabled(IsAlive.Value && combatant && playing);
    }

    private void ApplyLocalControlState()
    {
        if (!IsOwner || VRRigReferences.Instance == null)
            return;

        bool playing = LanMatchController.Instance != null &&
                       LanMatchController.Instance.Phase.Value == MatchPhase.Playing;
        MatchPhase phase = LanMatchController.Instance != null
            ? LanMatchController.Instance.Phase.Value
            : MatchPhase.Lobby;
        bool spectatorPhase = phase == MatchPhase.Playing ||
                              phase == MatchPhase.RoundEnd ||
                              phase == MatchPhase.MatchEnd ||
                              (phase == MatchPhase.Countdown &&
                               LanMatchController.Instance.RoundNumber.Value > 0);
        bool observer = Team.Value == GameTeam.Observer ||
                        (!IsAlive.Value && spectatorPhase);
        bool normalMovement = IsAlive.Value &&
                              Team.Value != GameTeam.Waiting &&
                              Team.Value != GameTeam.Observer &&
                              playing;
        VRRigReferences.Instance.SetObserverFlight(observer);
        VRRigReferences.Instance.SetLocomotionEnabled(normalMovement);
    }

    private IEnumerator BlinkWhileDead()
    {
        bool visible = false;
        while (!IsAlive.Value)
        {
            SetRemoteRenderersVisible(visible);
            visible = !visible;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private void SetCombatHitboxesEnabled(bool enabled)
    {
        if (combatHitboxes == null)
            return;

        foreach (Collider item in combatHitboxes)
            if (item != null)
                item.enabled = enabled;
    }

    private void SetRemoteRenderersVisible(bool visible)
    {
        for (int i = 0; i < avatarRenderers.Length; i++)
            if (avatarRenderers[i] != null && rendererDefaultStates[i])
                avatarRenderers[i].enabled = visible;
    }

    private void RestoreRendererDefaults()
    {
        for (int i = 0; i < avatarRenderers.Length; i++)
            if (avatarRenderers[i] != null)
                avatarRenderers[i].enabled = rendererDefaultStates[i];

        if (!IsOwner)
            return;

        foreach (Renderer item in localRendererToHide)
            if (item != null)
                item.enabled = false;
    }
}
