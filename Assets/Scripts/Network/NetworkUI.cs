using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public Button quickJoinButton;
    public ConnectionManager connectionManager;
    void Start()
    {
        quickJoinButton.onClick.AddListener(connectionManager.QuickJoinOrCreateSession);
    }
}
