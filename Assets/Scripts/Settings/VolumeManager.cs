using System;
using UnityEngine;

public class VolumeManager
{
    private VolumeManager() { }
    private static VolumeManager instance;

    public static VolumeManager Instance
    {
        get 
        {
            if (instance == null)
            {
                instance = new VolumeManager();

                instance.SetMainVolume(PlayerPrefs.GetFloat(PREF_MAIN_VOLUME, DEFAULT_MAIN_VOLUME));
                instance.SetMusicVolume(PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, DEFAULT_MUSIC_VOLUME));
                instance.SetSfxVolume(PlayerPrefs.GetFloat(PREF_SFX_VOLUME, DEFAULT_SFX_VOLUME));
                instance.SetTTSVolume(PlayerPrefs.GetFloat(PREF_TTS_VOLUME, DEFAULT_TTS_VOLUME));

                PlayerPrefs.SetFloat(PREF_MAIN_VOLUME, instance.mainVolume);
                PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, instance.musicVolume);
                PlayerPrefs.SetFloat(PREF_SFX_VOLUME, instance.sfxVolume);
                PlayerPrefs.SetInt(PREF_MUTE_STATUS, Convert.ToInt32(instance.mute));
                PlayerPrefs.Save();
            }
            return instance;
        }
    }
    private const string PREF_MUSIC_VOLUME = "SET01_BackgroundMusicVolume";
    private const string PREF_SFX_VOLUME = "SET02_SoundEffectVolume";
    private const string PREF_MAIN_VOLUME = "SET03_MainVolume";
    private const string PREF_MUTE_STATUS = "SET03_mute";
    private const string PREF_TTS_VOLUME = "SET03_TTSVolume";

    public const float DEFAULT_MUSIC_VOLUME = 0.35f;
    public const float DEFAULT_MAIN_VOLUME = 1.0f;
    public const float DEFAULT_SFX_VOLUME = 0.35f;
    public const bool DEFAULT_MUTE_STATUS = false;
    public const float DEFAULT_TTS_VOLUME = 1.0f;

    private float mainVolume = DEFAULT_MAIN_VOLUME;
    private float musicVolume = DEFAULT_MUSIC_VOLUME;
    private float sfxVolume = DEFAULT_SFX_VOLUME;
    private float ttsVolume = DEFAULT_TTS_VOLUME;

    private bool mute = DEFAULT_MUTE_STATUS;

    public void SetMainVolume(float v)
    {
        mainVolume = v;

        PlayerPrefs.SetFloat(PREF_MAIN_VOLUME, mainVolume);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(GetAdjustedMusicVolume());
        }
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = v;

        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicVolume);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(GetAdjustedMusicVolume());
        }
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = v;

        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetMuteStatus(bool m)
    {
        mute = m;

        PlayerPrefs.SetInt(PREF_MUTE_STATUS, Convert.ToInt32(instance.mute));
        PlayerPrefs.Save();

        if (mute)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(0.0f);
            }
        } else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(GetAdjustedMusicVolume());
            }
        }
    }

    public bool GetMuteStatus()
    {
        return mute;
    }
    public float GetMainVolume() { 
        if (mute) return 0.0f;
        return mainVolume; 
    }
    public float GetAdjustedMusicVolume() { 
        if (mute) return 0.0f;
        return musicVolume * mainVolume;
    }
    public float GetAdjustedSfxVolume() {
        if (mute) return 0.0f;
        return sfxVolume * mainVolume;
    }
    public float GetRawMusicVolume() {
        if (mute) return 0.0f; 
        return musicVolume; 
    }
    public float GetRawSfxVolume() { 
        if (mute) return 0.0f;
        return sfxVolume; 
    }
    public float GetRawTTSVolume() { 
        if (mute) return 0.0f;
        return ttsVolume;
    }
    public float GetAdjustedTTSVolume() {
        if (mute) return 0.0f;
        return ttsVolume * mainVolume;
    }
    public void SetTTSVolume(float v)
    {
        ttsVolume = v;

        PlayerPrefs.SetFloat(PREF_TTS_VOLUME, ttsVolume);
        PlayerPrefs.Save();
    }
}