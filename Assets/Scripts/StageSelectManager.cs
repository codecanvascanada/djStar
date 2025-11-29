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
            Debug.LogWarning($"[GEMINI_DEBUG] OnSongSelected: Could not find metadata for songId: {songId}");
            return;
        }

        bool isUnlocked = songMeta.unlockedByDefault || (PlayerDataManager.instance != null && PlayerDataManager.instance.IsSongUnlocked(songId));
        if (!isUnlocked)
        {
            Debug.LogWarning($"[GEMINI_DEBUG] OnSongSelected: Song '{songId}' is locked.");
            return;
        }
        
        // --- Coin Check (Temporarily Disabled) ---
        // if (!CurrencyManager.instance.SpendCoins(100))
        // {
        //     Debug.LogWarning("[GEMINI_DEBUG] OnSongSelected: Not enough coins to play this song!");
        //     return;
        // }
        
        Debug.Log($"[GEMINI_DEBUG] OnSongSelected: Preparing assets for '{songId}'...");

        Action<SongInfo> onComplete = (loadedSongInfo) => {
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

        // Since master bundles are pre-loaded, we just prepare the song directly from them.
        StartCoroutine(AssetDownloadManager.instance.PrepareSongCoroutine(songId, onComplete));
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
