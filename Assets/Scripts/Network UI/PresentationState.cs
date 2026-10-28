using Unity.Netcode;
using UnityEngine;

public class PresentationState : NetworkBehaviour
{
    public NetworkVariable<int> currentPage =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public int PageCount { get; private set; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
            NetworkObject.DontDestroyWithOwner = true;
    }

    public void SetPageCount(int count)
    {
        PageCount = Mathf.Max(0, count);

        if (IsOwner && PageCount > 0 && currentPage.Value >= PageCount)
            currentPage.Value = PageCount - 1;

        Debug.Log(
            $"[PresentationState] Page count: {PageCount}"
        );
    }

    public void NextPage()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning(
                "[PresentationState] NetworkObject is not spawned."
            );
            return;
        }

        RequestNextPageRpc();
    }

    public void PreviousPage()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning(
                "[PresentationState] NetworkObject is not spawned."
            );
            return;
        }

        RequestPreviousPageRpc();
    }

    [Rpc(
        SendTo.Authority,
        InvokePermission = RpcInvokePermission.Everyone
    )]
    private void RequestNextPageRpc(RpcParams rpcParams = default)
    {
        if (!NetworkRoleUtility.HasRole(
                rpcParams.Receive.SenderClientId,
                PlayerRole.Presenter))
        {
            Debug.LogWarning(
                "[PresentationState] Rejected NextPage from a non-presenter."
            );
            return;
        }

        Debug.Log(
            $"[PresentationState] Next: " +
            $"page={currentPage.Value}, count={PageCount}"
        );

        if (PageCount <= 0)
        {
            Debug.LogWarning(
                "[PresentationState] PageCount is zero."
            );
            return;
        }

        currentPage.Value = Mathf.Min(
            currentPage.Value + 1,
            PageCount - 1
        );
    }

    [Rpc(
        SendTo.Authority,
        InvokePermission = RpcInvokePermission.Everyone
    )]
    private void RequestPreviousPageRpc(RpcParams rpcParams = default)
    {
        if (!NetworkRoleUtility.HasRole(
                rpcParams.Receive.SenderClientId,
                PlayerRole.Presenter))
        {
            Debug.LogWarning(
                "[PresentationState] Rejected PreviousPage from a non-presenter."
            );
            return;
        }

        currentPage.Value = Mathf.Max(
            currentPage.Value - 1,
            0
        );
    }
}
