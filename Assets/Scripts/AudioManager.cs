using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public struct AudioCue
{
    [SerializeField]
    private AudioClip[] _audioSamples;

    [SerializeField][Range(0.0f, 3.0f)]
    private float _minPitch;
    [SerializeField][Range(0.0f, 3.0f)]
    private float _maxPitch;
    [Space(10)]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float _minVolume;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float _maxVolume;

    public AudioClip GetSample() 
    { 
        return _audioSamples[Random.Range(0, _audioSamples.Length)];
    }

    public float GetPitch()
    {
        return Random.Range(_minPitch, _maxPitch);
    }

    public float GetVolume()
    {
        return Random.Range(_minVolume, _maxVolume);
    }
}

[System.Serializable]
public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioMixer _audioMixer = null;
    [SerializeField]
    private AudioSource _musicSource = null;
    [SerializeField]
    private AudioSource _sfxSource = null;
    [SerializeField]
    private List<AudioSource> _sfxSourcePool = new List<AudioSource>();

    // MUSIC

    public void PlayMusic(AudioClip music, bool isLoop = true)
    {
        if (music != null)
        {
            _musicSource.Stop();
            _musicSource.loop = isLoop;
            _musicSource.volume = 1.0f;
            _musicSource.clip = music;
            _musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if(_musicSource.isPlaying)
        {
            _musicSource.Stop();
        }
    }

    public void PauseMusic(bool isPaused)
    { 
        if (isPaused)
        {
            _musicSource.Pause();
        }
        else
        {
            _musicSource.UnPause();
        }
    }

    public bool IsPlayingMusic()
    {
        return _musicSource.isPlaying;
    }

    public void FadeInMusic(AudioClip music, float time = 0.5f)
    {
        if(_musicSource.isPlaying)
        {
            _musicSource.Stop();
        }

        _musicSource.clip = music;
        _musicSource.volume = 0.0f;
        _musicSource.Play();
        StartCoroutine(FadeMusic(true, time));
    }

    public void FadeOutMusic(float time = 0.5f)
    {
        if (_musicSource.isPlaying)
        {
            StartCoroutine(FadeMusic(false, time));
        }
    }

    private IEnumerator FadeMusic(bool isFadeIn, float time)
    {
        float deltaTime = 0.0f;
        float target = isFadeIn ? 1.0f : 0.0f;
        float current = _musicSource.volume;

        while (deltaTime < time)
        {
            deltaTime += Time.deltaTime;
            _musicSource.volume = Mathf.Lerp(current, target, deltaTime / time);
            yield return null;
        }

        _musicSource.volume = target;
    }
}
