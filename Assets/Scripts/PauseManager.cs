using UnityEngine;
using UnityEngine.SceneManagement;

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
        Resume();
        if (_gameManager != null)
        {
            _gameManager.PrepareToExitScene();
        }
        SceneManager.LoadScene(stageSelectSceneName);
    }

    public void LoadHome()
    {
        Resume();
        if (_gameManager != null)
        {
            _gameManager.PrepareToExitScene();
        }
        SceneManager.LoadScene(titleSceneName);
    }

    public void LoadCalibrationScene()
    {
        Resume();
        if (_gameManager != null)
        {
            _gameManager.PrepareToExitScene();
        }

        var allSongs = Resources.LoadAll<SongInfo>("SongInfos");
        var calibrationSong = System.Linq.Enumerable.FirstOrDefault(allSongs, s => s.isCalibrationSong);

        if (calibrationSong != null)
        {
            GameData.SelectedSongInfo = calibrationSong;
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("PauseManager: Calibration Song not found in Resources/SongInfos!");
        }
    }
}
