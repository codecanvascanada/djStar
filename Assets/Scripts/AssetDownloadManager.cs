using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    // Keep track of loaded handles to release them later
    private AsyncOperationHandle<SongInfo> _songInfoHandle;
    private AsyncOperationHandle<AudioClip> _audioClipHandle;
    private AsyncOperationHandle<PlayableAsset> _timelineHandle;

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
    
    private void ReleaseHandles()
    {
        if (_songInfoHandle.IsValid()) Addressables.Release(_songInfoHandle);
        if (_audioClipHandle.IsValid()) Addressables.Release(_audioClipHandle);
        if (_timelineHandle.IsValid()) Addressables.Release(_timelineHandle);
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
        
        // Release any previously loaded song assets
        ReleaseHandles();
        if(_preparedSong != null)
        {
            Destroy(_preparedSong); // Destroy the instantiated ScriptableObject
            _preparedSong = null;
        }

        // Construct addresses based on the songId
        string infoAddress = $"{songId}_info";
        string audioAddress = $"{songId}_audio";
        string timelineAddress = $"{songId}_timeline";

        // Load all 3 assets in parallel
        _songInfoHandle = Addressables.LoadAssetAsync<SongInfo>(infoAddress);
        _audioClipHandle = Addressables.LoadAssetAsync<AudioClip>(audioAddress);
        _timelineHandle = Addressables.LoadAssetAsync<PlayableAsset>(timelineAddress);

        // Wait for all handles to complete
        var combinedHandle = Addressables.ResourceManager.CreateGenericGroupOperation(
            new List<AsyncOperationHandle> { _songInfoHandle.Handle, _audioClipHandle.Handle, _timelineHandle.Handle }, true);

        yield return combinedHandle;
        
        // Check if all operations were successful
        bool success = combinedHandle.Status == AsyncOperationStatus.Succeeded;

        if (success)
        {
            // All assets loaded, now create the runtime instance
            SongInfo loadedSongInfo = _songInfoHandle.Result;
            AudioClip loadedAudioClip = _audioClipHandle.Result;
            PlayableAsset loadedTimeline = _timelineHandle.Result;

            // Instantiate the ScriptableObject to create a mutable copy for this game session
            _preparedSong = Instantiate(loadedSongInfo);
            _preparedSong.name = loadedSongInfo.name + " (Runtime)";
            
            _preparedSong.songAudioClip = loadedAudioClip;
            _preparedSong.songPlayableAsset = loadedTimeline;
            
            Debug.Log($"[AssetDownloadManager] Successfully prepared assets for '{songId}' using Addressables.");
            onComplete?.Invoke(_preparedSong);
        }
        else
        {
            if (_songInfoHandle.Status != AsyncOperationStatus.Succeeded) Debug.LogError($"Failed to load SongInfo for address: {infoAddress} - {_songInfoHandle.OperationException}");
            if (_audioClipHandle.Status != AsyncOperationStatus.Succeeded) Debug.LogError($"Failed to load AudioClip for address: {audioAddress} - {_audioClipHandle.OperationException}");
            if (_timelineHandle.Status != AsyncOperationStatus.Succeeded) Debug.LogError($"Failed to load PlayableAsset for address: {timelineAddress} - {_timelineHandle.OperationException}");
            
            onComplete?.Invoke(null);
        }

        // Handles are now released in the OnDestroy or at the beginning of the next PrepareSong call
    }
    
    public SongInfo GetPreparedSong()
    {
        return _preparedSong;
    }

    void OnDestroy()
    {
        ReleaseHandles();
        if (_preparedSong != null)
        {
           Destroy(_preparedSong);
        }
    }
}
