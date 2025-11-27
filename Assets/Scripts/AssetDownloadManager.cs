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
        // This method is less relevant now since we unload bundles every time,
        // but it can be kept for potential future UI logic.
        // It will effectively always return false with the current implementation.
        if (manifest == null || manifest.songs == null) return false;
        SongMetadata metadata = manifest.songs.Find(s => s.id == songId);
        if (metadata == null) return false;
        return _loadedBundles.ContainsKey(metadata.chartBundleName) && _loadedBundles.ContainsKey(metadata.musicBundleName);
    }

    private IEnumerator LoadBundleCoroutine(string bundleName, int version)
    {
        if (string.IsNullOrEmpty(bundleName) || _loadedBundles.ContainsKey(bundleName))
        {
            yield break;
        }

        string platformName = GetPlatformName();
        string bundleUrl = assetBundleBaseUrl + platformName + "/" + bundleName + "?v=" + version;

        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl, (uint)version, 0))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                if (bundle != null)
                {
                    _loadedBundles[bundleName] = bundle;
                }
                else
                {
                    Debug.LogError($"[AssetDownloadManager] Failed to get content from bundle '{bundleName}'.");
                }
            }
            else
            {
                // This is the critical error we've been seeing.
                // With the "UnloadAll" strategy, this should no longer happen.
                Debug.LogError($"[AssetDownloadManager] Failed to download bundle '{bundleName}': {uwr.error}");
            }
        }
    }

    public IEnumerator PrepareSongCoroutine(string songId, Action<SongInfo> onComplete, Action<float> onProgress)
    {
        // Step 0: Unload ALL previously loaded bundles to ensure a clean state.
        // This is the most robust way to prevent bundle conflicts given the current asset build.
        UnloadAllBundles(); 
        _preparedSong = null;
        onProgress?.Invoke(0);

        if (!IsManifestLoaded)
        {
            Debug.LogError("[AssetDownloadManager] Manifest is not loaded yet.");
            onComplete?.Invoke(null);
            yield break;
        }

        SongMetadata metadata = manifest.songs.Find(s => s.id == songId);
        if (metadata == null)
        {
            Debug.LogError($"[AssetDownloadManager] Song with id '{songId}' not found in manifest.");
            onComplete?.Invoke(null);
            yield break;
        }

        // Step 1: Load Music Bundle
        yield return StartCoroutine(LoadBundleCoroutine(metadata.musicBundleName, metadata.version));
        if (!_loadedBundles.ContainsKey(metadata.musicBundleName))
        {
            Debug.LogError($"[AssetDownloadManager] Failed to load required music bundle: {metadata.musicBundleName}");
            onComplete?.Invoke(null);
            yield break;
        }
        onProgress?.Invoke(0.5f);

        // Step 2: Load Chart Bundle
        yield return StartCoroutine(LoadBundleCoroutine(metadata.chartBundleName, metadata.version));
        if (!_loadedBundles.ContainsKey(metadata.chartBundleName))
        {
             Debug.LogError($"[AssetDownloadManager] Failed to load required chart bundle: {metadata.chartBundleName}");
            onComplete?.Invoke(null);
            yield break;
        }
        onProgress?.Invoke(1.0f);
        
        // Step 3: Load assets and perform fix-up
        AssetBundle chartBundle = _loadedBundles[metadata.chartBundleName];
        AssetBundle musicBundle = _loadedBundles[metadata.musicBundleName];

        SongInfo songInfo = null;
        AudioClip correctAudioClip = null;

        // Load SongInfo from chart bundle by type, to bypass naming issues
        string[] chartAssetNames = chartBundle.GetAllAssetNames();
        foreach (string assetName in chartAssetNames)
        {
            if (chartBundle.LoadAsset(assetName) is SongInfo loadedAsset)
            {
                songInfo = loadedAsset;
                break;
            }
        }

        // Load AudioClip from music bundle by type
        string[] musicAssetNames = musicBundle.GetAllAssetNames();
        foreach (string assetName in musicAssetNames)
        {
            if (musicBundle.LoadAsset(assetName) is AudioClip clip)
            {
                correctAudioClip = clip;
                break;
            }
        }
        
        // Final validation and fix-up
        if (songInfo != null && correctAudioClip != null)
        {
            // This is the core fix: force-replace the audio clip in the loaded SongInfo
            // with the one from the correct music bundle.
            songInfo.songAudioClip = correctAudioClip;
            Debug.Log($"[AssetDownloadManager] Successfully loaded and corrected assets for '{songId}'. Audio '{correctAudioClip.name}' is now assigned.");
            
            _preparedSong = songInfo;
            onComplete?.Invoke(songInfo);
        }
        else
        {
            if (songInfo == null) Debug.LogError($"[AssetDownloadManager] Failed to find any SongInfo asset in bundle '{metadata.chartBundleName}'.");
            if (correctAudioClip == null) Debug.LogError($"[AssetDownloadManager] Failed to find any AudioClip asset in bundle '{metadata.musicBundleName}'.");
            onComplete?.Invoke(null);
        }
    }
    
    public void UnloadAllBundles()
    {
        foreach (var bundle in _loadedBundles.Values)
        {
            if (bundle != null)
            {
                bundle.Unload(true);
            }
        }
        _loadedBundles.Clear();
    }

    public SongInfo GetPreparedSong()
    {
        return _preparedSong;
    }
}
