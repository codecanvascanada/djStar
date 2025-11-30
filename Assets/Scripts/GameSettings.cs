using UnityEngine;

public static class GameSettings
{
    public static int UserAudioOffsetFrames { get; set; }
    public static int DefaultAudioOffsetFrames = 300; // New static field for default offset

    private const string AudioOffsetKey = "UserAudioOffsetFrames";

    public static void LoadSettings()
    {
        // Load the offset from PlayerPrefs, defaulting to DefaultAudioOffsetFrames if not found.
        UserAudioOffsetFrames = PlayerPrefs.GetInt(AudioOffsetKey, DefaultAudioOffsetFrames);
    }

    // This method no longer takes a parameter. It saves the current value.
    public static void SaveSettings()
    {
        PlayerPrefs.SetInt(AudioOffsetKey, UserAudioOffsetFrames);
        PlayerPrefs.Save();
    }
}