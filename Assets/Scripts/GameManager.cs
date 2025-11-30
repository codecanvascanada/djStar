using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.Video;

public class GameManager : MonoBehaviour
{
    [Header("Game Components")]
    public AudioSource musicSource;
    public TextMeshProUGUI judgmentText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI achievementRateText;
    public PlayableDirector director;
    public NoteSpawner noteSpawner;
#if UNITY_EDITOR
    public TimelineRecorder timelineRecorder;
#endif

    [Header("Background")]
    public RawImage backgroundA;
    public RawImage backgroundB;
    public RawImage videoBackground;
    public VideoPlayer videoPlayer;
    public float fadeDuration = 0.5f;
    public float zoomIntensity = 1.05f;
    public float staticBgZoomDuration = 30f;

    [Header("UI Panels")]
    public GameObject calibrationUIPanel;
    public GameObject resultPanel;
    public GameObject calibrationFailedPanel;
    public GameObject pauseButton;

    [Header("Result Screen UI")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI maxComboText;
    public TextMeshProUGUI finalRateText;

    [Header("Game State")]
    public int score = 0;
    public int combo = 0;

    [Header("Scene Names")]
    public string stageSelectSceneName = "StageSelectScene";

    [Header("Sync Settings")]
    [Tooltip("A default song to use for testing when starting the scene directly.")]
    public SongInfo testSong;
    [Tooltip("Temporary audio offset in frames for editor testing. Set to 0 to use GameSettings.")]
    public int editorAudioOffsetFrames = 0;
    public float startDelay = 3.0f;
    public float timelineFPS = 60.0f;
    public float webglSyncOffset = 0.0f;

    private int _totalNotes = 0;
    private int _successfulHits = 0;
    private int _maxCombo = 0;
    private Coroutine _songEndCoroutine;
    private Coroutine _comboManagementCoroutine;
    private float _originalJudgmentFontSize, _originalComboFontSize, _originalAchievementRateFontSize;
    private bool _isImageA_Active = true;
    private bool _isExitingScene = false;
    private static bool _applicationIsQuitting = false;
    private SongInfo _currentSong; // The song currently being played.
    private bool _isFullCombo; // Tracks if the player maintains a full combo


    void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    void Awake()
    {
        _applicationIsQuitting = false; // Reset static flag on awake
        if (director != null)
        {
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
        }
    }

    void Start()
    {
        if (judgmentText != null) _originalJudgmentFontSize = judgmentText.fontSize;
        if (comboText != null) _originalComboFontSize = comboText.fontSize;
        if (achievementRateText != null) _originalAchievementRateFontSize = achievementRateText.fontSize;
        
        ResetGame();
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        }
    }

    public void RetryInitialCalibration()
    {
        if (calibrationFailedPanel != null)
        {
            calibrationFailedPanel.SetActive(false);
        }
        ResetGame();
    }

    // This is the version with the "Aggressive Reset" + "Dummy Play"
    public void ResetGame()
    {
        if (BackgroundMusic.instance != null)
        {
            BackgroundMusic.instance.StopMusic();
        }

        _isExitingScene = false;
        StopAllCoroutines();

        // Aggressively reset Director and AudioSource to a clean state
        if (director != null)
        {
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            director.Stop();
            director.playableAsset = null; 
            director.time = 0;
            director.Evaluate();
        }
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null; 
        }

        score = 0;
        combo = 0;
        _maxCombo = 0;
        _successfulHits = 0;
        _totalNotes = 0;
        _currentSong = null;
        _isFullCombo = true;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (calibrationFailedPanel != null) calibrationFailedPanel.SetActive(false);
        if (judgmentText != null) { judgmentText.gameObject.SetActive(false); }
        if (comboText != null) { comboText.gameObject.SetActive(false); }
        if (achievementRateText != null)
        {
            achievementRateText.gameObject.SetActive(true);
            UpdateAchievementRateUI(false);
        }

        if (noteSpawner != null)
        {
            noteSpawner.InitializeSpawner();
        }

                if (director != null)
                {
                    if (AssetDownloadManager.instance != null) { _currentSong = AssetDownloadManager.instance.GetPreparedSong(); }
                    if (_currentSong == null) { _currentSong = GameData.SelectedSongInfo; }
                                            if (_currentSong == null && testSong != null)
                                            {
                                                // UnityEngine.Debug.Log("No song prepared or in GameData. Using test song for editor playback.");
                                                _currentSong = testSong;
                                            }                    if (_currentSong != null)
                    {
                        if (pauseButton != null) { pauseButton.SetActive(!_currentSong.isCalibrationSong); }
                        if (calibrationUIPanel != null) { calibrationUIPanel.SetActive(_currentSong.isCalibrationSong); }
        
                                                        if (_currentSong.songPlayableAsset != null)
        
                                                        {
        
                                                            director.playableAsset = _currentSong.songPlayableAsset;
        
                                                            // UnityEngine.Debug.Log(string.Format("DIAGNOSTIC: Assigned timeline asset '{0}'. Duration: {1} seconds.", director.playableAsset.name, director.duration));
        
                                                            CountTotalNotes();
        
                                                            UpdateAchievementRateUI(false);
        
                                                        }                        if (musicSource != null)
                        {
                            musicSource.clip = _currentSong.songAudioClip;
                            // DUMMY PLAY to kickstart audio loading
                            musicSource.Play();
                            musicSource.Stop();
                        }
                        HandleBackgroundSetup(_currentSong);
        
                        StartCoroutine(StartSongSequentially());
                    }
                                            else
                                            {
                                                // UnityEngine.Debug.LogWarning("GameManager: No song was prepared to play.");
                                                if (calibrationUIPanel != null) calibrationUIPanel.SetActive(false);
                                                if (backgroundA != null) backgroundA.gameObject.SetActive(false);
                                                if (backgroundB != null) backgroundB.gameObject.SetActive(false);
                                                if (videoBackground != null) videoBackground.gameObject.SetActive(false);
                                            }                }
                                else
                                {
                                    // UnityEngine.Debug.LogError("GameManager: PlayableDirector is not assigned in the Inspector!");
                                }        if (MissionManager.instance != null)
        {
            MissionManager.instance.OnSongPlayed();
        }
    }

    public void PrepareToExitScene()
    {
        _isExitingScene = true;
        if (_songEndCoroutine != null)
        {
            StopCoroutine(_songEndCoroutine);
        }
        
        if (director != null)
        {
            director.Stop();
        }
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        }
    }

    private void HandleBackgroundSetup(SongInfo song)
    {
        if (backgroundA != null) backgroundA.gameObject.SetActive(false);
        if (backgroundB != null) backgroundB.gameObject.SetActive(false);
        if (videoBackground != null) videoBackground.gameObject.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        }

        switch (song.backgroundType)
        {
            case BackgroundType.StaticImage:
                if (backgroundA != null && song.staticBackgroundImage != null)
                {
                    backgroundA.gameObject.SetActive(true);
                    backgroundA.texture = song.staticBackgroundImage.texture;
                    Color c = backgroundA.color; c.a = 1f; backgroundA.color = c;
                    StartCoroutine(AnimateStaticBackground(backgroundA));
                }
                break;

            case BackgroundType.TimedImages:
                if (backgroundA != null && backgroundB != null && song.backgroundImages != null && song.backgroundImages.Count > 0)
                {
                    backgroundA.gameObject.SetActive(true);
                    backgroundB.gameObject.SetActive(true);
                    var sortedImages = song.backgroundImages.OrderBy(img => img.displayTime).ToList();
                    StartCoroutine(UpdateBackgroundImages(sortedImages));
                }
                break;

            case BackgroundType.Video:
                if (videoPlayer != null && videoBackground != null && song.backgroundVideo != null)
                {
                    videoBackground.gameObject.SetActive(true);
                    videoBackground.uvRect = new Rect(0, 0, 1, 1);
                    videoPlayer.clip = song.backgroundVideo;
                    videoPlayer.isLooping = false;
                    videoPlayer.loopPointReached += OnVideoLoopPointReached;
                    videoPlayer.SetDirectAudioMute(0, true);
                    videoPlayer.Play();
                }
                break;
        }
    }

    void OnVideoLoopPointReached(VideoPlayer vp)
    {
        vp.Play();
    }

    private void CountTotalNotes()
    {
        _totalNotes = 0;
        if (director.playableAsset is TimelineAsset timelineAsset)
        {
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is NoteTrack)
                {
                    _totalNotes += track.GetClips().Count();
                }
            }
        }
    }

    private void UpdateAchievementRateUI(bool withAnimation = true)
    {
        if (achievementRateText == null) return;
        float rate = (_totalNotes > 0) ? ((float)_successfulHits / _totalNotes) * 100f : 0f;
        int integerPart = Mathf.FloorToInt(rate);
        int decimalPart = Mathf.RoundToInt((rate - integerPart) * 100);
        string formattedText = $"{integerPart}.<size=75%>{decimalPart:00}</size>%";
        achievementRateText.text = formattedText;
        if (withAnimation)
        {
            StartCoroutine(AnimateAchievementRate());
        }
    }

    public void AddScore(int points)
    {
        score += points;
    }

    public void AddCombo(int amount = 1)
    {
        combo += amount;
        if (combo > _maxCombo)
        {
            _maxCombo = combo;
        }

        if (_comboManagementCoroutine != null)
        {
            StopCoroutine(_comboManagementCoroutine);
        }

        if (combo > 1)
        {
            _comboManagementCoroutine = StartCoroutine(ManageComboVisibilityCoroutine());
        }
        else
        {
            if (comboText != null) comboText.gameObject.SetActive(false);
        }

        if (MissionManager.instance != null)
        {
            MissionManager.instance.OnComboAchieved(combo);
        }
    }

    public void RegisterSuccessfulHit()
    {
        _successfulHits++;
        UpdateAchievementRateUI();
    }

    public void ResetCombo()
    {
        combo = 0;
        
        if (_comboManagementCoroutine != null)
        {
            StopCoroutine(_comboManagementCoroutine);
            _comboManagementCoroutine = null;
        }

        if (comboText != null)
        {
            comboText.gameObject.SetActive(false);
        }

        _isFullCombo = false;
    }

    public void ShowJudgment(string text)
    {
        if (judgmentText == null) return;
        StartCoroutine(ShowJudgmentCoroutine(text));
    }

    void HandleSongFinished()
    {
        if (_applicationIsQuitting || _isExitingScene) return;

        float finalAchievementRate = (_totalNotes > 0) ? ((float)_successfulHits / _totalNotes) * 100f : 0f;

        if (MissionManager.instance != null)
        {
            MissionManager.instance.OnSongCleared(_isFullCombo, finalAchievementRate, _maxCombo);
        }

        if (_currentSong != null && _currentSong.isCalibrationSong)
        {
            if (finalAchievementRate >= 100f)
            {
                PlayerPrefs.SetInt("HasCompletedInitialCalibration", 1);
                PlayerPrefs.Save();
                GameSettings.SaveSettings();
                GameData.IsInitialCalibration = false;
                SceneManager.LoadScene(stageSelectSceneName);
            }
            else
            {
                if (calibrationFailedPanel != null)
                {
                    calibrationFailedPanel.SetActive(true);
                }
            }
            return;
        }
        
        StartCoroutine(ShowResultScreenCoroutine());
    }

            #region Coroutines

            private IEnumerator StartSongSequentially()
            {
        #if UNITY_EDITOR
                if (timelineRecorder != null)
                {
                    Debug.Log($"[GameManager] TimelineRecorder.isRecording = {timelineRecorder.isRecording}");
                }
        #endif
                Debug.Log($"[GameManager] StartSongSequentially started. Countdown: {startDelay}s");
        
                // 1. PREPARATION
                if (musicSource != null && musicSource.clip != null)
                {
                    if (musicSource.clip.loadState != AudioDataLoadState.Loaded)
                    {
                        yield return new WaitUntil(() => musicSource.clip.loadState == AudioDataLoadState.Loaded);
                    }
                }
        
                // 2. FADE IN
                if (FadeController.instance != null)
                {
                    FadeController.instance.FadeIn(2.0f);
                }
        
                // 3. SETTINGS & BINDINGS
                // Get user offset (use editor override if set, otherwise use saved game settings)
                int offsetFrames = editorAudioOffsetFrames != 0 ? editorAudioOffsetFrames : GameSettings.UserAudioOffsetFrames;
                float userOffsetSeconds = (offsetFrames / timelineFPS);
                
                // Bind timeline tracks to the spawner
                if (director.playableAsset is TimelineAsset timelineAsset)
                {
                    if (noteSpawner != null)
                    {
                        foreach (var track in timelineAsset.GetOutputTracks())
                        {
                            if (track is NoteTrack) { director.SetGenericBinding(track, noteSpawner.gameObject); }
                        }
                    }
                }
        
                // 4. PLAY
                if (_currentSong != null && director.playableAsset != null)
                {
                    // Start notes (Timeline) immediately
                    director.time = 0;
                    director.Play();
                    Debug.Log("[GameManager] Timeline (notes) started at time 0.");

                    // Schedule audio with countdown delay AND user offset
                    double finalAudioDelay = startDelay + userOffsetSeconds;
                    if(finalAudioDelay < 0) finalAudioDelay = 0;
                    double audioStartTime = AudioSettings.dspTime + finalAudioDelay;

                    musicSource.PlayScheduled(audioStartTime);
                    Debug.Log($"[GameManager] Audio scheduled with {finalAudioDelay}s delay (Countdown: {startDelay}s, User Offset: {userOffsetSeconds}s).");
                    
                    _songEndCoroutine = StartCoroutine(WaitForSongToEndCoroutine());
                }
            }

        

            private IEnumerator WaitForSongToEndCoroutine()

            {

                // Wait until the music has been scheduled and started playing

                yield return new WaitUntil(() => musicSource.isPlaying);

        

                // Now, wait until the song is almost finished by checking the playback time.

                // This is more robust than checking `isPlaying` because pausing/unpausing

                // will not prematurely trigger the end of the song.

                while (musicSource.time < musicSource.clip.length)

                {

                    yield return null;

                }

        

                // Add a small buffer for any trailing notes or effects to complete.

                yield return new WaitForSeconds(2.0f);

        

                HandleSongFinished();

            }
    private IEnumerator ShowResultScreenCoroutine()
    {
        yield return null;

        if (resultPanel != null)
        {
            if (finalScoreText != null) finalScoreText.text = score.ToString();
            if (maxComboText != null) maxComboText.text = _maxCombo.ToString();
            if (finalRateText != null)
            {
                float rate = (_totalNotes > 0) ? ((float)_successfulHits / _totalNotes) * 100f : 0f;
                int integerPart = Mathf.FloorToInt(rate);
                int decimalPart = Mathf.RoundToInt((rate - integerPart) * 100);
                finalRateText.text = $"{integerPart}.<size=75%>{decimalPart:00}</size>%";
            }
            
            resultPanel.SetActive(true);
        }
    }

    private IEnumerator ShowJudgmentCoroutine(string text)
    {
        if (judgmentText == null) yield break;

        if (text == "Miss")
        {
            judgmentText.gameObject.SetActive(false);
            yield break;
        }

        judgmentText.text = text;
        judgmentText.gameObject.SetActive(true);
        Color startColor = judgmentText.color;
        startColor.a = 1f;
        judgmentText.color = startColor;
        judgmentText.fontSize = _originalJudgmentFontSize;
        
        float durationMultiplier = 1.5f;
        float sustainTime = 0.2f * durationMultiplier;

        yield return new WaitForSeconds(sustainTime);
        
        if (judgmentText != null)
        {
            judgmentText.gameObject.SetActive(false);
            judgmentText.color = startColor;
            judgmentText.fontSize = _originalJudgmentFontSize;
        }
    }

    private IEnumerator ManageComboVisibilityCoroutine()
    {
        if (comboText == null) yield break;
        comboText.text = combo.ToString();
        comboText.gameObject.SetActive(true);
        Color color = comboText.color;
        color.a = 1f;
        comboText.color = color;
        float baseFontSize = _originalComboFontSize;
        float animationDuration = 0.3f;
        float timer = 0f;
        while (timer < animationDuration)
        {
            if (comboText == null) yield break;
            timer += Time.deltaTime;
            float t = timer / animationDuration;
            float currentFontSize;
            float peakFontSize = baseFontSize * 1.5f;
            if (t < 0.33f) currentFontSize = Mathf.Lerp(baseFontSize, peakFontSize, t * 3f);
            else if (t < 0.66f) currentFontSize = Mathf.Lerp(peakFontSize, baseFontSize * 0.9f, (t - 0.33f) * 3f);
            else currentFontSize = Mathf.Lerp(baseFontSize * 0.9f, baseFontSize, (t - 0.66f) * 3f);
            comboText.fontSize = currentFontSize;
            yield return null;
        }
        if (comboText != null) comboText.fontSize = baseFontSize;

        yield return new WaitForSeconds(3f);

        float fadeOutDuration = 0.3f;
        timer = 0f;
        Color startColor = comboText.color;
        while (timer < fadeOutDuration)
        {
            if (comboText == null) yield break;
            timer += Time.deltaTime;
            float t = timer / fadeOutDuration;
            float alpha = Mathf.Lerp(startColor.a, 0f, t);
            comboText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        if (comboText != null)
        {
            comboText.gameObject.SetActive(false);
            comboText.color = startColor;
        }
    }

    private IEnumerator AnimateAchievementRate()
    {
        if (achievementRateText == null) yield break;
        float baseFontSize = _originalAchievementRateFontSize;
        float animationDuration = 0.3f;
        float timer = 0f;
        while (timer < animationDuration)
        {
            if (achievementRateText == null) yield break;
            timer += Time.deltaTime;
            float t = timer / animationDuration;
            float currentFontSize;
            float peakFontSize = baseFontSize * 1.05f;
            if (t < 0.33f) currentFontSize = Mathf.Lerp(baseFontSize, peakFontSize, t * 3f);
            else if (t < 0.66f) currentFontSize = Mathf.Lerp(peakFontSize, baseFontSize * 0.9f, (t - 0.33f) * 3f);
            else currentFontSize = Mathf.Lerp(baseFontSize * 0.9f, baseFontSize, (t - 0.66f) * 3f);
            achievementRateText.fontSize = currentFontSize;
            yield return null;
        }
        if (achievementRateText != null)
        {
            achievementRateText.fontSize = baseFontSize;
        }
    }
    
    private IEnumerator UpdateBackgroundImages(List<TimedImage> images)
    {
        bool currentIsImageAActive = _isImageA_Active;

        Color colorA = backgroundA.color; colorA.a = 1f; backgroundA.color = colorA;
        Color colorB = backgroundB.color; colorB.a = 0f; backgroundB.color = colorB;
        _isImageA_Active = true;
        int nextImageIndex = 0;
        if (nextImageIndex < images.Count && images[nextImageIndex].displayTime <= 0)
        {
            if (images[nextImageIndex] != null && images[nextImageIndex].image != null)
            {
                backgroundA.texture = images[nextImageIndex].image.texture;
                float duration = (nextImageIndex + 1 < images.Count) ? images[nextImageIndex + 1].displayTime - images[nextImageIndex].displayTime : (float)director.duration - images[nextImageIndex].displayTime;
                StartCoroutine(AnimateImageScale(backgroundA, duration));
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Null entry found in background images list for '{_currentSong.songName}' at index {nextImageIndex}.");
            }
            nextImageIndex++;
        }
        else
        {
            colorA.a = 0f;
            backgroundA.color = colorA;
        }
        while (nextImageIndex < images.Count)
        {
            if (director.time >= images[nextImageIndex].displayTime)
            {
                if (images[nextImageIndex] != null && images[nextImageIndex].image != null)
                {
                    float duration = (nextImageIndex + 1 < images.Count) ? images[nextImageIndex + 1].displayTime - images[nextImageIndex].displayTime : (float)director.duration - images[nextImageIndex].displayTime;
                    StartCoroutine(FadeToImage(images[nextImageIndex].image, duration));
                }
                else
                {
                     UnityEngine.Debug.LogWarning($"Null entry found in background images list for '{_currentSong.songName}' at index {nextImageIndex}.");
                }
                nextImageIndex++;
            }
            yield return null;
        }
    }

    private IEnumerator FadeToImage(Sprite newSprite, float duration)
    {
        RawImage activeImage = _isImageA_Active ? backgroundA : backgroundB;
        RawImage inactiveImage = _isImageA_Active ? backgroundB : backgroundA;
        inactiveImage.texture = newSprite.texture;
        inactiveImage.transform.localScale = Vector3.one;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            Color activeColor = activeImage.color; activeColor.a = 1f - alpha; activeImage.color = activeColor;
            Color inactiveColor = inactiveImage.color; inactiveColor.a = alpha; inactiveImage.color = inactiveColor;
            yield return null;
        }
        _isImageA_Active = !_isImageA_Active;
        StartCoroutine(AnimateImageScale(inactiveImage, duration));
    }

    private IEnumerator AnimateImageScale(RawImage targetImage, float duration)
    {
        if (duration <= 0) yield break;
        targetImage.transform.localScale = Vector3.one;
        Vector3 originalScale = Vector3.one;
        Vector3 peakScale = Vector3.one * zoomIntensity;
        float halfDuration = duration / 2f;
        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / halfDuration;
            targetImage.transform.localScale = Vector3.Lerp(originalScale, peakScale, progress);
            yield return null;
        }
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / halfDuration;
            targetImage.transform.localScale = Vector3.Lerp(peakScale, originalScale, progress);
            yield return null;
        }
        targetImage.transform.localScale = originalScale;
    }

    private IEnumerator AnimateStaticBackground(RawImage targetImage)
    {
        if (targetImage == null) yield break;

        Vector3 originalScale = Vector3.one;
        Vector3 peakScale = Vector3.one * zoomIntensity;
        float halfDuration = staticBgZoomDuration / 2f;

        while (true)
        {
            float timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / halfDuration;
                targetImage.transform.localScale = Vector3.Lerp(originalScale, peakScale, progress);
                yield return null;
            }

            timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / halfDuration;
                targetImage.transform.localScale = Vector3.Lerp(peakScale, originalScale, progress);
                yield return null;
            }
        }
    }
    #endregion
}
