using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.Playables;

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
    public bool IsManifestLoaded { get; private set; } = false;
    public bool AreMasterBundlesLoaded { get; private set; } = false;

    private string manifestPath;
    private SongInfo _preparedSong;
    private string _platformName;
    
    private AssetBundle _musicBundle;
    private AssetBundle _chartsBundle;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            manifestPath = "https://raw.githubusercontent.com/codecanvascanada/djStar/master/Assets/ServerMock/SongList.json";
            _platformName = GetPlatformName();

            StartCoroutine(LoadMasterBundles());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        UnloadAllBundles();
    }

    private IEnumerator LoadMasterBundles()
    {
        // First, load the manifest
        yield return StartCoroutine(LoadSongManifestCoroutine());

        if (!IsManifestLoaded)
        {
            Debug.LogError("[AssetDownloadManager] Cannot load master bundles because song manifest failed to load.");
            yield break;
        }

        int latestVersion = manifest.songs.Max(s => s.version);
        string assetBundleBaseUrl = "https://raw.githubusercontent.com/codecanvascanada/djStar/master/AssetBundles/";

        // Load Music Bundle
        string musicBundleUrl = $"{assetBundleBaseUrl}{_platformName}/songs/music?v={latestVersion}";
        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(musicBundleUrl, (uint)latestVersion, 0))
        {
            Debug.Log($"[AssetDownloadManager] Loading master music bundle from: {musicBundleUrl}");
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                _musicBundle = DownloadHandlerAssetBundle.GetContent(uwr);
                Debug.Log("[AssetDownloadManager] Master music bundle loaded.");
            }
            else
            {
                Debug.LogError($"[AssetDownloadManager] Failed to download master music bundle: {uwr.error}");
            }
        }

        // Load Charts Bundle
        string chartsBundleUrl = $"{assetBundleBaseUrl}{_platformName}/songs/charts?v={latestVersion}";
        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(chartsBundleUrl, (uint)latestVersion, 0))
        {
            Debug.Log($"[AssetDownloadManager] Loading master charts bundle from: {chartsBundleUrl}");
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                _chartsBundle = DownloadHandlerAssetBundle.GetContent(uwr);
                Debug.Log("[AssetDownloadManager] Master charts bundle loaded.");
            }
            else
            {
                Debug.LogError($"[AssetDownloadManager] Failed to download master charts bundle: {uwr.error}");
            }
        }

        if(_musicBundle != null && _chartsBundle != null)
        {
            AreMasterBundlesLoaded = true;
        }
    }

    private IEnumerator LoadSongManifestCoroutine()
    {
        string urlWithCacheBuster = manifestPath + "?t=" + DateTime.Now.Ticks;
        using (UnityWebRequest uwr = UnityWebRequest.Get(urlWithCacheBuster))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                string json = uwr.downloadHandler.text;
                manifest = JsonUtility.FromJson<SongManifest>(json);
                IsManifestLoaded = true;
                Debug.Log("[AssetDownloadManager] Song Manifest loaded.");
            }
            else
            {
                Debug.LogError($"[AssetDownloadManager] Failed to load song manifest from {urlWithCacheBuster}: {uwr.error}");
                manifest = new SongManifest { songs = new List<SongMetadata>() };
            }
        }
    }
    
    private string GetPlatformName()
    {
        #if UNITY_ANDROID
                return "Android";
        #elif UNITY_IOS
                return "iOS";
        #elif UNITY_WEBGL
                return "WebGL";
        #else
                return "StandaloneOSX";
        #endif
    }

    public IEnumerator PrepareSongCoroutine(string songId, Action<SongInfo> onComplete)
    {
        Debug.Log($"[GEMINI_DEBUG] ----- New Song Preparation Started for ID: '{songId}' -----");
        _preparedSong = null;

        if (_chartsBundle == null || _musicBundle == null)
        {
            Debug.LogError("[AssetDownloadManager] Master bundles are not loaded yet. Cannot prepare song.");
            onComplete?.Invoke(null);
            yield break;
        }

        // Load assets by name from the already loaded master bundles.
        SongInfo songInfo = _chartsBundle.LoadAsset<SongInfo>(songId);
        AudioClip audioClip = _musicBundle.LoadAsset<AudioClip>(songId);
        PlayableAsset timeline = _chartsBundle.LoadAsset<PlayableAsset>(songId);
        
        if (songInfo != null && audioClip != null && timeline != null)
        {
            SongInfo runtimeSongInfo = Instantiate(songInfo);
            runtimeSongInfo.name = songInfo.name + " (Runtime)";
            runtimeSongInfo.songAudioClip = audioClip;
            runtimeSongInfo.songPlayableAsset = timeline;
            
            _preparedSong = runtimeSongInfo;
            Debug.Log($"[AssetDownloadManager] Successfully prepared assets for '{songId}'.");
            onComplete?.Invoke(_preparedSong);
        }
        else
        {
            if (songInfo == null) Debug.LogError($"[AssetDownloadManager] Failed to find SongInfo asset named '{songId}' in charts bundle.");
            if (audioClip == null) Debug.LogError($"[AssetDownloadManager] Failed to find AudioClip asset named '{songId}' in music bundle.");
            if (timeline == null) Debug.LogError($"[AssetDownloadManager] Failed to find PlayableAsset named '{songId}' in charts bundle.");
            onComplete?.Invoke(null);
        }

        yield return null;
    }
    
    public void UnloadAllBundles()
    {
        Debug.Log("[GEMINI_DEBUG] UnloadAllBundles() called.");
        if (_musicBundle != null)
        {
            _musicBundle.Unload(true);
            _musicBundle = null;
        }
        if (_chartsBundle != null)
        {
            _chartsBundle.Unload(true);
            _chartsBundle = null;
        }
        AreMasterBundlesLoaded = false;
        Debug.Log("[GEMINI_DEBUG] Master bundles unloaded.");
    }

    public SongInfo GetPreparedSong()
    {
        return _preparedSong;
    }
}