using Unity.Netcode;

public class PresentationState : NetworkBehaviour
{
    public NetworkVariable<int> currentPage =
        new NetworkVariable<int>();

    public void NextPage()
    {
        RequestNextPageRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestNextPageRpc()
    {
        currentPage.Value++;
    }
}