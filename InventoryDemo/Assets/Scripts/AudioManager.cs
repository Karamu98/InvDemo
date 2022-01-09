using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Super simple manager for test purposes
public class AudioManager : MonoBehaviour
{
    [SerializeField] private int m_concurrentCount = 10;
    public static AudioManager Instance { get; private set; }


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
            return;
        }

        Destroy(gameObject);
    }

    private void Init()
    {
        m_concurrentSources = new AudioSource[m_concurrentCount];

        for(int i = 0; i < m_concurrentCount; ++i)
        {
            m_concurrentSources[i] = gameObject.AddComponent<AudioSource>();
        }
    }

    public void TryPlaySound(AudioClip clip, float volume = 1.0f)
    {
        foreach(AudioSource src in m_concurrentSources)
        {
            if(!src.isPlaying)
            {
                src.clip = clip;
                src.volume = volume;
                src.Play();
                return;
            }
        }
    }



    private AudioSource[] m_concurrentSources;
}
