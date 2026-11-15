using UnityEngine;

public sealed class LanMatchObjective : MonoBehaviour
{
    public static LanMatchObjective Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private NetworkFlag flag;
    [SerializeField] private Transform escapeZone;

    [Header("Objective Settings")]
    [SerializeField, Min(0.5f)] private float escapeRadius = 2f;
    [SerializeField, Min(0.05f)] private float checkInterval = 0.2f;

    private float nextCheck;

    public float EscapeRadius => escapeRadius;
    public Vector3 FlagPosition => flag != null ? flag.transform.position : transform.position;
    public Vector3 EscapePosition => escapeZone != null ? escapeZone.position : transform.position;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck)
            return;
        nextCheck = Time.unscaledTime + checkInterval;

        if (LanMatchController.Instance == null ||
            LanMatchController.Instance.Phase.Value != MatchPhase.Playing)
            return;

        VRNetworkPlayer local = null;
        foreach (VRNetworkPlayer player in
                 FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None))
            if (player.IsOwner)
            {
                local = player;
                break;
            }
        if (local == null || !local.IsAlive.Value)
            return;

        if (local.HasFlag.Value)
            local.TryEscapeWithFlag();
    }

    public void DropFlagForServer(ulong clientId) => flag?.DropForClientServer(clientId);
    public void ResetFlagHomeServer() => flag?.ResetHomeServer();

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
