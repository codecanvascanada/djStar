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
        Debug.Log("Starting Addressables.InitializeAsync().");
        var initHandle = Addressables.InitializeAsync();
        if (!initHandle.IsValid())
        {
            Debug.LogError("Addressables.InitializeAsync() returned an invalid handle immediately.");
            yield break;
        }
        Debug.Log("Yielding to wait for initHandle completion...");
        yield return initHandle;
        Debug.Log("...initHandle has completed.");
        if (!initHandle.IsValid())
        {
            Debug.LogError("initHandle became invalid after completion.");
            yield break;
        }
        Debug.Log($"initHandle status is: {initHandle.Status}");
        if (initHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Addressables failed to initialize. Exception: {initHandle.OperationException}");
            yield break;
        }
        Debug.Log("Addressables initialized successfully.");

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
        ReleaseHandles();
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
    }
}
