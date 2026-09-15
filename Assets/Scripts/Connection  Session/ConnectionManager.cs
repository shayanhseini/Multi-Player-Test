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


    // =========================
    // CONNECTION STATE
    // =========================

    public enum ConnectionState
    {
        Connecting,
        Failed,
        Success,
        Disconnecting,
        Disconnected
    }

    public UnityEvent<ConnectionState> onConnectionEvent;
    public UnityEvent<string> onSessionCodeCreated;


    // =========================
    // SESSION EVENTS
    // =========================

    // وقتی Session با موفقیت ساخته شد
    public UnityEvent onSessionCreated;

    // وقتی وارد Session شدیم
    public UnityEvent onSessionJoined;


    // =========================
    // CREATE SESSION
    // =========================

    public async void CreateSession()
    {
        onConnectionEvent?.Invoke(ConnectionState.Connecting);

        try
        {
            // Authenticate
            bool authenticated = await Authenticate();

            if (!authenticated)
            {
                onConnectionEvent?.Invoke(ConnectionState.Failed);
                return;
            }


            // Session Options
            var options = new SessionOptions
            {
                MaxPlayers = maxPlayers
            }.WithDistributedAuthorityNetwork();


            // Create Session
            _currentSession =
                await MultiplayerService.Instance.CreateSessionAsync(options);


            // Debug Information
            Debug.Log("Session Created!");
            Debug.Log($"Session ID: {_currentSession.Id}");
            Debug.Log($"Session Code: {_currentSession.Code}");


            // Connection Success
            onConnectionEvent?.Invoke(ConnectionState.Success);
            onSessionCodeCreated?.Invoke(_currentSession.Code);


            // Notify UI
            onSessionCreated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);

            onConnectionEvent?.Invoke(ConnectionState.Failed);
        }
    }


    // =========================
    // JOIN SESSION BY CODE
    // =========================

    public async void JoinSession(string sessionCode)
    {
        onConnectionEvent?.Invoke(ConnectionState.Connecting);

        try
        {
            // Authenticate
            bool authenticated = await Authenticate();

            if (!authenticated)
            {
                onConnectionEvent?.Invoke(ConnectionState.Failed);
                return;
            }


            // Join Session By Code
            _currentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);


            // Debug Information
            Debug.Log("Joined Session!");
            Debug.Log($"Session ID: {_currentSession.Id}");
            Debug.Log($"Session Code: {_currentSession.Code}");


            // Connection Success
            onConnectionEvent?.Invoke(ConnectionState.Success);


            // Notify UI
            onSessionJoined?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);

            onConnectionEvent?.Invoke(ConnectionState.Failed);
        }
    }


    // =========================
    // DISCONNECT
    // =========================

    public async void Disconnect()
    {
        if (_currentSession != null)
        {
            onConnectionEvent?.Invoke(ConnectionState.Disconnecting);

            try
            {
                await _currentSession.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            _currentSession = null;

            onConnectionEvent?.Invoke(ConnectionState.Disconnected);
        }
    }


    // =========================
    // AUTHENTICATION
    // =========================

    public async Task<bool> Authenticate()
    {
        try
        {
            // Initialize Unity Services
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();

                await UnityServices.InitializeAsync(options);
            }


            // Anonymous Authentication
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