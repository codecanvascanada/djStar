using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video; // Required for VideoClip
using System.Collections.Generic;

// Enum to define the type of background to use for a song
public enum BackgroundType
{
    StaticImage,
    TimedImages,
    Video
}

[System.Serializable]
public class TimedImage
{
    public Sprite image;
    [Tooltip("The time in seconds into the song when this image should appear.")]
    public float displayTime;
}

[CreateAssetMenu(fileName = "NewSongInfo", menuName = "RhythmGame/Song Info")]
public class SongInfo : ScriptableObject
{
    public string songName = "New Song";
    public string artistName = "Unknown Artist";
    public PlayableAsset songPlayableAsset;
    public AudioClip songAudioClip;

    [Header("Special Settings")]
    [Tooltip("Check this if this song is used for audio sync calibration.")]
    public bool isCalibrationSong = false;
    
    [Header("Background Settings")]
    public BackgroundType backgroundType = BackgroundType.StaticImage;

    [Tooltip("Used if Background Type is StaticImage.")]
    public Sprite staticBackgroundImage;

    [Tooltip("Used if Background Type is TimedImages.")]
    public List<TimedImage> backgroundImages = new List<TimedImage>();

    [Tooltip("Used if Background Type is Video.")]
    public VideoClip backgroundVideo;
}
