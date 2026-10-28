using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkShooter : NetworkBehaviour
{
    [SerializeField, Min(1)] private int damage = 25;
    [SerializeField, Min(1f)] private float maxDistance = 50f;
    [SerializeField, Min(0.02f)] private float fireInterval = 0.2f;
    [SerializeField, Min(0.005f)] private float bulletRadius = 0.02f;
    [SerializeField, Min(1f)] private float visualBulletSpeed = 35f;
    [SerializeField] private LayerMask hitMask = ~0;

    private readonly NetworkVariable<bool> gunHeld = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<Vector3> gunPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<Quaternion> gunRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private VRNetworkPlayer player;
    private SceneGunPickup localPickup;
    private GameObject remoteGunVisual;
    private double lastServerShotTime = double.NegativeInfinity;

    private void Awake() => player = GetComponent<VRNetworkPlayer>();

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (IsOwner)
        {
            bool holding = localPickup != null && localPickup.IsHeld;
            gunHeld.Value = holding;
            if (holding)
            {
                gunPosition.Value = localPickup.GunTransform.position;
                gunRotation.Value = localPickup.GunTransform.rotation;
            }
            return;
        }

        UpdateRemoteGunVisual();
    }

    public void BindPickup(SceneGunPickup pickup)
    {
        if (IsOwner)
            localPickup = pickup;
    }

    public void UnbindPickup(SceneGunPickup pickup)
    {
        if (IsOwner && localPickup == pickup)
            localPickup = null;
    }

    public void TryShoot(Vector3 origin, Vector3 direction)
    {
        if (IsOwner && localPickup != null && player != null && player.IsAlive.Value)
            RequestShootRpc(origin, direction.normalized);
    }

    private void UpdateRemoteGunVisual()
    {
        if (!gunHeld.Value)
        {
            if (remoteGunVisual != null)
                remoteGunVisual.SetActive(false);
            return;
        }

        if (remoteGunVisual == null && SceneGunPickup.Instance != null)
            remoteGunVisual = SceneGunPickup.Instance.CreateVisualClone();

        if (remoteGunVisual == null)
            return;

        remoteGunVisual.SetActive(true);
        remoteGunVisual.transform.SetPositionAndRotation(gunPosition.Value, gunRotation.Value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestShootRpc(Vector3 origin, Vector3 direction, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId || player == null || !player.IsAlive.Value)
            return;

        double now = NetworkManager.ServerTime.Time;
        if (now - lastServerShotTime < fireInterval)
            return;

        if (player.rightHand == null || Vector3.Distance(origin, player.rightHand.position) > 1.5f)
            return;

        direction = direction.normalized;
        if (direction.sqrMagnitude < 0.99f)
            return;

        lastServerShotTime = now;
        Vector3 endPoint = origin + direction * maxDistance;

        if (Physics.SphereCast(origin, bulletRadius, direction, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            VRNetworkPlayer target = hit.collider.GetComponentInParent<VRNetworkPlayer>();
            if (target != null && target != player)
                target.ApplyDamageServer(damage, OwnerClientId);
        }

        ShowShotRpc(origin, endPoint);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void ShowShotRpc(Vector3 origin, Vector3 endPoint)
    {
        if (SceneGunPickup.Instance != null)
            StartCoroutine(AnimateBullet(origin, endPoint));
    }

    private IEnumerator AnimateBullet(Vector3 origin, Vector3 endPoint)
    {
        GameObject bullet = SceneGunPickup.Instance.CreateBulletClone(origin);
        if (bullet == null)
            yield break;

        float duration = Mathf.Max(0.01f, Vector3.Distance(origin, endPoint) / visualBulletSpeed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bullet.transform.position = Vector3.Lerp(origin, endPoint, elapsed / duration);
            yield return null;
        }
        Destroy(bullet);
    }

    public override void OnNetworkDespawn()
    {
        if (remoteGunVisual != null)
            Destroy(remoteGunVisual);
        base.OnNetworkDespawn();
    }
}
