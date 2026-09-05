using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public Button clientButton;
    public Button hostButton;
    
    void Start()
    {
        clientButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
        hostButton.onClick.AddListener(() => NetworkManager.Singleton.StartHost());
    }
}
