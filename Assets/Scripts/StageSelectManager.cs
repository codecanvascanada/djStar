using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System;

public class StageSelectManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject songButtonPrefab;
    public Transform contentParent;
    public GameObject loadingPanel;

    [Header("Scene Names")]
    public string gameSceneName = "GameScene";

    void Start()
    {
        // Show a loading panel initially, it will be hidden when Addressables are ready
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        StartCoroutine(WaitForSystemReadyAndDisplay());
    }

    private IEnumerator WaitForSystemReadyAndDisplay()
    {
        // Wait until the AssetDownloadManager has been initialized and has loaded the manifest + master bundles
        yield return new WaitUntil(() => AssetDownloadManager.instance != null && AssetDownloadManager.instance.IsReady);

        // Now it's safe to display the songs
        if (loadingPanel != null) loadingPanel.SetActive(false);
        DisplaySongs();
    }

    void DisplaySongs()
    {
        List<SongMetadata> songs = AssetDownloadManager.instance.manifest.songs;

        foreach (SongMetadata songMeta in songs)
        {
            if (songMeta.id == "title" || songMeta.id == "calibrationsong")
            {
                continue;
            }

            GameObject buttonGO = Instantiate(songButtonPrefab, contentParent);
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = songMeta.id;
            }

            Button button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                bool isUnlocked = songMeta.unlockedByDefault || (PlayerDataManager.instance != null && PlayerDataManager.instance.IsSongUnlocked(songMeta.id));
                if (isUnlocked)
                {
                    string currentSongId = songMeta.id;
                    button.onClick.AddListener(() => OnSongSelected(currentSongId));
                }
                else
                {
                    button.interactable = false;
                    if (buttonText != null)
                    {
                        buttonText.color = Color.gray;
                        buttonText.text += " (Locked)";
                    }
                }
            }
        }
        
        if (songButtonPrefab != null)
        {
            songButtonPrefab.SetActive(false);
        }
    }

    public void OnSongSelected(string songId)
    {
        Debug.Log($"[GEMINI_DEBUG] OnSongSelected({songId}) called.");
        
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
        if (loadingPanel != null) loadingPanel.SetActive(true);

        Action<SongInfo> onComplete = (loadedSongInfo) => {
            if (loadingPanel != null) loadingPanel.SetActive(false);
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

        StartCoroutine(AssetDownloadManager.instance.PrepareSongCoroutine(songId, onComplete));
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
