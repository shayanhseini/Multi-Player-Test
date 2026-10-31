using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VRNetworkPlayer : NetworkBehaviour
{
    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float respawnDelay = 3f;
    [SerializeField, Min(0f)] private float respawnRadius = 2f;

    [Header("Hitboxes")]
    [SerializeField, Min(0.05f)] private float headHitboxRadius = 0.16f;
    [SerializeField, Min(0.1f)] private float bodyHitboxHeight = 0.9f;
    [SerializeField, Min(0.05f)] private float bodyHitboxRadius = 0.22f;

    public readonly NetworkVariable<int> CurrentHealth = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> IsAlive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public List<Renderer> localRendererToHide;

    private Transform bodyHitbox;

    private void Awake() => CreateCombatHitboxes();

    public override void OnNetworkSpawn()
    {
        IsAlive.OnValueChanged += OnAliveChanged;
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
            IsAlive.Value = true;
        }

        if (IsOwner)
            foreach (Renderer item in localRendererToHide)
                if (item != null) item.enabled = false;
    }

    public override void OnNetworkDespawn()
    {
        IsAlive.OnValueChanged -= OnAliveChanged;
        base.OnNetworkDespawn();
    }

    public void ApplyDamageServer(int amount, ulong attackerClientId)
    {
        if (!IsServer || !IsAlive.Value || amount <= 0 || attackerClientId == OwnerClientId) return;
        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);
        if (CurrentHealth.Value > 0) return;
        IsAlive.Value = false;
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Vector2 offset = UnityEngine.Random.insideUnitCircle * respawnRadius;
        Vector3 spawnPosition = new(offset.x, 0f, offset.y);
        CurrentHealth.Value = maxHealth;
        IsAlive.Value = true;
        RespawnOwnerRpc(spawnPosition, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
    private void RespawnOwnerRpc(Vector3 spawnPosition, RpcParams rpcParams = default)
    {
        if (VRRigReferences.Instance != null) VRRigReferences.Instance.transform.position = spawnPosition;
    }

    private void Update()
    {
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
        if (head != null && head.Find("Combat Head Hitbox") == null)
        {
            GameObject headObject = new("Combat Head Hitbox");
            headObject.transform.SetParent(head, false);
            SphereCollider collider = headObject.AddComponent<SphereCollider>();
            collider.radius = headHitboxRadius;
            collider.isTrigger = true;
        }

        GameObject bodyObject = new("Combat Body Hitbox");
        bodyHitbox = bodyObject.transform;
        bodyHitbox.SetParent(transform, false);
        CapsuleCollider bodyCollider = bodyObject.AddComponent<CapsuleCollider>();
        bodyCollider.direction = 1;
        bodyCollider.height = Mathf.Max(bodyHitboxHeight, bodyHitboxRadius * 2f);
        bodyCollider.radius = bodyHitboxRadius;
        bodyCollider.isTrigger = true;
    }

    private void OnAliveChanged(bool wasAlive, bool isAlive)
    {
        foreach (Renderer item in localRendererToHide)
            if (item != null) item.enabled = isAlive && !IsOwner;
    }
}
