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
    public string assetBundleName;
    public string versionHash; // Changed from int version
    // ... (rest of the fields remain the same) ...

    // --- New Scalable BM Fields ---
    public bool unlockedByDefault;
    public int priceCoins;
    public int priceGems;
    public int requiredLevel;
    public List<string> tags;
    public string availableUntil;
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
            
            manifestPath = "https://raw.githubusercontent.com/codecanvascanada/djStar/main/ServerMock/SongList.json";
            assetBundleBaseUrl = "https://raw.githubusercontent.com/codecanvascanada/djStar/main/AssetBundles/";

            StartCoroutine(LoadManifestCoroutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Unload all loaded asset bundles to prevent memory leaks
        foreach (var bundle in _loadedBundles.Values)
        {
            if (bundle != null)
            {
                bundle.Unload(true); // Unload all assets and the bundle itself
            }
        }
        _loadedBundles.Clear();
    }

    private IEnumerator LoadManifestCoroutine()
    {
        string urlWithCacheBuster = manifestPath + "?t=" + DateTime.Now.Ticks;
        Debug.Log($"Loading Manifest from: {urlWithCacheBuster}");
        
        using (UnityWebRequest uwr = UnityWebRequest.Get(urlWithCacheBuster))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                string json = uwr.downloadHandler.text;
                manifest = JsonUtility.FromJson<SongManifest>(json);
                IsManifestLoaded = true;
                Debug.Log($"Manifest loaded successfully. Found {manifest.songs.Count} songs.");
            }
            else
            {
                Debug.LogError($"Failed to load manifest from {urlWithCacheBuster}: {uwr.error}");
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
        if (manifest == null || manifest.songs == null) return false;

        SongMetadata metadata = manifest.songs.Find(s => s.id == songId);
        if (metadata == null) return false;

        return _loadedBundles.ContainsKey(metadata.assetBundleName);
    }

    public IEnumerator PrepareSongCoroutine(string songId, Action<SongInfo> onComplete, Action<float> onProgress)
    {
        if (!IsManifestLoaded)
        {
            Debug.LogError("Manifest is not loaded yet.");
            onComplete?.Invoke(null);
            yield break;
        }

        SongMetadata metadata = manifest.songs.Find(s => s.id == songId);
        if (metadata == null)
        {
            Debug.LogError($"Song with id '{songId}' not found in manifest.");
            onComplete?.Invoke(null);
            yield break;
        }

        AssetBundle bundle = null;

        // 1. Check if the bundle is already cached
        if (_loadedBundles.ContainsKey(metadata.assetBundleName))
        {
            bundle = _loadedBundles[metadata.assetBundleName];
            Debug.Log($"AssetBundle '{metadata.assetBundleName}' found in cache.");
            onProgress?.Invoke(1f); // Instantly report 100% for cached bundles
            yield return null; // Wait one frame to allow UI to update
        }
        else
        {
            // 2. If not cached, download it
            string platformName = GetPlatformName();
            string bundleUrl = assetBundleBaseUrl + platformName + "/" + metadata.assetBundleName + "?v=" + metadata.versionHash;
            Debug.Log($"Attempting to download AssetBundle from: {bundleUrl}");

            Hash128 bundleHash = new Hash128();
            if (!string.IsNullOrEmpty(metadata.versionHash))
            {
                bundleHash = Hash128.Parse(metadata.versionHash);
            }
            
            using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl, bundleHash, 0))
            {
                var asyncOp = uwr.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    onProgress?.Invoke(uwr.downloadProgress);
                    yield return null;
                }

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                    if (bundle != null)
                    {
                        _loadedBundles[metadata.assetBundleName] = bundle;
                    }
                    else
                    {
                        Debug.LogError($"Failed to get AssetBundle content from '{bundleUrl}'.");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to download AssetBundle '{metadata.assetBundleName}': {uwr.error}");
                }
            }
        }

        // 3. If we have a valid bundle (from cache or download), load the asset from it
        if (bundle != null)
        {
            SongInfo songInfo = bundle.LoadAsset<SongInfo>(songId);
            if (songInfo != null)
            {
                _preparedSong = songInfo;
                Debug.Log($"Successfully loaded SongInfo '{songId}' from AssetBundle '{metadata.assetBundleName}'.");
                onComplete?.Invoke(songInfo);
            }
            else
            {
                Debug.LogError($"Failed to load SongInfo asset '{songId}' from bundle '{metadata.assetBundleName}'.");
                onComplete?.Invoke(null);
            }
        }
        else
        {
            onComplete?.Invoke(null);
        }
    }

    public SongInfo GetPreparedSong()
    {
        return _preparedSong;
    }
}
