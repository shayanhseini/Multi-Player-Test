using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public sealed class LanVoiceChat : MonoBehaviour
{
    private const string UploadMessage = "LAN_VOICE_UP";
    private const string PlaybackMessage = "LAN_VOICE_DOWN";
    private const int SampleRate = 8000;
    private const int ChunkSamples = 160;
    private const int MaxChunkSamples = 320;

    [SerializeField, Min(2f)] private float enemyVoiceDistance = 12f;
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.85f;
    [SerializeField] private bool diagnosticLogs = true;

    private readonly Dictionary<ulong, Speaker> speakers = new();
    private NetworkManager manager;
    private AudioClip microphoneClip;
    private int microphoneReadPosition;
    private float nextNetworkCheck;
    private bool registered;

    private sealed class Speaker
    {
        public AudioSource Source;
        public AudioClip Clip;
        public int WritePosition;
    }

    private IEnumerator Start()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        if (Application.HasUserAuthorization(UserAuthorization.Microphone) &&
            Microphone.devices.Length > 0)
        {
            microphoneClip = Microphone.Start(null, true, 1, SampleRate);
            microphoneReadPosition = 0;
            if (diagnosticLogs)
                Debug.Log($"[LAN Voice] Microphone started: {Microphone.devices[0]}");
        }
        else
        {
            Debug.LogWarning("[LAN Voice] Microphone permission or device is unavailable.");
        }
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextNetworkCheck)
        {
            nextNetworkCheck = Time.unscaledTime + 0.25f;
            EnsureRegistered();
            UpdateSpeakerRouting();
        }

        if (!registered || microphoneClip == null || manager == null ||
            !manager.IsListening)
            return;

        int position = Microphone.GetPosition(null);
        if (position < 0 || position == microphoneReadPosition)
            return;

        int available = position >= microphoneReadPosition
            ? position - microphoneReadPosition
            : microphoneClip.samples - microphoneReadPosition + position;
        while (available >= ChunkSamples)
        {
            float[] samples = new float[ChunkSamples];
            microphoneClip.GetData(samples, microphoneReadPosition);
            microphoneReadPosition = (microphoneReadPosition + ChunkSamples) % microphoneClip.samples;
            available -= ChunkSamples;
            SendVoice(samples);
        }
    }

    private void EnsureRegistered()
    {
        if (registered)
            return;
        manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening)
            return;

        if (manager.IsServer)
            manager.CustomMessagingManager.RegisterNamedMessageHandler(
                UploadMessage, OnVoiceUploaded);
        manager.CustomMessagingManager.RegisterNamedMessageHandler(
            PlaybackMessage, OnVoicePlayback);
        manager.OnClientDisconnectCallback += OnClientDisconnected;
        registered = true;
        if (diagnosticLogs)
            Debug.Log($"[LAN Voice] Network voice registered as " +
                      $"{(manager.IsServer ? "server" : "client")}.");
    }

    private void SendVoice(float[] samples)
    {
        byte[] pcm = EncodePcm16(samples);
        if (manager.IsServer)
        {
            BroadcastVoice(manager.LocalClientId, pcm, samples.Length);
            return;
        }

        using FastBufferWriter writer =
            new(sizeof(int) + pcm.Length, Allocator.Temp);
        writer.WriteValueSafe(samples.Length);
        writer.WriteBytesSafe(pcm);
        manager.CustomMessagingManager.SendNamedMessage(
            UploadMessage,
            NetworkManager.ServerClientId,
            writer,
            NetworkDelivery.UnreliableSequenced);
    }

    private void OnVoiceUploaded(ulong senderClientId, FastBufferReader reader)
    {
        if (!manager.IsServer || senderClientId == NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out int sampleCount);
        if (sampleCount <= 0 || sampleCount > MaxChunkSamples)
            return;
        byte[] pcm = new byte[sampleCount * 2];
        reader.ReadBytesSafe(ref pcm, pcm.Length);
        BroadcastVoice(senderClientId, pcm, sampleCount);
    }

    private void BroadcastVoice(ulong speakerClientId, byte[] pcm, int sampleCount)
    {
        using FastBufferWriter writer =
            new(sizeof(ulong) + sizeof(int) + pcm.Length, Allocator.Temp);
        writer.WriteValueSafe(speakerClientId);
        writer.WriteValueSafe(sampleCount);
        writer.WriteBytesSafe(pcm);
        manager.CustomMessagingManager.SendNamedMessageToAll(
            PlaybackMessage,
            writer,
            NetworkDelivery.UnreliableSequenced);
    }

    private void OnVoicePlayback(ulong senderClientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out ulong speakerClientId);
        reader.ReadValueSafe(out int sampleCount);
        if (speakerClientId == manager.LocalClientId || sampleCount <= 0 ||
            sampleCount > MaxChunkSamples)
            return;

        byte[] pcm = new byte[sampleCount * 2];
        reader.ReadBytesSafe(ref pcm, pcm.Length);
        PlaySamples(speakerClientId, DecodePcm16(pcm, sampleCount));
    }

    private void PlaySamples(ulong clientId, float[] samples)
    {
        VRNetworkPlayer player = FindPlayer(clientId);
        if (player == null)
            return;

        if (!speakers.TryGetValue(clientId, out Speaker speaker))
        {
            GameObject voiceObject = new($"LAN Voice {clientId}");
            voiceObject.transform.SetParent(
                player.head != null ? player.head : player.transform, false);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            AudioClip clip = AudioClip.Create(
                $"LAN Voice Buffer {clientId}", SampleRate * 2, 1, SampleRate, false);
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = voiceVolume;
            source.minDistance = 1f;
            source.maxDistance = enemyVoiceDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            speaker = new Speaker { Source = source, Clip = clip };
            speakers.Add(clientId, speaker);
            if (diagnosticLogs)
                Debug.Log($"[LAN Voice] Created remote speaker for client {clientId}.");
        }

        speaker.Clip.SetData(samples, speaker.WritePosition);
        speaker.WritePosition = (speaker.WritePosition + samples.Length) % speaker.Clip.samples;
        if (!speaker.Source.isPlaying)
        {
            speaker.Source.timeSamples =
                (speaker.WritePosition + speaker.Clip.samples - ChunkSamples * 3) %
                speaker.Clip.samples;
            speaker.Source.Play();
        }
    }

    private void UpdateSpeakerRouting()
    {
        VRNetworkPlayer local = FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);
        foreach ((ulong clientId, Speaker speaker) in speakers)
        {
            VRNetworkPlayer remote = FindPlayer(clientId);
            bool matchPlaying = LanMatchController.Instance != null &&
                                LanMatchController.Instance.Phase.Value == MatchPhase.Playing;
            bool teammate = local != null && remote != null &&
                            local.Team.Value != GameTeam.Waiting &&
                            local.Team.Value == remote.Team.Value;
            bool globalLobbyVoice = !matchPlaying;
            speaker.Source.spatialBlend = globalLobbyVoice || teammate ? 0f : 1f;
            speaker.Source.maxDistance = enemyVoiceDistance;
            speaker.Source.volume = voiceVolume;
        }
    }

    private VRNetworkPlayer FindPlayer(ulong clientId)
    {
        if (manager != null && manager.ConnectedClients.TryGetValue(
                clientId, out NetworkClient client) && client.PlayerObject != null)
            return client.PlayerObject.GetComponent<VRNetworkPlayer>();
        return FindObjectsByType<VRNetworkPlayer>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.OwnerClientId == clientId);
    }

    private static byte[] EncodePcm16(float[] samples)
    {
        byte[] bytes = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            bytes[i * 2] = (byte)value;
            bytes[i * 2 + 1] = (byte)(value >> 8);
        }
        return bytes;
    }

    private static float[] DecodePcm16(byte[] bytes, int sampleCount)
    {
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            short value = (short)(bytes[i * 2] | bytes[i * 2 + 1] << 8);
            samples[i] = value / 32768f;
        }
        return samples;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!speakers.Remove(clientId, out Speaker speaker))
            return;
        if (speaker.Source != null)
            Destroy(speaker.Source.gameObject);
    }

    private void OnDestroy()
    {
        if (microphoneClip != null && Microphone.IsRecording(null))
            Microphone.End(null);
        if (!registered || manager == null || manager.CustomMessagingManager == null)
            return;
        if (manager.IsServer)
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(UploadMessage);
        manager.CustomMessagingManager.UnregisterNamedMessageHandler(PlaybackMessage);
        manager.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}
