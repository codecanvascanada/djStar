using UnityEngine;
using System.Collections.Generic;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager instance;

    // The key prefix for storing song unlock status in PlayerPrefs.
    private const string SongUnlockPrefix = "song_unlocked_";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Checks if a specific song has been unlocked by the player.
    /// </summary>
    /// <param name="songId">The ID of the song to check.</param>
    /// <returns>True if the song is unlocked, false otherwise.</returns>
    public bool IsSongUnlocked(string songId)
    {
        // A value of 1 means unlocked, 0 (default) means locked.
        return PlayerPrefs.GetInt(SongUnlockPrefix + songId, 0) == 1;
    }

    /// <summary>
    /// Marks a specific song as unlocked for the player and saves it.
    /// </summary>
    /// <param name="songId">The ID of the song to unlock.</param>
    public void UnlockSong(string songId)
    {
        PlayerPrefs.SetInt(SongUnlockPrefix + songId, 1);
        PlayerPrefs.Save();
        Debug.Log($"Song '{songId}' has been unlocked and saved.");
    }

    /// <summary>
    /// For debugging: Resets all song unlock statuses.
    /// </summary>
    [ContextMenu("DEBUG - Reset All Song Unlocks")]
    public void ResetAllSongUnlocks()
    {
        // This is a bit brute-force. A better way would be to iterate through known song IDs.
        // For now, this is a simple debug helper. We can improve it if needed.
        // Be careful with this in a production game with many PlayerPrefs keys.
        PlayerPrefs.DeleteKey(SongUnlockPrefix + "cherry");
        PlayerPrefs.DeleteKey(SongUnlockPrefix + "unfold");
        PlayerPrefs.DeleteKey(SongUnlockPrefix + "20minutes"); // Add any other songs here
        PlayerPrefs.Save();
        Debug.Log("DEBUG: All known song unlock statuses have been reset.");
    }
}
