using Unity.Netcode;

public static class NetworkRoleUtility
{
    public static bool HasRole(ulong clientId, PlayerRole requiredRole)
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null || manager.SpawnManager == null)
            return false;

        NetworkObject playerObject =
            manager.SpawnManager.GetPlayerNetworkObject(clientId);

        if (playerObject == null)
            return false;

        VRNetworkPlayer player =
            playerObject.GetComponent<VRNetworkPlayer>();

        return player != null &&
               (player.role.Value & requiredRole) == requiredRole;
    }
}
