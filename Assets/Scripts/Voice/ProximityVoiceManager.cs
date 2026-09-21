using System;
using Unity.Services.Vivox;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;  
#endif

public class ProximityVoiceManager : MonoBehaviour
{
    public ConnectionManager connectionManager;
    
    private bool hasJoinedChannel = false;
    public bool debugEcho = false;

    public int audibleDistance = 32;
    public int conversionalDistance = 7;
    public float audioFadeIntensity = 1;
    
    public static ProximityVoiceManager instance;

    private void Awake()
    {
        instance = this;
    }

    public bool HasJoinedChannel()
    {
        return hasJoinedChannel;
    }

    private void Start()
    {
        connectionManager.onConnectionEvent.AddListener(JoinOrLeaveVoiceChannel);
    }

    public void JoinOrLeaveVoiceChannel( ConnectionManager.ConnectionState state)
    {
        if (state == ConnectionManager.ConnectionState.Success)
        {
            JoinVoice();
        }
        else if (state == ConnectionManager.ConnectionState.Disconnected)
        {
            LeaveVoice();
        }
    }

    public async void JoinVoice()
    {
        bool vivoxSignIn =  false;

        if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
        {
            vivoxSignIn = true;
        }
        else
        {
            try
            {
               await VivoxService.Instance.InitializeAsync();
               await VivoxService.Instance.LoginAsync();
               
               RequestMicrophone();
               vivoxSignIn = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                vivoxSignIn = false;
            }
        }

        string channelId = connectionManager.GetSessionID();

        try
        {
            if (debugEcho)
            {
                await VivoxService.Instance.JoinEchoChannelAsync(channelId, ChatCapability.AudioOnly);
                
            }
            else
            {
                Channel3DProperties properties  = 
                    new Channel3DProperties(audibleDistance,  conversionalDistance, audioFadeIntensity, AudioFadeModel.LinearByDistance);
                await VivoxService.Instance.JoinPositionalChannelAsync(channelId, ChatCapability.AudioOnly, properties);
            }
            
            hasJoinedChannel = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            hasJoinedChannel = false;
        }
        
        
    }

    public void RequestMicrophone()
    {
        #if UNITY_ANDROID
        if(!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
        Permission.RequestUserPermission(Permission.Microphone);
        }
        #endif
    }

    public async void LeaveVoice()
    {
        if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn && hasJoinedChannel)
        {
           await VivoxService.Instance.LeaveAllChannelsAsync(); 
           hasJoinedChannel = false;
        }
    }
}
