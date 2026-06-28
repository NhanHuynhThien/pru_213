using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("Pool Settings")]
    public int sfxPoolSize = 8;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Range(0f, 1f)] public float voiceVolume = 1f;

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip battleMusicTier1;
    public AudioClip battleMusicTier2;
    public AudioClip battleMusicTier3;
    public AudioClip battleMusicTier4;
    public AudioClip bossMusic;

    [Header("SFX Clips")]
    public AudioClip attackSFX;
    public AudioClip hitSFX;
    public AudioClip deathSFX;
    public AudioClip pickupSFX;
    public AudioClip upgradeSFX;
    public AudioClip healSFX;
    public AudioClip stepSFX;
    public AudioClip jumpSFX;
    public AudioClip bowDrawSFX;
    public AudioClip arrowReleaseSFX;
    public AudioClip bossSpawnSFX;
    public AudioClip consecrationSFX;
    public AudioClip uiClickSFX;
    public AudioClip uiErrorSFX;

    [Header("Ambient")]
    public AudioClip forestAmbient;
    public AudioClip battleAmbient;
    public AudioClip bossAmbient;

    private AudioClip currentMusic;
    private float musicTimer = 0f;
    private float musicCheckInterval = 2f;

    private Queue<AudioSource> sfxPool = new();
    private List<AudioSource> activeSFX = new();
    private int poolIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
        InitializeSFXPool();
    }

    void InitializeAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
        }

        ApplyVolumeSettings();
    }

    void InitializeSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.volume = sfxVolume;
            sfxPool.Enqueue(src);
        }
    }

    void Update()
    {
        musicTimer += Time.deltaTime;
        if (musicTimer >= musicCheckInterval)
        {
            musicTimer = 0f;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                TryUpdateBattleMusic();
            }
        }

        CleanupFinishedSFX();
    }

    void CleanupFinishedSFX()
    {
        for (int i = activeSFX.Count - 1; i >= 0; i--)
        {
            if (activeSFX[i] == null)
            {
                activeSFX.RemoveAt(i);
            }
            else if (!activeSFX[i].isPlaying)
            {
                activeSFX[i].clip = null;
                sfxPool.Enqueue(activeSFX[i]);
                activeSFX.RemoveAt(i);
            }
        }
    }

    public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
    {
        if (clip == null || clip == currentMusic) return;

        StartCoroutine(FadeMusic(clip, fadeDuration));
    }

    System.Collections.IEnumerator FadeMusic(AudioClip newClip, float duration)
    {
        float elapsed = 0f;
        float startVol = musicSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.clip = newClip;
        musicSource.Play();
        currentMusic = newClip;

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / duration);
            yield return null;
        }
    }

    public void PlaySFX(AudioClip clip, float volMult = 1f, float pitchVariance = 0.05f)
    {
        if (clip == null) return;

        if (sfxPool.Count == 0)
        {
            AudioSource emergencySrc = gameObject.AddComponent<AudioSource>();
            emergencySrc.playOnAwake = false;
            emergencySrc.clip = clip;
            emergencySrc.volume = sfxVolume * volMult;
            emergencySrc.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            emergencySrc.Play();
            StartCoroutine(CleanupEmergencySource(emergencySrc, clip.length));
            return;
        }

        AudioSource src = sfxPool.Dequeue();
        src.clip = clip;
        src.volume = sfxVolume * volMult;
        src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        src.Play();
        activeSFX.Add(src);
    }

    System.Collections.IEnumerator CleanupEmergencySource(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f);
        if (src != null) Destroy(src);
    }

    public void PlaySFXOneShot(AudioClip clip, float volMult = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volMult);
    }

    public void PlayAttackSound()
    {
        PlaySFX(attackSFX);
    }

    public void PlayHitSound()
    {
        PlaySFX(hitSFX);
    }

    public void PlayDeathSound()
    {
        PlaySFX(deathSFX);
    }

    public void PlayPickupSound()
    {
        PlaySFX(pickupSFX);
    }

    public void PlayUpgradeSound()
    {
        PlaySFX(upgradeSFX);
    }

    public void PlayHealSound()
    {
        PlaySFX(healSFX);
    }

    public void PlayBossSpawnSound()
    {
        PlaySFX(bossSpawnSFX, 1.2f);
    }

    public void PlayConsecrationSound()
    {
        PlaySFX(consecrationSFX, 1.5f);
    }

    public void PlayUIClick()
    {
        PlaySFXOneShot(uiClickSFX);
    }

    void TryUpdateBattleMusic()
    {
        if (GameManager.Instance == null) return;

        int tier = GameManager.Instance.currentBossTier;
        AudioClip targetClip = tier switch
        {
            1 => battleMusicTier1,
            2 => battleMusicTier2,
            3 => battleMusicTier3,
            4 => battleMusicTier4,
            _ => battleMusicTier1
        };

        if (targetClip != null && targetClip != currentMusic)
        {
            PlayMusic(targetClip);
        }
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        foreach (var src in activeSFX)
        {
            if (src != null) src.volume = sfxVolume;
        }
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    public void ApplyVolumeSettings()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (voiceSource != null) voiceSource.volume = voiceVolume;
    }
}
