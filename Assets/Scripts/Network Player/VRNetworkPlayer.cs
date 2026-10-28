using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class VRNetworkPlayer : NetworkBehaviour
{
    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float respawnDelay = 3f;
    [SerializeField, Min(0f)] private float respawnRadius = 2f;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    public List<Renderer> localRendererToHide;
    
    public NetworkVariable<PlayerRole> role =
        new NetworkVariable<PlayerRole>(
            PlayerRole.Guest,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
    
    public event Action<PlayerRole> OnRoleReady;

    public void SetRole(PlayerRole newRole)
    {
        if (!IsOwner)
            return;

        role.Value = newRole;
    }

    public override void OnNetworkSpawn()
    {
        role.OnValueChanged += OnRoleChanged;
        IsAlive.OnValueChanged += OnAliveChanged;

        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
            IsAlive.Value = true;
        }

        if (IsOwner)
        {
            role.Value = UIManager.SelectedRole;

            Debug.Log($"My Role: {role.Value}");

            foreach (Renderer rend in localRendererToHide)
            {
                rend.enabled = false;
            }
        }

        OnRoleReady?.Invoke(role.Value);
    }

    public override void OnNetworkDespawn()
    {
        role.OnValueChanged -= OnRoleChanged;
        IsAlive.OnValueChanged -= OnAliveChanged;
        base.OnNetworkDespawn();
    }

    public void ApplyDamageServer(int amount, ulong attackerClientId)
    {
        if (!IsServer || !IsAlive.Value || amount <= 0 || attackerClientId == OwnerClientId)
            return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);
        if (CurrentHealth.Value > 0)
            return;

        IsAlive.Value = false;
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector2 offset = UnityEngine.Random.insideUnitCircle * respawnRadius;
        Vector3 spawnPosition = new Vector3(offset.x, 0f, offset.y);
        CurrentHealth.Value = maxHealth;
        IsAlive.Value = true;
        RespawnOwnerRpc(
            spawnPosition,
            RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
    private void RespawnOwnerRpc(Vector3 spawnPosition, RpcParams rpcParams = default)
    {
        if (VRRigReferences.Instance != null)
            VRRigReferences.Instance.transform.position = spawnPosition;
    }

    private void OnAliveChanged(bool wasAlive, bool isAlive)
    {
        foreach (Renderer rend in localRendererToHide)
        {
            if (rend != null)
                rend.enabled = isAlive && !IsOwner;
        }
    }

    private void OnRoleChanged(PlayerRole previousRole, PlayerRole newRole)
    {
        OnRoleReady?.Invoke(newRole);
    }
    
    void Update()
    {
        if (IsOwner && VRRigReferences.Instance != null)
        {
            head.transform.position = VRRigReferences.Instance.head.position;
            head.transform.rotation = VRRigReferences.Instance.head.rotation;
            
            leftHand.transform.position = VRRigReferences.Instance.leftHand.position;
            leftHand.transform.rotation = VRRigReferences.Instance.leftHand.rotation;
            
            rightHand.transform.position = VRRigReferences.Instance.rightHand.position;
            rightHand.transform.rotation = VRRigReferences.Instance.rightHand.rotation;
        }
    }
    
}
