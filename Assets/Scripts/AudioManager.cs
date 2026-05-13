using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Effects")]
    public AudioClip diceRollSound;
    public AudioClip coughSound;
    public AudioClip confirmSound;
    /// <summary>Played when player gets &gt;10 taps in Tree Planter minigame (MG.02).</summary>
    public AudioClip birdsChirpingSound;

    [Header("Background Music")]
    public AudioClip backgroundMusicClip;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup SFX audio source
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // Setup music audio source
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // Set volumes from VolumeManager
        if (VolumeManager.Instance != null)
        {
            musicSource.volume = VolumeManager.Instance.GetAdjustedMusicVolume();
        }

        // Start playing background music if assigned
        if (backgroundMusicClip != null)
        {
            musicSource.clip = backgroundMusicClip;
            musicSource.Play();
        }
    }

    // Sound Effect Methods
    public void PlayDiceRoll()
    {
        PlaySound(diceRollSound);
    }

    public void PlayCough()
    {
        PlaySound(coughSound);
    }

    public void PlayConfirm()
    {
        PlaySound(confirmSound);
    }

    /// <summary>Birds chirping — Tree Planter high score (MG.02).</summary>
    public void PlayBirdsChirping()
    {
        PlaySound(birdsChirpingSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxSource != null && VolumeManager.Instance != null)
        {
            sfxSource.PlayOneShot(clip, VolumeManager.Instance.GetAdjustedSfxVolume());
        }
    }

    // Music Control Methods
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }

    public float GetMusicVolume()
    {
        return musicSource != null ? musicSource.volume : 0f;
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PlayMusic()
    {
        if (musicSource != null && !musicSource.isPlaying && backgroundMusicClip != null)
        {
            musicSource.Play();
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void UnpauseMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }
}