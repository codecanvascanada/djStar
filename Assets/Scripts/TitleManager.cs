using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;

public class TitleManager : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("The name of the stage selection scene (e.g., StageSelectScene).")]
    public string stageSelectSceneName = "StageSelectScene";
    [Tooltip("The name of the main game scene (e.g., GameScene).")]
    public string gameSceneName = "GameScene";

    private const string InitialCalibrationKey = "HasCompletedInitialCalibration";
    private bool _isStartingGame = false;

    void Start()
    {
        // Initial setup if needed, but no automatic scene transition.
        // The scene transition logic is now in StartGame() triggered by a button.
    }

    private IEnumerator ForceInitialCalibrationCoroutine()
    {
        _isStartingGame = true;

        // 1. Wait for the manifest to be loaded by the singleton manager.
        float timeout = 10f; // 10 second timeout
        float timer = 0f;
        while (!AssetDownloadManager.instance.IsManifestLoaded)
        {
            timer += Time.deltaTime;
            if (timer > timeout)
            {
                UnityEngine.Debug.LogError("Failed to start calibration: Manifest loading timed out.");
                _isStartingGame = false;
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
                GameData.IsInitialCalibration = true;
                GameData.SelectedSongInfo = loadedSongInfo;
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to prepare calibration song assets!");
                _isStartingGame = false;
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Initial calibration song 'calibrationsong' not found in manifest! Loading stage select directly.");
            GameData.IsInitialCalibration = false;
            LoadStageSelect();
        }
    }

    private void LoadStageSelect()
    {
        GameData.IsInitialCalibration = false;
        SceneManager.LoadScene(stageSelectSceneName);
    }

    // This method can be used for a UI button if you want an explicit "Start" button
    // instead of automatically starting on scene load.
    public void StartGame()
    {
        if (_isStartingGame) return;

        // Check if the user has completed the initial calibration before.
        if (PlayerPrefs.GetInt(InitialCalibrationKey, 0) == 0) // Key is "HasCompletedInitialCalibration"
        {
            // First time playing: force calibration.
            StartCoroutine(ForceInitialCalibrationCoroutine());
        }
        else
        {
            // Not the first time: go to stage select.
            _isStartingGame = true; // Set here as well to prevent re-entry during scene load
            LoadStageSelect();
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        // For debugging: Press Shift + R in the Title Scene to reset game data.
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
        {
            ResetGameData();
        }
    }
#endif

    public void QuitGame()
    {
        // This will only work in a built game, not in the Unity Editor.
        Application.Quit();
    }

    public void LoadTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }

    // For debugging: Resets the first-run flag and audio offset
    public void ResetGameData()
    {
        UnityEngine.Debug.Log("ResetGameData() called. Deleting keys...");
        PlayerPrefs.DeleteKey(InitialCalibrationKey);
        PlayerPrefs.DeleteKey("UserAudioOffsetFrames"); // Also clear the audio offset
        PlayerPrefs.Save();
    }
}