using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Playables;

// The Manifest classes are still needed to read SongList.json
[Serializable]
public class SongManifest
{
    public List<SongMetadata> songs;
}

[Serializable]
public class SongMetadata
{
    public string id;
    public int version;
    public bool unlockedByDefault;
    public int priceCoins;
    public int priceGems;
    public int requiredLevel;
    public List<string> tags;
}

public class AssetDownloadManager : MonoBehaviour
{
    public static AssetDownloadManager instance;

    public SongManifest manifest;
    public bool IsReady { get; private set; } = false;

    private string manifestPath;
    private SongInfo _preparedSong;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            manifestPath = "https://raw.githubusercontent.com/codecanvascanada/djStar/master/Assets/ServerMock/SongList.json";
            
            StartCoroutine(InitializeAndLoadManifest());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator InitializeAndLoadManifest()
    {
        // Initialize Addressables
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
        if (initHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Addressables failed to initialize.");
            yield break;
        }
        Debug.Log("Addressables initialized successfully.");

        // Load the manifest
        string urlWithCacheBuster = manifestPath + "?t=" + DateTime.Now.Ticks;
        using (var uwr = UnityEngine.Networking.UnityWebRequest.Get(urlWithCacheBuster))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string json = uwr.downloadHandler.text;
                manifest = JsonUtility.FromJson<SongManifest>(json);
                IsReady = true;
                Debug.Log("[AssetDownloadManager] Song Manifest loaded and Addressables initialized.");
            }
            else
            {
                Debug.LogError($"[AssetDownloadManager] Failed to load song manifest from {urlWithCacheBuster}: {uwr.error}");
                manifest = new SongManifest { songs = new List<SongMetadata>() };
            }
        }
    }
    
    public IEnumerator PrepareSongCoroutine(string songId, Action<SongInfo> onComplete)
    {
        Debug.Log($"[GEMINI_DEBUG] ----- Addressables: New Song Preparation Started for ID: '{songId}' -----");
        _preparedSong = null;

        // Construct addresses based on the songId
        string infoAddress = $"{songId}_info";
        string audioAddress = $"{songId}_audio";
        string timelineAddress = $"{songId}_timeline";

        // Load all 3 assets in parallel
        var songInfoHandle = Addressables.LoadAssetAsync<SongInfo>(infoAddress);
        var audioClipHandle = Addressables.LoadAssetAsync<AudioClip>(audioAddress);
        var timelineHandle = Addressables.LoadAssetAsync<PlayableAsset>(timelineAddress);

        yield return songInfoHandle;
        yield return audioClipHandle;
        yield return timelineHandle;

        // Check if all operations were successful
        if (songInfoHandle.Status != AsyncOperationStatus.Succeeded ||
            audioClipHandle.Status != AsyncOperationStatus.Succeeded ||
            timelineHandle.Status != AsyncOperationStatus.Succeeded)
        {
            if (songInfoHandle.Status != AsyncOperationStatus.Succeeded) Debug.LogError($"Failed to load SongInfo for address: {infoAddress}");
            if (audioClipHandle.Status != AsyncOperationGStatus.Succeeded) Debug.LogError($"Failed to load AudioClip for address: {audioAddress}");
            if (timelineHandle.Status != AsyncOperationStatus.Succeeded) Debug.LogError($"Failed to load PlayableAsset for address: {timelineAddress}");
            
            onComplete?.Invoke(null);
            yield break;
        }

        // All assets loaded, now create the runtime instance
        SongInfo loadedSongInfo = songInfoHandle.Result;
        AudioClip loadedAudioClip = audioClipHandle.Result;
        PlayableAsset loadedTimeline = timelineHandle.Result;

        SongInfo runtimeSongInfo = Instantiate(loadedSongInfo);
        runtimeSongInfo.name = loadedSongInfo.name + " (Runtime)";
        runtimeSongInfo.songAudioClip = loadedAudioClip;
        runtimeSongInfo.songPlayableAsset = loadedTimeline;
            
        _preparedSong = runtimeSongInfo;
        Debug.Log($"[AssetDownloadManager] Successfully prepared assets for '{songId}' using Addressables.");
        onComplete?.Invoke(_preparedSong);
        
        // Release the handles
        Addressables.Release(songInfoHandle);
        Addressables.Release(audioClipHandle);
        Addressables.Release(timelineHandle);
    }
    
    public SongInfo GetPreparedSong()
    {
        return _preparedSong;
    }
}
