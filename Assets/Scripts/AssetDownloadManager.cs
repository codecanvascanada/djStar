using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Playables;

// The Manifest classes are still needed to read SongList.json
[System.Serializable]
public class SongManifest
{
    public List<SongMetadata> songs;
}

[System.Serializable]
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
    private static bool s_isInitializing = false; // Static flag to ensure init only runs once

    public SongManifest manifest;
    public bool IsReady { get; private set; } = false;

    private SongInfo _preparedSong;

    // Keep track of loaded handles to release them later
    private AsyncOperationHandle<SongInfo> _songInfoHandle;
    private AsyncOperationHandle<AudioClip> _audioClipHandle;
    private AsyncOperationHandle<PlayableAsset> _timelineHandle;
    private AsyncOperationHandle<TextAsset> _manifestHandle; // Handle for the manifest

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Duplicate AssetDownloadManager found. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Only initialize if it's not ready and not already in the process of initializing.
        if (!IsReady && !s_isInitializing)
        {
            StartCoroutine(InitializeAndLoadManifest());
        }
    }

    private void ReleaseHandles()
    {
        if (_songInfoHandle.IsValid()) Addressables.Release(_songInfoHandle);
        if (_audioClipHandle.IsValid()) Addressables.Release(_audioClipHandle);
        if (_timelineHandle.IsValid()) Addressables.Release(_timelineHandle);
        // Do not release the manifest handle here as we might need it across scenes
    }

    private IEnumerator InitializeAndLoadManifest()
    {
        s_isInitializing = true;
        Debug.Log("Starting Addressables.InitializeAsync().");
        var initHandle = Addressables.InitializeAsync();

        if (!initHandle.IsValid())
        {
            Debug.LogError("Addressables.InitializeAsync() returned an invalid handle immediately.");
            s_isInitializing = false;
            yield break;
        }

        yield return initHandle;

        if (!initHandle.IsValid())
        {
            Debug.LogError("initHandle became invalid after completion.");
            s_isInitializing = false;
            yield break;
        }

        if (initHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Addressables failed to initialize. Exception: {initHandle.OperationException}");
            s_isInitializing = false;
            yield break;
        }
        Debug.Log("Addressables initialized successfully.");

        // Load the manifest using Addressables
        _manifestHandle = Addressables.LoadAssetAsync<TextAsset>("SongListJson");
        yield return _manifestHandle;

        if (_manifestHandle.Status == AsyncOperationStatus.Succeeded)
        {
            string json = _manifestHandle.Result.text;
            manifest = JsonUtility.FromJson<SongManifest>(json);
            IsReady = true;
            Debug.Log("[AssetDownloadManager] Song Manifest loaded via Addressables.");
        }
        else
        {
            Debug.LogError($"[AssetDownloadManager] Failed to load song manifest from Addressables: {_manifestHandle.OperationException}");
            manifest = new SongManifest { songs = new List<SongMetadata>() };
        }

        s_isInitializing = false;
    }

    public IEnumerator PrepareSongCoroutine(string songId, System.Action<SongInfo> onComplete)
    {
        Debug.Log($"[GEMINI_DEBUG] ----- Addressables: New Song Preparation Started for ID: '{songId}' -----");
        
        // Release handles for the song assets only
        if (_songInfoHandle.IsValid()) Addressables.Release(_songInfoHandle);
        if (_audioClipHandle.IsValid()) Addressables.Release(_audioClipHandle);
        if (_timelineHandle.IsValid()) Addressables.Release(_timelineHandle);

        if(_preparedSong != null)
        {
            Destroy(_preparedSong);
            _preparedSong = null;
        }
        
        string infoAddress = $"{songId}_info";
        string audioAddress = $"{songId}_audio";
        string timelineAddress = $"{songId}_timeline";

        _songInfoHandle = Addressables.LoadAssetAsync<SongInfo>(infoAddress);
        _audioClipHandle = Addressables.LoadAssetAsync<AudioClip>(audioAddress);
        _timelineHandle = Addressables.LoadAssetAsync<PlayableAsset>(timelineAddress);

        var combinedHandle = Addressables.ResourceManager.CreateGenericGroupOperation(new List<AsyncOperationHandle> { _songInfoHandle, _audioClipHandle, _timelineHandle }, true);
        
        yield return combinedHandle;

        bool success = combinedHandle.Status == AsyncOperationStatus.Succeeded;

        if (success)
        {
            SongInfo loadedSongInfo = _songInfoHandle.Result;
            AudioClip loadedAudioClip = _audioClipHandle.Result;
            PlayableAsset loadedTimeline = _timelineHandle.Result;
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

        if (_manifestHandle.IsValid())
        {
            Addressables.Release(_manifestHandle);
        }
    }
}