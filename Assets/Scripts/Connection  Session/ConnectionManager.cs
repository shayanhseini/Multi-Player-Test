using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

public class ConnectionManager : MonoBehaviour
{
    [Header("LAN")]
    [SerializeField, Min(1)] private int maxPlayers = 12;
    [SerializeField, Min(1)] private ushort port = 7777;
    [SerializeField, Min(1)] private ushort discoveryPort = 47777;
    [SerializeField, Min(0.5f)] private float discoveryTimeout = 3f;
    [SerializeField] private string fallbackHostAddress = "127.0.0.1";

    private const string DiscoveryRequest = "LAN_SHOOTER_DISCOVER_V1";
    private const string DiscoveryResponsePrefix = "LAN_SHOOTER_HOST_V1|";

    private NetworkManager networkManager;
    private UnityTransport transport;
    private bool connectionOperationInProgress;
    private bool intentionalDisconnect;
    private string localAddress = string.Empty;
    private UdpClient hostDiscoverySocket;
    private Coroutine hostDiscoveryRoutine;
    private Coroutine clientDiscoveryRoutine;

    public enum ConnectionState
    {
        Connecting,
        Reconnecting,
        Failed,
        Success,
        Disconnecting,
        Disconnected
    }

    public UnityEvent<ConnectionState> onConnectionEvent;
    public UnityEvent<string> onSessionCodeCreated;
    public UnityEvent<string> onConnectionError;
    public UnityEvent<int, int> onReconnectAttempt;
    public UnityEvent onSessionCreated;
    public UnityEvent onSessionJoined;

    public string GetSessionID()
    {
        return string.IsNullOrWhiteSpace(localAddress)
            ? string.Empty
            : $"lan-{localAddress.Replace('.', '-')}-{port}";
    }

    public string GetLocalAddress() => localAddress;

    private void Awake()
    {
        networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();

        if (networkManager != null)
            transport = networkManager.GetComponent<UnityTransport>();

        localAddress = FindLanIPv4Address();
    }

    private void OnEnable() => RegisterNetworkCallbacks();

    private void OnDisable()
    {
        UnregisterNetworkCallbacks();
        StopLanDiscovery();
    }

    public void CreateSession()
    {
        if (!CanStartConnection())
            return;

        connectionOperationInProgress = true;
        intentionalDisconnect = false;
        onConnectionEvent?.Invoke(ConnectionState.Connecting);

        ConfigureClientServerTopology();
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback = ApproveLanClient;

        string advertisedAddress = IsPrivateOrLoopbackIPv4(localAddress)
            ? localAddress
            : IPAddress.Loopback.ToString();

        // Listen on every local adapter. This supports both real LAN clients and
        // multiple Editor/clone instances connecting through loopback.
        transport.SetConnectionData(
            advertisedAddress,
            port,
            IPAddress.Any.ToString());

        if (!networkManager.StartHost())
        {
            FailConnection($"Could not start LAN host on port {port}.");
            return;
        }

        localAddress = advertisedAddress;
        StartHostDiscovery();
        connectionOperationInProgress = false;
        onSessionCodeCreated?.Invoke($"{localAddress}:{port}");
        onConnectionEvent?.Invoke(ConnectionState.Success);
        onSessionCreated?.Invoke();
        Debug.Log($"LAN host started at {localAddress}:{port}");
    }

    public void JoinSession(string hostAddress)
    {
        if (!CanStartConnection())
            return;

        if (!TryParseLanEndpoint(hostAddress, out string address, out ushort targetPort, out string error))
        {
            FailConnection(error);
            return;
        }

        connectionOperationInProgress = true;
        intentionalDisconnect = false;
        localAddress = address;
        port = targetPort;
        onConnectionEvent?.Invoke(ConnectionState.Connecting);

        ConfigureClientServerTopology();
        // This flag is part of the Netcode connection message format and must
        // match the host. Only the host needs an approval callback.
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback = null;
        string connectionAddress = IsAddressAssignedToThisMachine(address)
            ? IPAddress.Loopback.ToString()
            : address;

        transport.SetConnectionData(connectionAddress, targetPort);
        Debug.Log(
            $"Connecting to LAN host {address}:{targetPort} " +
            $"through {connectionAddress}:{targetPort}");

        if (!networkManager.StartClient())
            FailConnection($"Could not start LAN client for {address}:{targetPort}.");
    }

    public void JoinFirstLanHost()
    {
        if (!CanStartConnection())
            return;

        connectionOperationInProgress = true;
        intentionalDisconnect = false;
        onConnectionEvent?.Invoke(ConnectionState.Connecting);
        clientDiscoveryRoutine = StartCoroutine(DiscoverFirstHost());
    }

    public void Disconnect()
    {
        if (networkManager == null || !networkManager.IsListening)
        {
            onConnectionEvent?.Invoke(ConnectionState.Disconnected);
            return;
        }

        intentionalDisconnect = true;
        connectionOperationInProgress = true;
        StopLanDiscovery();
        onConnectionEvent?.Invoke(ConnectionState.Disconnecting);
        networkManager.Shutdown();
    }

    private bool CanStartConnection()
    {
        if (connectionOperationInProgress)
            return false;

        if (networkManager == null || transport == null)
        {
            FailConnection("NetworkManager or UnityTransport was not found in the scene.");
            return false;
        }

        if (networkManager.IsListening)
        {
            FailConnection("A network connection is already running.");
            return false;
        }

        return true;
    }

    private void ConfigureClientServerTopology()
    {
        networkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.ClientServer;
    }

    private void StartHostDiscovery()
    {
        StopHostDiscovery();

        try
        {
            hostDiscoverySocket = new UdpClient(AddressFamily.InterNetwork);
            hostDiscoverySocket.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);
            hostDiscoverySocket.Client.Bind(
                new IPEndPoint(IPAddress.Any, discoveryPort));
            hostDiscoveryRoutine = StartCoroutine(AnswerDiscoveryRequests());
            Debug.Log($"LAN discovery started on UDP port {discoveryPort}.");
        }
        catch (Exception exception)
        {
            StopHostDiscovery();
            Debug.LogWarning($"Could not start LAN discovery: {exception.Message}");
        }
    }

    private IEnumerator AnswerDiscoveryRequests()
    {
        byte[] response = Encoding.UTF8.GetBytes(
            $"{DiscoveryResponsePrefix}{port}");

        while (hostDiscoverySocket != null &&
               networkManager != null &&
               networkManager.IsHost)
        {
            while (hostDiscoverySocket.Available > 0)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] request = hostDiscoverySocket.Receive(ref sender);
                string message = Encoding.UTF8.GetString(request);

                if (message == DiscoveryRequest)
                    hostDiscoverySocket.Send(response, response.Length, sender);
            }

            yield return null;
        }

        hostDiscoveryRoutine = null;
        hostDiscoverySocket?.Close();
        hostDiscoverySocket = null;
    }

    private IEnumerator DiscoverFirstHost()
    {
        UdpClient discoverySocket = null;

        try
        {
            discoverySocket = new UdpClient(AddressFamily.InterNetwork);
            discoverySocket.EnableBroadcast = true;
            discoverySocket.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            byte[] request = Encoding.UTF8.GetBytes(DiscoveryRequest);
            discoverySocket.Send(
                request,
                request.Length,
                new IPEndPoint(IPAddress.Broadcast, discoveryPort));
        }
        catch (Exception exception)
        {
            discoverySocket?.Close();
            clientDiscoveryRoutine = null;
            FailConnection($"Could not search the LAN: {exception.Message}");
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + discoveryTimeout;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (discoverySocket.Available > 0)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = discoverySocket.Receive(ref sender);
                string message = Encoding.UTF8.GetString(response);

                if (message.StartsWith(DiscoveryResponsePrefix, StringComparison.Ordinal) &&
                    ushort.TryParse(
                        message[DiscoveryResponsePrefix.Length..],
                        out ushort discoveredPort) &&
                    discoveredPort > 0)
                {
                    string endpoint = $"{sender.Address}:{discoveredPort}";
                    discoverySocket.Close();
                    clientDiscoveryRoutine = null;
                    connectionOperationInProgress = false;
                    Debug.Log($"Discovered LAN host at {endpoint}.");
                    JoinSession(endpoint);
                    yield break;
                }
            }

            yield return null;
        }

        discoverySocket.Close();
        clientDiscoveryRoutine = null;
        FailConnection("No LAN host was found. Make sure the host is running on the same network.");
    }

    private void StopLanDiscovery()
    {
        if (clientDiscoveryRoutine != null)
        {
            StopCoroutine(clientDiscoveryRoutine);
            clientDiscoveryRoutine = null;
        }

        StopHostDiscovery();
    }

    private void StopHostDiscovery()
    {
        if (hostDiscoveryRoutine != null)
        {
            StopCoroutine(hostDiscoveryRoutine);
            hostDiscoveryRoutine = null;
        }

        hostDiscoverySocket?.Close();
        hostDiscoverySocket = null;
    }

    private void ApproveLanClient(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        bool hasRoom = networkManager.ConnectedClientsIds.Count < Mathf.Max(1, maxPlayers);
        response.Approved = hasRoom;
        response.CreatePlayerObject = hasRoom;
        response.Pending = false;
        response.Reason = hasRoom ? string.Empty : "The LAN room is full.";
    }

    private void RegisterNetworkCallbacks()
    {
        if (networkManager == null)
            return;

        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        networkManager.OnClientStopped -= OnClientStopped;
        networkManager.OnClientStopped += OnClientStopped;
    }

    private void UnregisterNetworkCallbacks()
    {
        if (networkManager == null)
            return;

        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        networkManager.OnClientStopped -= OnClientStopped;

        if (networkManager.ConnectionApprovalCallback == ApproveLanClient)
            networkManager.ConnectionApprovalCallback = null;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager == null || clientId != networkManager.LocalClientId)
            return;

        connectionOperationInProgress = false;

        if (networkManager.IsHost)
            return;

        onConnectionEvent?.Invoke(ConnectionState.Success);
        onSessionJoined?.Invoke();
        Debug.Log($"Joined LAN host at {localAddress}:{port}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (networkManager == null || clientId != networkManager.LocalClientId || intentionalDisconnect)
            return;

        string reason = string.IsNullOrWhiteSpace(networkManager.DisconnectReason)
            ? "Disconnected from the LAN host."
            : networkManager.DisconnectReason;

        connectionOperationInProgress = false;
        onConnectionError?.Invoke(reason);
        onConnectionEvent?.Invoke(ConnectionState.Failed);
        onConnectionEvent?.Invoke(ConnectionState.Disconnected);
    }

    private void OnClientStopped(bool wasHost)
    {
        connectionOperationInProgress = false;

        if (!intentionalDisconnect)
            return;

        intentionalDisconnect = false;
        onConnectionEvent?.Invoke(ConnectionState.Disconnected);
    }

    private void FailConnection(string message)
    {
        connectionOperationInProgress = false;
        Debug.LogWarning(message);
        onConnectionError?.Invoke(message);
        onConnectionEvent?.Invoke(ConnectionState.Failed);
    }

    private bool TryParseLanEndpoint(
        string input,
        out string address,
        out ushort targetPort,
        out string error)
    {
        address = string.Empty;
        targetPort = port;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Host IP address is empty.";
            return false;
        }

        string value = input.Trim();
        int separatorIndex = value.LastIndexOf(':');
        if (separatorIndex > 0)
        {
            if (!ushort.TryParse(value[(separatorIndex + 1)..], out targetPort) || targetPort == 0)
            {
                error = "The LAN port is invalid.";
                return false;
            }

            value = value[..separatorIndex].Trim();
        }

        if (!IPAddress.TryParse(value, out IPAddress parsedAddress) ||
            parsedAddress.AddressFamily != AddressFamily.InterNetwork ||
            !IsPrivateOrLoopbackIPv4(parsedAddress.ToString()))
        {
            error = "Enter a private LAN IPv4 address such as 192.168.1.10.";
            return false;
        }

        address = parsedAddress.ToString();
        return true;
    }

    private string FindLanIPv4Address()
    {
        try
        {
            string activeAdapterAddress = NetworkInterface.GetAllNetworkInterfaces()
                .Where(adapter =>
                    adapter.OperationalStatus == OperationalStatus.Up &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                    adapter.GetIPProperties().GatewayAddresses.Count > 0)
                .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                .Where(unicast => unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(unicast => unicast.Address.ToString())
                .FirstOrDefault(IsPrivateIPv4);

            if (!string.IsNullOrEmpty(activeAdapterAddress))
                return activeAdapterAddress;

            string privateAddress = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(candidate => candidate.AddressFamily == AddressFamily.InterNetwork)
                .Select(candidate => candidate.ToString())
                .FirstOrDefault(IsPrivateIPv4);

            if (!string.IsNullOrEmpty(privateAddress))
                return privateAddress;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not detect a LAN address: {exception.Message}");
        }

        return fallbackHostAddress;
    }

    private static bool IsPrivateOrLoopbackIPv4(string address)
    {
        return IPAddress.TryParse(address, out IPAddress parsed) &&
               parsed.AddressFamily == AddressFamily.InterNetwork &&
               (IPAddress.IsLoopback(parsed) || IsPrivateIPv4(address));
    }

    private static bool IsPrivateIPv4(string address)
    {
        if (!IPAddress.TryParse(address, out IPAddress parsed) ||
            parsed.AddressFamily != AddressFamily.InterNetwork)
            return false;

        byte[] bytes = parsed.GetAddressBytes();
        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }

    private static bool IsAddressAssignedToThisMachine(string address)
    {
        if (IPAddress.IsLoopback(IPAddress.Parse(address)))
            return true;

        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                .Any(unicast =>
                    unicast.Address.AddressFamily == AddressFamily.InterNetwork &&
                    string.Equals(
                        unicast.Address.ToString(),
                        address,
                        StringComparison.Ordinal));
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Could not compare the host address with local adapters: " +
                exception.Message);
            return false;
        }
    }
}
