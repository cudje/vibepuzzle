using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;
    private AudioSource audioSource;

    [SerializeField] private AudioClip defaultBgm;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.25f;
    }

    void Start()
    {
        if (defaultBgm != null)
            Play(defaultBgm);
    }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.Play();
    }
}