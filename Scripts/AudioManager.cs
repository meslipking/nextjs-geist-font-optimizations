using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;
    public AudioSource ambienceSource;

    [Header("Audio Mixers")]
    public AudioMixer audioMixer;
    public string masterVolumeParam = "MasterVolume";
    public string musicVolumeParam = "MusicVolume";
    public string sfxVolumeParam = "SFXVolume";
    public string uiVolumeParam = "UIVolume";

    [Header("Sound Effects")]
    public AudioClip buttonClickSound;
    public AudioClip summonSound;
    public AudioClip victorySound;
    public AudioClip defeatSound;
    public AudioClip invalidActionSound;

    [System.Serializable]
    public class AnimalSoundSet
    {
        public AnimalType type;
        public AudioClip[] attackSounds;
        public AudioClip[] hitSounds;
        public AudioClip[] deathSounds;
        public AudioClip[] specialAbilitySounds;
    }

    public List<AnimalSoundSet> animalSounds = new List<AnimalSoundSet>();

    [Header("Background Music")]
    public AudioClip mainMenuMusic;
    public AudioClip battleMusic;
    public AudioClip victoryMusic;
    public float musicFadeDuration = 2f;
    public float musicCrossFadeDuration = 1f;

    private Dictionary<string, float> volumeSettings = new Dictionary<string, float>();
    private AudioClip currentMusic;
    private bool isMuted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        // Initialize volume settings
        volumeSettings[masterVolumeParam] = 1f;
        volumeSettings[musicVolumeParam] = 0.7f;
        volumeSettings[sfxVolumeParam] = 0.8f;
        volumeSettings[uiVolumeParam] = 0.5f;

        // Apply initial volume settings
        foreach (var setting in volumeSettings)
        {
            SetVolume(setting.Key, setting.Value);
        }

        // Configure audio sources
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (ambienceSource != null)
        {
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
        }
    }

    #region Volume Control

    public void SetVolume(string parameter, float value)
    {
        value = Mathf.Clamp01(value);
        volumeSettings[parameter] = value;
        
        // Convert to decibels (-80dB to 0dB)
        float decibelValue = value > 0 ? 20f * Mathf.Log10(value) : -80f;
        audioMixer.SetFloat(parameter, decibelValue);
    }

    public float GetVolume(string parameter)
    {
        return volumeSettings.ContainsKey(parameter) ? volumeSettings[parameter] : 1f;
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        audioMixer.SetFloat(masterVolumeParam, isMuted ? -80f : 20f * Mathf.Log10(volumeSettings[masterVolumeParam]));
    }

    #endregion

    #region Music Control

    public void PlayMusic(AudioClip music, bool fadeIn = true)
    {
        if (musicSource == null || music == null) return;

        if (currentMusic == music) return;

        currentMusic = music;
        
        if (fadeIn)
        {
            StartCoroutine(FadeMusicCoroutine(music));
        }
        else
        {
            musicSource.clip = music;
            musicSource.Play();
        }
    }

    private System.Collections.IEnumerator FadeMusicCoroutine(AudioClip newMusic)
    {
        if (musicSource.isPlaying)
        {
            // Fade out current music
            float startVolume = musicSource.volume;
            float timer = 0;

            while (timer < musicFadeDuration)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / musicFadeDuration);
                yield return null;
            }
        }

        // Change and play new music
        musicSource.clip = newMusic;
        musicSource.Play();

        // Fade in new music
        float timer = 0;
        while (timer < musicFadeDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, volumeSettings[musicVolumeParam], timer / musicFadeDuration);
            yield return null;
        }
    }

    public void StopMusic(bool fadeOut = true)
    {
        if (fadeOut)
        {
            StartCoroutine(FadeOutMusicCoroutine());
        }
        else
        {
            musicSource.Stop();
        }
    }

    private System.Collections.IEnumerator FadeOutMusicCoroutine()
    {
        float startVolume = musicSource.volume;
        float timer = 0;

        while (timer < musicFadeDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / musicFadeDuration);
            yield return null;
        }

        musicSource.Stop();
    }

    #endregion

    #region Sound Effects

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale * volumeSettings[sfxVolumeParam]);
        }
    }

    public void PlayUISound(AudioClip clip)
    {
        if (uiSource != null && clip != null)
        {
            uiSource.PlayOneShot(clip, volumeSettings[uiVolumeParam]);
        }
    }

    public void PlayButtonClick()
    {
        PlayUISound(buttonClickSound);
    }

    public void PlaySummonSound()
    {
        PlaySFX(summonSound);
    }

    public void PlayVictorySound()
    {
        PlaySFX(victorySound);
        PlayMusic(victoryMusic);
    }

    public void PlayDefeatSound()
    {
        PlaySFX(defeatSound);
    }

    public void PlayInvalidActionSound()
    {
        PlaySFX(invalidActionSound);
    }

    #endregion

    #region Animal Sounds

    public void PlayAnimalSound(AnimalType type, string soundType, float volumeScale = 1f)
    {
        var soundSet = animalSounds.Find(set => set.type == type);
        if (soundSet == null) return;

        AudioClip[] clips = null;
        switch (soundType.ToLower())
        {
            case "attack":
                clips = soundSet.attackSounds;
                break;
            case "hit":
                clips = soundSet.hitSounds;
                break;
            case "death":
                clips = soundSet.deathSounds;
                break;
            case "special":
                clips = soundSet.specialAbilitySounds;
                break;
        }

        if (clips != null && clips.Length > 0)
        {
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            PlaySFX(randomClip, volumeScale);
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
