using UnityEngine;

public sealed class TacticalAudioFeedback : MonoBehaviour
{
    public static TacticalAudioFeedback Instance { get; private set; }

    [SerializeField] private AudioClip[] gunshots;
    [SerializeField] private AudioClip hitConfirm;
    [SerializeField] private AudioClip damageTaken;
    [SerializeField] private AudioClip countdownTick;
    [SerializeField] private AudioClip roundStart;
    [SerializeField] private AudioClip roundWin;

    private AudioSource uiSource;

    private void Awake()
    {
        Instance = this;
        uiSource = GetComponent<AudioSource>();
        if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.spatialBlend = 0f;
    }

    public void PlayHitConfirm() => PlayUI(hitConfirm, 0.65f);
    public void PlayDamageTaken() => PlayUI(damageTaken, 0.75f);
    public void PlayCountdownTick() => PlayUI(countdownTick, 0.7f);
    public void PlayRoundStart() => PlayUI(roundStart, 0.8f);
    public void PlayRoundWin() => PlayUI(roundWin, 0.9f);

    public void PlayGunshot(Vector3 position)
    {
        if (gunshots == null || gunshots.Length == 0) return;
        AudioClip clip = gunshots[Random.Range(0, gunshots.Length)];
        if (clip == null) return;
        GameObject sourceObject = new("Gunshot Audio");
        sourceObject.transform.position = position;
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = 1.5f;
        source.maxDistance = 45f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.volume = 0.75f;
        source.Play();
        Destroy(sourceObject, clip.length + 0.1f);
    }

    private void PlayUI(AudioClip clip, float volume)
    {
        if (clip != null && uiSource != null)
            uiSource.PlayOneShot(clip, volume);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
