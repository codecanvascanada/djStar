using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalibrationUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI offsetDisplayText;
    public Button increaseButton;
    public Button decreaseButton;
    public Button restartButton; // Added restart button

    [Header("Settings")]
    public int step = 1; // How much to change the offset per click

    private GameManager _gameManager;

    void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        if (_gameManager == null)
        {
            Debug.LogError("GameManager not found in the scene. CalibrationUI requires GameManager.", this);
            enabled = false;
            return;
        }

        // Load the current offset from global settings and update the UI
        UpdateUI();

        // Add listeners to the buttons
        if (increaseButton != null) increaseButton.onClick.AddListener(IncreaseOffset);
        if (decreaseButton != null) decreaseButton.onClick.AddListener(DecreaseOffset);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartButtonClicked); // Add listener for restart button
    }

    private void UpdateUI()
    {
        if (offsetDisplayText != null)
        {
            // Display the current value from GameSettings
            offsetDisplayText.text = GameSettings.UserAudioOffsetFrames.ToString();
        }
    }

    public void IncreaseOffset()
    {
        // Directly modify the static value in GameSettings without saving yet
        GameSettings.UserAudioOffsetFrames += step;
        UpdateUI();
        _gameManager.RetryInitialCalibration();
    }

    public void DecreaseOffset()
    {
        // Directly modify the static value in GameSettings without saving yet
        GameSettings.UserAudioOffsetFrames -= step;
        UpdateUI();
        _gameManager.RetryInitialCalibration();
    }

    public void OnRestartButtonClicked()
    {
        _gameManager.RetryInitialCalibration();
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (increaseButton != null) increaseButton.onClick.RemoveAllListeners();
        if (decreaseButton != null) decreaseButton.onClick.RemoveAllListeners();
        if (restartButton != null) restartButton.onClick.RemoveAllListeners(); // Remove listener for restart button
    }
}
