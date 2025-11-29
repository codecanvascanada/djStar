using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System; // Added for Action<>

public class StageSelectManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject songButtonPrefab;
    public Transform contentParent;
    public GameObject loadingPanel;
    public Slider progressBar;
    public TextMeshProUGUI progressText;

    [Header("Scene Names")]
    public string gameSceneName = "GameScene";

    private Coroutine _progressAnimationCoroutine;

    void Start()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        StartCoroutine(WaitForManifestAndDisplay());
    }

    private IEnumerator WaitForManifestAndDisplay()
    {
        // Wait until the AssetDownloadManager has been initialized.
        yield return new WaitUntil(() => AssetDownloadManager.instance != null);

        // Then, wait until the manifest has been successfully loaded.
        yield return new WaitUntil(() => AssetDownloadManager.instance.IsManifestLoaded);

        // Now it's safe to display the songs.
        DisplaySongs();
    }

    void DisplaySongs()
    {
        // Get the list of available songs from the manifest via the manager.
        List<SongMetadata> songs = AssetDownloadManager.instance.manifest.songs;

        // Iterate through the song metadata from the manifest.
        foreach (SongMetadata songMeta in songs)
        {
            // Don't display special songs like 'title' or 'calibrationsong' in the stage select list.
            if (songMeta.id == "title" || songMeta.id == "calibrationsong")
            {
                continue;
            }

            // Instantiate a button for every valid song.
            GameObject buttonGO = Instantiate(songButtonPrefab, contentParent);
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = songMeta.id;
            }

            Button button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                // Check if the song is unlocked to determine button state.
                bool isUnlocked = songMeta.unlockedByDefault || (PlayerDataManager.instance != null && PlayerDataManager.instance.IsSongUnlocked(songMeta.id));

                if (isUnlocked)
                {
                    string currentSongId = songMeta.id;
                    button.onClick.AddListener(() => OnSongSelected(currentSongId));
                }
                else
                {
                    // If locked, make the button non-interactable and change its appearance.
                    button.interactable = false;
                    if (buttonText != null)
                    {
                        buttonText.color = Color.gray;
                        buttonText.text += " (Locked)";
                    }
                }
            }
        }

        // After instantiating all buttons, hide the original prefab.
        if (songButtonPrefab != null)
        {
            songButtonPrefab.SetActive(false);
        }
    }

    public void OnSongSelected(string songId)
    {
        Debug.Log($"[GEMINI_DEBUG] OnSongSelected({songId}) called.");
        // --- Unlock Check ---
        SongMetadata songMeta = AssetDownloadManager.instance.manifest.songs.Find(s => s.id == songId);
        if (songMeta == null)
        {
            // if (showDebugLogs) UnityEngine.Debug.LogError(string.Format("Could not find metadata for songId: {0}", songId));
            Debug.LogWarning($"[GEMINI_DEBUG] OnSongSelected: Could not find metadata for songId: {songId}");
            return;
        }

        bool isUnlocked = songMeta.unlockedByDefault || (PlayerDataManager.instance != null && PlayerDataManager.instance.IsSongUnlocked(songMeta.id));

        if (!isUnlocked)
        {
            // if (showDebugLogs) UnityEngine.Debug.Log(string.Format("Song '{0}' is locked. Please purchase it in the shop.", songId));
            // TODO: Here you would show a UI popup to navigate to the shop.
            Debug.LogWarning($"[GEMINI_DEBUG] OnSongSelected: Song '{songId}' is locked.");
            return;
        }
        // --- End Unlock Check ---

        // Check if player has enough coins
        if (CurrencyManager.instance == null)
        {
            // if (showDebugLogs) UnityEngine.Debug.LogError("CurrencyManager not found!");
            Debug.LogError("[GEMINI_DEBUG] OnSongSelected: CurrencyManager not found!");
            return;
        }

        // Assuming 100 coins per play
        // if (!CurrencyManager.instance.SpendCoins(100))
        // {
        //     // if (showDebugLogs) UnityEngine.Debug.Log("Not enough coins to play this song!");
        //     // TODO: Show a UI message to the user
        //     Debug.LogWarning("[GEMINI_DEBUG] OnSongSelected: Not enough coins to play this song!");
        //     return;
        // }
        // TEMPORARILY DISABLED COIN CHECK FOR DEBUGGING
        Debug.Log($"[GEMINI_DEBUG] OnSongSelected: Coin check temporarily disabled. Proceeding with song '{songId}' preparation.");
        
        // if (showDebugLogs) UnityEngine.Debug.Log(string.Format("Song '{0}' selected. Preparing assets...", songId));
        Debug.Log($"[GEMINI_DEBUG] OnSongSelected: Song '{songId}' selected. Preparing assets...");

        Action<SongInfo> onComplete = (loadedSongInfo) => {
            if (_progressAnimationCoroutine != null) StopCoroutine(_progressAnimationCoroutine);
            if (loadingPanel != null)
            {
                Debug.Log($"[GEMINI_DEBUG] OnSongSelected.onComplete: Setting loadingPanel.SetActive(false). Current state: {loadingPanel.activeSelf}");
                loadingPanel.SetActive(false);
            }
            if (loadedSongInfo != null)
            {
                Debug.Log($"[GEMINI_DEBUG] OnSongSelected.onComplete: Assets for '{songId}' are ready. Starting game...");
                StartCoroutine(PlaySongAndFade(loadedSongInfo));
            }
            else
            {
                Debug.LogError($"[GEMINI_DEBUG] OnSongSelected.onComplete: Failed to prepare assets for song '{songId}'.");
            }
        };

        // bool isCached = AssetDownloadManager.instance.IsBundleCached(songId); // Not used for flow control anymore
        Debug.Log($"[GEMINI_DEBUG] OnSongSelected: Cache check skipped. Forcing download for {songId}.");

        Debug.Log($"[GEMINI_DEBUG] OnSongSelected: Displaying loading panel and calling PrepareSongCoroutine with progress.");
        if (loadingPanel != null)
        {
            Debug.Log($"[GEMINI_DEBUG] OnSongSelected: Setting loadingPanel.SetActive(true). Current state: {loadingPanel.activeSelf}");
            loadingPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[GEMINI_DEBUG] OnSongSelected: loadingPanel is NULL! Cannot display progress.");
        }
        if (progressBar != null) progressBar.value = 0;
        if (progressText != null) progressText.text = "0%";

        Action<float> onProgress = (progress) => {
            if (_progressAnimationCoroutine != null) StopCoroutine(_progressAnimationCoroutine);
            _progressAnimationCoroutine = StartCoroutine(AnimateProgressBar(progress));
        };

        StartCoroutine(AssetDownloadManager.instance.PrepareSongCoroutine(songId, onComplete, onProgress));
    }
    private IEnumerator AnimateProgressBar(float targetProgress)
    {
        if (progressBar == null) yield break;

        float currentProgress = progressBar.value;
        float timer = 0f;
        float duration = 0.2f; // A short duration to smooth the animation

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float animatedProgress = Mathf.Lerp(currentProgress, targetProgress, timer / duration);
            progressBar.value = animatedProgress;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(animatedProgress * 100)}%";
            yield return null;
        }
        progressBar.value = targetProgress;
        if (progressText != null) progressText.text = $"{Mathf.RoundToInt(targetProgress * 100)}%";
        _progressAnimationCoroutine = null;
    }

    private IEnumerator PlaySongAndFade(SongInfo selectedSong)
    {
        if (BackgroundMusic.instance != null)
        {
            float fadeDuration = 0.5f;
            yield return BackgroundMusic.instance.FadeOutMusic(fadeDuration);
        }

        GameData.SelectedSongInfo = selectedSong; 
        SceneManager.LoadScene(gameSceneName);
    }
}
