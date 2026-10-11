using UnityEngine;

/// <summary>
/// 管理背景音乐与音效。
/// 由 Game 创建并持有，不需要挂载为 MonoBehaviour。
/// </summary>
public class AudioManager
{
    private readonly AudioSource musicSource;
    private readonly AudioSource sfxSource;

    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    public AudioManager(Transform parent)
    {
        musicSource = CreateAudioSource("MusicAudioSource", parent);
        musicSource.loop = true;

        sfxSource = CreateAudioSource("SfxAudioSource", parent);
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

    /// <summary>播放背景音乐；再次播放同一首正在播放的音乐时不会重新开始。</summary>
    public void PlayMusic(AudioClip clip)
    {
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

    /// <summary>播放音效，可通过 volumeScale 调整单次播放音量。</summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * sfxVolume);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
}