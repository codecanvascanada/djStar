using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic instance;

    [Header("Settings")]
    public AudioClip titleBGM;
    public bool playOnStart = true;
    public float fadeInDuration = 1.0f; // New field for fade-in duration

    private AudioSource audioSource;
    private float originalVolume; // To store the volume set in the inspector
    public double musicDspStartTime = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            originalVolume = audioSource.volume; // Store original volume
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "TitleScene" || scene.name == "StageSelectScene")
        {
            PlayMusic(titleBGM);
        }
    }

    void Start()
    {
        if (playOnStart && titleBGM != null)
        {
            PlayMusic(titleBGM);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip && audioSource.isPlaying)
        {
            return; // Don't restart if the same clip is already playing
        }
        
        audioSource.clip = clip;
        audioSource.loop = true; // Ensure music loops
        audioSource.volume = 0f; // Start from 0 volume for fade-in
        audioSource.Play();
        musicDspStartTime = AudioSettings.dspTime;

        // Start fade-in
        StartCoroutine(FadeInCoroutine(fadeInDuration, originalVolume));
    }

    // New FadeInCoroutine
    private IEnumerator FadeInCoroutine(float duration, float targetVolume)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }

    public void StopMusic()
    {
        audioSource.Stop();
        musicDspStartTime = 0;
    }

    public Coroutine FadeOutMusic(float duration)
    {
        return StartCoroutine(FadeOutCoroutine(duration));
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        StopMusic();
    }
}
