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