using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public Button quickJoinButton, disconnectButton;
    public ConnectionManager connectionManager;

    public GameObject offlinePanel, connectingPanel, connectedPanel;
    void Start()
    {
        quickJoinButton.onClick.AddListener(connectionManager.QuickJoinOrCreateSession);
        disconnectButton.onClick.AddListener(connectionManager.Disconnect);
        
        UpdateUI(ConnectionManager.ConnectionState.Disconnected);
        connectionManager.onConnectionEvent.AddListener(UpdateUI);
    }

    public void UpdateUI(ConnectionManager.ConnectionState state)
    {
        offlinePanel.SetActive( state == ConnectionManager.ConnectionState.Failed || state == ConnectionManager.ConnectionState.Disconnected);
        connectingPanel.SetActive( state == ConnectionManager.ConnectionState.Connecting ||  state == ConnectionManager.ConnectionState.Disconnecting);
        connectedPanel.SetActive( state == ConnectionManager.ConnectionState.Success);
    }
}
