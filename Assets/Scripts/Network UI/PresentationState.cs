using Unity.Netcode;

public class PresentationState : NetworkBehaviour
{
    public NetworkVariable<int> currentPage =
        new NetworkVariable<int>();

    public int PageCount { get; private set; }

    public void SetPageCount(int count)
    {
        PageCount = count;
    }

    public void NextPage()
    {
        RequestNextPageRpc();
    }

    public void PreviousPage()
    {
        RequestPreviousPageRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestNextPageRpc()
    {
        if (PageCount <= 0)
            return;

        if (currentPage.Value < PageCount - 1)
        {
            currentPage.Value++;
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestPreviousPageRpc()
    {
        if (PageCount <= 0)
            return;

        if (currentPage.Value > 0)
        {
            currentPage.Value--;
        }
    }
}