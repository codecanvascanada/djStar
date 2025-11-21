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
            // Instantiate a button for every song.
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
        // --- Unlock Check ---
        SongMetadata songMeta = AssetDownloadManager.instance.manifest.songs.Find(s => s.id == songId);
        if (songMeta == null)
        {
            Debug.LogError($"Could not find metadata for songId: {songId}");
            return;
        }

        bool isUnlocked = songMeta.unlockedByDefault || (PlayerDataManager.instance != null && PlayerDataManager.instance.IsSongUnlocked(songId));

        if (!isUnlocked)
        {
            Debug.Log($"Song '{songId}' is locked. Please purchase it in the shop.");
            // TODO: Here you would show a UI popup to navigate to the shop.
            return;
        }
        // --- End Unlock Check ---

        // Check if player has enough coins
        if (CurrencyManager.instance == null)
        {
            Debug.LogError("CurrencyManager not found!");
            return;
        }

        // Assuming 100 coins per play
        if (!CurrencyManager.instance.SpendCoins(100))
        {
            Debug.Log("Not enough coins to play this song!");
            // TODO: Show a UI message to the user
            return;
        }

        // Update UI after spending coins
        // This requires a reference to the coin display text in StageSelectManager or a global UI update event
        // For now, we'll just log.
        Debug.Log($"Spent 100 coins. Current balance: {CurrencyManager.instance.GetCoinBalance()}");


        Debug.Log($"Song '{songId}' selected. Preparing assets...");

        Action<SongInfo> onComplete = (loadedSongInfo) => {
            if (_progressAnimationCoroutine != null) StopCoroutine(_progressAnimationCoroutine);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (loadedSongInfo != null)
            {
                Debug.Log($"Assets for '{songId}' are ready. Starting game...");
                StartCoroutine(PlaySongAndFade(loadedSongInfo));
            }
            else
            {
                Debug.LogError($"Failed to prepare assets for song '{songId}'.");
                // If song preparation fails, refund coins? Or show error and keep coins spent?
                // For now, coins are spent.
            }
        };

        if (AssetDownloadManager.instance.IsBundleCached(songId))
        {
            StartCoroutine(AssetDownloadManager.instance.PrepareSongCoroutine(songId, onComplete, null));
        }
        else
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);
            if (progressBar != null) progressBar.value = 0;
            if (progressText != null) progressText.text = "0%";

            Action<float> onProgress = (progress) => {
                if (_progressAnimationCoroutine != null) StopCoroutine(_progressAnimationCoroutine);
                _progressAnimationCoroutine = StartCoroutine(AnimateProgressBar(progress));
            };

            StartCoroutine(AssetDownloadManager.instance.PrepareSongCoroutine(songId, onComplete, onProgress));
        }
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
