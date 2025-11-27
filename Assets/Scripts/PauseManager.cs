using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class PauseManager : MonoBehaviour
{
    public static bool IsPaused = false;

    [Header("UI")]
    [Tooltip("Assign the pause menu panel game object here in the Inspector.")]
    public GameObject pauseMenuUI;

    [Header("Scene Names")]
    public string titleSceneName = "TitleScene";
    public string stageSelectSceneName = "StageSelectScene";
    public string gameSceneName = "GameScene";

    private GameManager _gameManager;
    private bool _isLoadingScene = false; // Prevents double scene loads

    void Start()
    {
        // Find the GameManager in the scene.
        _gameManager = FindObjectOfType<GameManager>();
        if (_gameManager == null)
        {
            // This is not an error in scenes like StageSelect where there is no GameManager
            // Debug.LogError("PauseManager could not find GameManager in the scene!");
        }

        // Ensure the pause menu is hidden at the start and time is running.
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        IsPaused = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;

        if (_gameManager != null)
        {
            if (_gameManager.musicSource != null) _gameManager.musicSource.Pause();
            if (_gameManager.director != null) _gameManager.director.Pause();
            // Pause the video if it's playing
            if (_gameManager.videoPlayer != null && _gameManager.videoPlayer.isPlaying)
            {
                _gameManager.videoPlayer.Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;

        if (_gameManager != null)
        {
            if (_gameManager.musicSource != null) _gameManager.musicSource.UnPause();
            if (_gameManager.director != null) _gameManager.director.Play();
            // Resume the video if it was paused
            if (_gameManager.videoPlayer != null && _gameManager.videoPlayer.isPaused)
            {
                _gameManager.videoPlayer.Play();
            }
        }
    }

    public void Restart()
    {
        Resume(); // Ensure timescale is reset and pause menu is hidden.
        if (_gameManager != null)
        {
            _gameManager.ResetGame(); // Reset the game state without reloading the scene.
        }
        else
        {
            Debug.LogError("GameManager not found for ResetGame on Restart!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Fallback to scene reload if GameManager is missing
        }
    }

    public void LoadStageSelect()
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;
        Resume();
        if (_gameManager != null)
        {
            _gameManager.PrepareToExitScene();
        }
        SceneManager.LoadScene(stageSelectSceneName);
    }

    public void LoadHome()
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;
        Resume();
        if (_gameManager != null)
        {
            _gameManager.PrepareToExitScene();
        }
        SceneManager.LoadScene(titleSceneName);
    }

    public void GoToCalibrationScene()
    {
        if (_isLoadingScene) return;
        _isLoadingScene = true;
        Resume();
        if (_gameManager != null)
        {
            _gameManager.PrepareToExitScene();
        }
        StartCoroutine(LoadCalibrationSceneCoroutine());
    }

    private IEnumerator LoadCalibrationSceneCoroutine()
    {
        // 1. Wait for the manifest to be loaded by the singleton manager.
        float timeout = 10f; // 10 second timeout
        float timer = 0f;
        while (!AssetDownloadManager.instance.IsManifestLoaded)
        {
            timer += Time.deltaTime;
            if (timer > timeout)
            {
                Debug.LogError("Failed to start calibration: Manifest loading timed out.");
                _isLoadingScene = false;
                yield break;
            }
            yield return null;
        }

        // 2. Find the calibration song in the manifest.
        SongMetadata calibrationSongMeta = AssetDownloadManager.instance.manifest.songs.FirstOrDefault(s => s.id == "calibrationsong");

        if (calibrationSongMeta != null)
        {
            // 3. Prepare the song using the AssetDownloadManager.
            bool isSongPrepared = false;
            SongInfo loadedSongInfo = null;

            yield return StartCoroutine(AssetDownloadManager.instance.PrepareSongCoroutine(
                calibrationSongMeta.id,
                (songInfo) => {
                    if (songInfo != null)
                    {
                        loadedSongInfo = songInfo;
                        isSongPrepared = true;
                    }
                },
                null // No progress tracking needed here
            ));

            // 4. If prepared, set GameData and load the scene.
            if (isSongPrepared)
            {
                GameData.SelectedSongInfo = loadedSongInfo;
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogError("Failed to prepare calibration song assets!");
                _isLoadingScene = false;
            }
        }
        else
        {
            Debug.LogError("PauseManager: Calibration Song 'calibrationsong' not found in manifest!");
            _isLoadingScene = false;
        }
    }
}
