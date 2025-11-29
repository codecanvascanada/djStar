using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using UnityEngine.Networking;

// Data structures to hold the manifest information
[Serializable]
public class SongManifest
{
    public List<SongMetadata> songs;
}

[Serializable]
public class SongMetadata
{
    public string id;
    public string chartBundleName;
    public string musicBundleName;
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

    private string manifestPath;
    private SongInfo _preparedSong;
    private string assetBundleBaseUrl;
    
    private Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            manifestPath = "https://raw.githubusercontent.com/codecanvascanada/djStar/master/Assets/ServerMock/SongList.json";
            assetBundleBaseUrl = "https://raw.githubusercontent.com/codecanvascanada/djStar/master/AssetBundles/";

            StartCoroutine(LoadSongManifestCoroutine());
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
            }
            else
            {
                Debug.LogError($"Failed to load song manifest from {urlWithCacheBuster}: {uwr.error}");
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

    public bool IsBundleCached(string songId)
    {
        Debug.Log($"[GEMINI_DEBUG] IsBundleCached({songId}) called.");
        if (manifest == null || manifest.songs == null)
        {
            Debug.Log($"[GEMINI_DEBUG] IsBundleCached: Manifest not loaded or empty. Returning false.");
            return false;
        }
        SongMetadata metadata = manifest.songs.Find(s => s.id == songId);
        if (metadata == null)
        {
            Debug.Log($"[GEMINI_DEBUG] IsBundleCached: Metadata for '{songId}' not found. Returning false.");
            return false;
        }

        bool chartCached = _loadedBundles.ContainsKey(metadata.chartBundleName);
        bool musicCached = _loadedBundles.ContainsKey(metadata.musicBundleName);
        bool result = chartCached && musicCached;

        Debug.Log($"[GEMINI_DEBUG] IsBundleCached: For '{songId}' -> ChartBundle '{metadata.chartBundleName}' cached: {chartCached}, MusicBundle '{metadata.musicBundleName}' cached: {musicCached}. Result: {result}");
        return result;
    }

    public void UnloadAllBundles()
    {
        Debug.Log($"[GEMINI_DEBUG] UnloadAllBundles() called. Current loaded bundles count: {_loadedBundles.Count}");
        int unloadedCount = 0;
        foreach (var bundle in _loadedBundles.Values)
        {
            if (bundle != null)
            {
                bundle.Unload(true);
                unloadedCount++;
            }
        }
        _loadedBundles.Clear();
        Debug.Log($"[GEMINI_DEBUG] UnloadAllBundles: {unloadedCount} bundles unloaded. _loadedBundles cleared. Count: {_loadedBundles.Count}");
    }

    public SongInfo GetPreparedSong()
    {
        return _preparedSong;
    }
}
