using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理背景音乐与音效。
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject managerObject = new GameObject(nameof(AudioManager));
                instance = managerObject.AddComponent<AudioManager>();
            }

            return instance;
        }
    }
    /// <summary>可通过名称查找并播放的音频列表。</summary>
    [SerializeField]
    private List<AudioInfo> audioInfos = new List<AudioInfo>();
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = CreateAudioSource("MusicAudioSource", transform);
        musicSource.loop = true;

        sfxSource = CreateAudioSource("SfxAudioSource", transform);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private static AudioSource CreateAudioSource(string objectName, Transform parent)
    {
        GameObject sourceObject = new GameObject(objectName);
        sourceObject.transform.SetParent(parent, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        return source;
    }

    /// <summary>根据名称播放背景音乐；同一首音乐正在播放时不会重新开始。</summary>
    public void PlayMusic(string audioName)
    {
        AudioClip clip = FindAudioClip(audioName);
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying))
            return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    /// <summary>停止背景音乐。</summary>
    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    /// <summary>根据名称播放音效，可通过 volumeScale 调整单次播放音量。</summary>
    public void PlaySfx(string audioName, float volumeScale = 1f)
    {
        AudioClip clip = FindAudioClip(audioName);
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * sfxVolume);
    }

    /// <summary>根据名称从音频列表中查找音频片段。</summary>
    private AudioClip FindAudioClip(string audioName)
    {
        foreach (AudioInfo audioInfo in audioInfos)
        {
            if (audioInfo != null && audioInfo.name == audioName)
            {
                return audioInfo.audioClip;
            }
        }

        Debug.LogWarning($"音频列表中未找到名称为“{audioName}”的音频。", this);
        return null;
    }

    /// <summary>设置背景音乐音量。</summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    /// <summary>设置音效音量。</summary>
    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
}