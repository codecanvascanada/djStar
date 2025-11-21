using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class TitleManager : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("The name of the stage selection scene (e.g., StageSelectScene).")]
    public string stageSelectSceneName = "StageSelectScene";
    [Tooltip("The name of the main game scene (e.g., GameScene).")]
    public string gameSceneName = "GameScene";

    private const string InitialCalibrationKey = "HasCompletedInitialCalibration";

    void Start()
    {
        // Initial setup if needed, but no automatic scene transition.
        // The scene transition logic is now in StartGame() triggered by a button.
    }

    private void ForceInitialCalibration()
    {
        // Mark that the initial calibration will be attempted.
        PlayerPrefs.SetInt(InitialCalibrationKey, 1);
        PlayerPrefs.Save();

        // Find the calibration song in Resources.
        var allSongs = Resources.LoadAll<SongInfo>("SongInfos");
        SongInfo calibrationSong = allSongs.FirstOrDefault(s => s.isCalibrationSong);

        if (calibrationSong != null)
        {
            GameData.IsInitialCalibration = true;
            GameData.SelectedSongInfo = calibrationSong;
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // Fallback if no calibration song is found.
            Debug.LogWarning("Initial calibration song not found! Loading stage select directly.");
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
        // Check if the user has completed the initial calibration before.
        if (PlayerPrefs.GetInt(InitialCalibrationKey, 0) == 0)
        {
            // First time playing: force calibration.
            ForceInitialCalibration();
        }
        else
        {
            // Not the first time: go to stage select.
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
        PlayerPrefs.DeleteKey(InitialCalibrationKey);
        PlayerPrefs.DeleteKey("UserAudioOffsetFrames"); // Also clear the audio offset
        PlayerPrefs.Save();
    }
}