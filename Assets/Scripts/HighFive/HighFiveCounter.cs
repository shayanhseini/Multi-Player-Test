using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HighFiveCounter : NetworkBehaviour
{
    private readonly NetworkVariable<int> highFiveCount =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    [SerializeField] private TextMeshProUGUI tmp;
    
    public static HighFiveCounter Instance {get; private set;}

    private void Awake()
    {
        Instance = this;
    }

    public void RequestIncrement()
    {
        if (!IsSpawned)
            return;

        RequestIncrementRpc();
    }

    [Rpc(
        SendTo.Authority,
        InvokePermission = RpcInvokePermission.Everyone
    )]
    private void RequestIncrementRpc()
    {
        highFiveCount.Value += 1;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
            NetworkObject.DontDestroyWithOwner = true;

        UpdateText(highFiveCount.Value);
        highFiveCount.OnValueChanged += OnCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        highFiveCount.OnValueChanged -= OnCountChanged;
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    private void OnCountChanged(int preValue, int newValue)
    {
        UpdateText(newValue);
    }

    private void UpdateText(int count)
    {
        if (tmp != null)
            tmp.text = count.ToString();
    }
}
