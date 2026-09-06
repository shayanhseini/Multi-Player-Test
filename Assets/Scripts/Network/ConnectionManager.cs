using System;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using System.Threading.Tasks;
using UnityEngine.Events;

public class ConnectionManager : MonoBehaviour
{
    public int maxPlayers = 10;
    private ISession _currentSession;
    
    public enum ConnectionState { Connecting, Failed, Success ,Disconnecting, Disconnected }
    public UnityEvent<ConnectionState> onConnectionEvent;
    
    public async void QuickJoinOrCreateSession()
    {
        onConnectionEvent.Invoke(ConnectionState.Connecting);
        try
        {
            bool authenticated = await Authenticate();
            if (!authenticated)
            {
                onConnectionEvent.Invoke(ConnectionState.Failed);
                return;
            }

            var availableSessions = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
            foreach (var session in availableSessions.Sessions)
            {
                if (session.AvailableSlots <= 0)
                    continue;
                
                try
                {
                    _currentSession =  await MultiplayerService.Instance.JoinSessionByIdAsync(session.Id);
                    onConnectionEvent.Invoke(ConnectionState.Success);
                    return;
                }
                catch
                {
                    continue;
                }
            }

            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithDistributedAuthorityNetwork();
            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            onConnectionEvent.Invoke(ConnectionState.Success);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            onConnectionEvent.Invoke(ConnectionState.Failed);
        }
    }

    public async void Disconnect()
    {
        if (_currentSession != null)
        {
            await _currentSession.LeaveAsync();
        }
    }
    
    public async Task<bool> Authenticate()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
}
