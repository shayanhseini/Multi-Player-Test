using System;
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

    [Header("Unlimited Grenade")]
    [SerializeField, Min(1)] private int grenadeDamage = 60;
    [SerializeField, Min(0.5f)] private float grenadeRadius = 3f;
    [SerializeField, Min(0.1f)] private float grenadeFuse = 1.5f;
    [SerializeField, Min(0.1f)] private float grenadeCooldown = 1f;
    [SerializeField, Min(1f)] private float grenadeRange = 10f;

    private readonly NetworkVariable<bool> gunHeld = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<Vector3> gunPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<Quaternion> gunRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private VRNetworkPlayer player;
    private SceneGunPickup localPickup;
    private GameObject remoteGunVisual;
    private double lastServerShotTime = double.NegativeInfinity;
    private bool serverGunHeld;
    private double lastServerGrenadeTime = double.NegativeInfinity;

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
        if (!IsOwner) return;
        localPickup = pickup;
        if (CanSendRpc())
            SetGunHeldRpc(true);
    }

    public void UnbindPickup(SceneGunPickup pickup)
    {
        if (!IsOwner || localPickup != pickup) return;
        localPickup = null;
        if (CanSendRpc())
            SetGunHeldRpc(false);
    }

    public void TryShoot(Vector3 origin, Vector3 direction)
    {
        if (IsOwner &&
            localPickup != null &&
            player != null &&
            player.IsAlive.Value &&
            IsCombatTeam(player.Team.Value) &&
            CanSendRpc())
            RequestShootRpc(origin, direction.normalized);
    }

    public bool TryThrowGrenade(Vector3 origin, Vector3 direction)
    {
        if (!IsOwner || player == null || !player.IsAlive.Value ||
            (player.Team.Value != GameTeam.Attackers &&
             player.Team.Value != GameTeam.Defenders) || !CanSendRpc())
            return false;

        RequestGrenadeRpc(origin, direction.normalized);
        return true;
    }

    private bool CanSendRpc()
    {
        return IsSpawned &&
               NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetGunHeldRpc(bool isHeld, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId == OwnerClientId) serverGunHeld = isHeld;
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
        if (rpcParams.Receive.SenderClientId != OwnerClientId || player == null ||
            !player.IsAlive.Value || !IsCombatTeam(player.Team.Value) || !serverGunHeld)
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

        RaycastHit[] hits = Physics.SphereCastAll(origin, bulletRadius, direction, maxDistance, hitMask, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            VRNetworkPlayer target = hit.collider.GetComponentInParent<VRNetworkPlayer>();
            if (target == player) continue;
            endPoint = hit.point;
            if (target != null) target.ApplyDamageServer(damage, OwnerClientId);
            break;
        }

        ShowShotRpc(origin, endPoint);
    }

    private static bool IsCombatTeam(GameTeam team) =>
        LanMatchController.Instance != null &&
        LanMatchController.Instance.Phase.Value == MatchPhase.Playing &&
        (team == GameTeam.Attackers || team == GameTeam.Defenders);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestGrenadeRpc(
        Vector3 origin,
        Vector3 direction,
        RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId ||
            player == null || !player.IsAlive.Value ||
            (player.Team.Value != GameTeam.Attackers &&
             player.Team.Value != GameTeam.Defenders))
            return;

        double now = NetworkManager.ServerTime.Time;
        if (now - lastServerGrenadeTime < grenadeCooldown ||
            player.rightHand == null ||
            Vector3.Distance(origin, player.rightHand.position) > 1.5f)
            return;

        lastServerGrenadeTime = now;
        direction = direction.sqrMagnitude > 0.5f
            ? direction.normalized
            : player.rightHand.forward;
        Vector3 target = origin + direction * grenadeRange;
        if (Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                grenadeRange,
                hitMask,
                QueryTriggerInteraction.Ignore))
            target = hit.point;

        ShowGrenadeRpc(origin, target, grenadeFuse);
        StartCoroutine(ExplodeGrenadeAfterDelay(target, OwnerClientId));
    }

    private IEnumerator ExplodeGrenadeAfterDelay(
        Vector3 position,
        ulong attackerClientId)
    {
        yield return new WaitForSeconds(grenadeFuse);
        System.Collections.Generic.HashSet<VRNetworkPlayer> damaged = new();
        foreach (Collider item in Physics.OverlapSphere(
                     position,
                     grenadeRadius,
                     hitMask,
                     QueryTriggerInteraction.Collide))
        {
            VRNetworkPlayer target =
                item.GetComponentInParent<VRNetworkPlayer>();
            if (target != null && damaged.Add(target))
                target.ApplyDamageServer(grenadeDamage, attackerClientId);
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void ShowGrenadeRpc(Vector3 origin, Vector3 target, float fuse)
    {
        StartCoroutine(AnimateGrenade(origin, target, fuse));
    }

    private IEnumerator AnimateGrenade(
        Vector3 origin,
        Vector3 target,
        float duration)
    {
        GameObject grenade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        grenade.name = "Grenade (Visual)";
        grenade.transform.localScale = Vector3.one * 0.14f;
        Destroy(grenade.GetComponent<Collider>());
        Renderer renderer = grenade.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.12f, 0.25f, 0.1f);

        float elapsed = 0f;
        duration = Mathf.Max(0.1f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(origin, target, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 1.5f;
            grenade.transform.position = position;
            yield return null;
        }

        grenade.transform.position = target;
        grenade.transform.localScale = Vector3.one * grenadeRadius * 0.35f;
        if (renderer != null)
            renderer.material.color = new Color(1f, 0.35f, 0.05f);
        yield return new WaitForSeconds(0.12f);
        Destroy(grenade);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void ShowShotRpc(Vector3 origin, Vector3 endPoint)
    {
        TacticalAudioFeedback.Instance?.PlayGunshot(origin);
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
        localPickup = null;
        serverGunHeld = false;
        if (remoteGunVisual != null)
            Destroy(remoteGunVisual);
        base.OnNetworkDespawn();
    }
}
