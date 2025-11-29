using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Diagnostics;
using System.Text;

public class SongManager : EditorWindow
{
    #region Data Structures
    private class SongBuildInfo
    {
        public SongMetadata metadata;
        public string status;
        public Color statusColor;

        public int version_edit;
        public bool unlockedByDefault_edit;
        public int priceCoins_edit;
        public int priceGems_edit;
        public int requiredLevel_edit;
        public string tags_edit;

        public bool isDirty = false;

        public SongBuildInfo(SongMetadata source)
        {
            metadata = source;
            version_edit = source.version;
            unlockedByDefault_edit = source.unlockedByDefault;
            priceCoins_edit = source.priceCoins;
            priceGems_edit = source.priceGems;
            requiredLevel_edit = source.requiredLevel;
            tags_edit = source.tags != null ? string.Join(", ", source.tags) : "";
        }

        public void MarkAsDirty()
        {
            isDirty = true;
        }

        public bool HasChanges()
        {
            if (version_edit != metadata.version) return true;
            if (unlockedByDefault_edit != metadata.unlockedByDefault) return true;
            if (priceCoins_edit != metadata.priceCoins) return true;
            if (priceGems_edit != metadata.priceGems) return true;
            if (requiredLevel_edit != metadata.requiredLevel) return true;
            string originalTags = metadata.tags != null ? string.Join(", ", metadata.tags) : "";
            if (tags_edit != originalTags) return true;
            return false;
        }
    }
    #endregion

    #region Private Fields
    private List<SongBuildInfo> _songInfos = new List<SongBuildInfo>();
    private Vector2 _scrollPosition;
    private const string SongListPath = "Assets/ServerMock/SongList.json";
    private const string AssetBundlesOutputPath = "AssetBundles";
    private const string SongsSourceBasePath = "Assets/Songs/";
    private const string FfmpegPath = "/opt/homebrew/bin/ffmpeg";

    private string _songListGitStatus = "Press Refresh...";
    private string _globalBranchStatus = "Press Refresh...";
    private string _commitMessage = "Update song data via SongManager";
    #endregion

    [MenuItem("Tools/Song Manager")]
    public static void ShowWindow()
    {
        GetWindow<SongManager>("Song Manager");
    }

    #region Unity Lifecycle
    private void OnFocus()
    {
        ReloadData();
    }
    #endregion

    #region GUI Drawing
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Song AssetBundle & Metadata Manager", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Manage song metadata, versions, and builds.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Data & Git Status"))
        {
            ReloadData();
        }
        if (GUILayout.Button("Scan for New Songs")) 
        {
            ScanForNewSongs(); 
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("Branch Status: " + _globalBranchStatus, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("SongList.json Status: " + _songListGitStatus, EditorStyles.boldLabel);

        if (AnySongIsDirty())
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save All Changes to SongList.json"))
            {
                SaveChanges();
            }
            GUI.backgroundColor = Color.white;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        EditorGUILayout.BeginVertical("box");

        DrawHeader();
        DrawSongList();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        DrawGlobalActions();
    }

    private bool AnySongIsDirty()
    {
        return _songInfos.Any(s => s.isDirty || s.HasChanges());
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Status", GUILayout.Width(110));
        EditorGUILayout.LabelField("Song ID", EditorStyles.boldLabel, GUILayout.MinWidth(150));
        EditorGUILayout.LabelField("Version", GUILayout.Width(60));
        EditorGUILayout.LabelField("Unlocked", GUILayout.Width(70));
        EditorGUILayout.LabelField("Coin", GUILayout.Width(70));
        EditorGUILayout.LabelField("Gem", GUILayout.Width(70));
        EditorGUILayout.LabelField("Level", GUILayout.Width(50));
        EditorGUILayout.LabelField("Tags", GUILayout.MinWidth(100));
        EditorGUILayout.LabelField("Actions", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSongList()
    {
        foreach (var songInfo in _songInfos)
        {
            EditorGUILayout.BeginHorizontal("box");

            GUI.color = songInfo.statusColor;
            EditorGUILayout.LabelField(new GUIContent(songInfo.status, GetTooltipForStatus(songInfo.status)), GUILayout.Width(110));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(songInfo.metadata.id, GUILayout.MinWidth(150));
            
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField(songInfo.version_edit.ToString(), GUILayout.Width(60));
            
            bool isSynced = songInfo.status.Contains("Synced");
            EditorGUI.BeginDisabledGroup(!isSynced);
            songInfo.unlockedByDefault_edit = EditorGUILayout.Toggle(songInfo.unlockedByDefault_edit, GUILayout.Width(70));
            EditorGUI.EndDisabledGroup();

            songInfo.priceCoins_edit = EditorGUILayout.IntField(songInfo.priceCoins_edit, GUILayout.Width(70));
            songInfo.priceGems_edit = EditorGUILayout.IntField(songInfo.priceGems_edit, GUILayout.Width(70));
            songInfo.requiredLevel_edit = EditorGUILayout.IntField(songInfo.requiredLevel_edit, GUILayout.Width(50));
            
            List<string> currentTags = songInfo.tags_edit.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            string primaryTag = "None";

            if (currentTags.Contains("base")) { primaryTag = "Base"; }
            else if (currentTags.Contains("new")) { primaryTag = "New"; }
            else if (currentTags.Contains("dlc")) { primaryTag = "DLC"; }
            
            Color tagButtonColor = Color.gray;
            if (primaryTag == "Base") tagButtonColor = Color.cyan;
            else if (primaryTag == "New") tagButtonColor = Color.yellow;
            else if (primaryTag == "DLC") tagButtonColor = Color.magenta;

            GUI.backgroundColor = tagButtonColor;
            if (GUILayout.Button(primaryTag, GUILayout.Width(100)))
            {
                List<string> cycleTags = new List<string> { "base", "new", "dlc" };
                int currentIndex = cycleTags.IndexOf(primaryTag.ToLower());
                string nextTag;

                if (currentIndex == -1) { nextTag = cycleTags[0]; }
                else if (currentIndex == cycleTags.Count - 1) { nextTag = "None"; }
                else { nextTag = cycleTags[currentIndex + 1]; }

                currentTags.RemoveAll(t => cycleTags.Contains(t));
                if (nextTag != "None") { currentTags.Add(nextTag); }
                
                songInfo.tags_edit = string.Join(", ", currentTags);
                songInfo.MarkAsDirty();
                Repaint();
            }
            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                songInfo.MarkAsDirty();
            }

            if (GUILayout.Button(new GUIContent("[+", "Increment version number"), GUILayout.Width(30))) { IncrementVersion(songInfo); }
            if (GUILayout.Button("To OGG", GUILayout.Width(60))) { EditorApplication.delayCall += () => ConvertToCompatibleOGG(songInfo); }
            if (GUILayout.Button("Build", GUILayout.Width(60))) { EditorApplication.delayCall += () => BuildSingleSong(songInfo.metadata); }
            
            EditorGUILayout.EndHorizontal();
        }
    }
            
    private void DrawGlobalActions()
    {
        EditorGUILayout.LabelField("Global Actions", EditorStyles.boldLabel);
        if (GUILayout.Button("Build Changed Songs")) { EditorApplication.delayCall += BuildChangedSongs; }
        if (GUILayout.Button("Force Rebuild All Songs"))
        {
            if (EditorUtility.DisplayDialog("Confirm Rebuild",
                "This will delete the entire platform-specific AssetBundles directory and rebuild everything from scratch. Are you sure?",
                "Yes, Rebuild All", "Cancel"))
            {
                EditorApplication.delayCall += () => BuildAllSongs(true);
            }
        }
        if (GUILayout.Button("Open Build Folder")) { OpenBuildFolder(); }
        if (GUILayout.Button("Open SongList.json Folder")) { OpenSongListJsonFolder(); }
        if (GUILayout.Button("Scan for New Songs")) { ScanForNewSongs(); }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Git Actions", EditorStyles.boldLabel);
        _commitMessage = EditorGUILayout.TextField("Commit Message", _commitMessage);
        if (GUILayout.Button("Commit & Push All Changes"))
        {
            if (EditorUtility.DisplayDialog("Confirm Commit & Push", "This will add, commit, and push all tracked changes in the repository. Are you sure?", "Yes, Commit & Push", "Cancel"))
            {
                CommitAndPush();
            }
        }
    }
    #endregion
            
    #region Utility Methods
    private string GetTooltipForStatus(string status)
    {
        if (status.Contains("Needs Build")) return "Source assets changed. Needs a rebuild.";
        if (status.Contains("Synced")) return "AssetBundle is up-to-date.";
        if (status.Contains("New")) return "Song not yet built.";
        if (status.Contains("Error")) return "Source folder not found.";
        return "Unknown status.";
    }

    private string FindSourceAudio(string songId)
    {
        string songDirectory = SongsSourceBasePath + songId;
        if (!Directory.Exists(songDirectory)) return null;

        // Prefer WAV over MP3 for higher quality source
        string[] wavFiles = Directory.GetFiles(songDirectory, "*.wav");
        if (wavFiles.Length > 0) return wavFiles[0];

        string[] mp3Files = Directory.GetFiles(songDirectory, "*.mp3");
        if (mp3Files.Length > 0) return mp3Files[0];
        
        string[] oggFiles = Directory.GetFiles(songDirectory, "*.ogg");
        if (oggFiles.Length > 0) return oggFiles[0];
            
        return null;
    }

    private void ReloadData()
    {
        _songInfos.Clear();
        if (!File.Exists(SongListPath)) { return; }
        string json = File.ReadAllText(SongListPath);
        SongManifest manifest = JsonUtility.FromJson<SongManifest>(json);
        if (manifest == null || manifest.songs == null) { return; }
        string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
        string platformBundlePath = Path.Combine(AssetBundlesOutputPath, platformName);
        var ignoreList = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "title" };
        foreach (var songMetadata in manifest.songs)
        {
            if (ignoreList.Contains(songMetadata.id)) { continue; }
            var info = new SongBuildInfo(songMetadata);
            string sourceFolderPath = SongsSourceBasePath + songMetadata.id;
            string bundleFilePath = Path.Combine(platformBundlePath, songMetadata.chartBundleName); 
            if (!Directory.Exists(sourceFolderPath)) { info.status = "❌ Error"; info.statusColor = Color.red; }
            else if (!File.Exists(bundleFilePath)) { info.status = "✨ New"; info.statusColor = Color.cyan; }
            else
            {
                DateTime sourceUpdateTime = Directory.GetLastWriteTimeUtc(sourceFolderPath);
                DateTime bundleUpdateTime = File.GetLastWriteTimeUtc(bundleFilePath);
                if (sourceUpdateTime > bundleUpdateTime) { info.status = "⚠️ Needs Build"; info.statusColor = Color.yellow; }
                else { info.status = "✅ Synced"; info.statusColor = Color.green; }
            }
            _songInfos.Add(info);
        }
        Repaint();
        UpdateGitStatus();
    }

    private new void SaveChanges()
    {
        SongManifest manifest = new SongManifest();
        if (File.Exists(SongListPath)) { manifest = JsonUtility.FromJson<SongManifest>(File.ReadAllText(SongListPath)); }
        else { manifest.songs = new List<SongMetadata>(); }
        foreach (var info in _songInfos)
        {
            SongMetadata songToUpdate = manifest.songs.Find(s => s.id.Equals(info.metadata.id, StringComparison.OrdinalIgnoreCase));
            if (songToUpdate != null)
            {
                songToUpdate.version = info.version_edit;
                songToUpdate.unlockedByDefault = info.unlockedByDefault_edit;
                songToUpdate.priceCoins = info.priceCoins_edit;
                songToUpdate.priceGems = info.priceGems_edit;
                songToUpdate.requiredLevel = info.requiredLevel_edit;
                songToUpdate.tags = info.tags_edit.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            }
        }
        string newJson = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(SongListPath, newJson);
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("SongList.json has been saved with new metadata.");
        ReloadData();
    }
            
    #endregion
            
    #region Core Actions
            
    private void ConvertToCompatibleOGG(SongBuildInfo songInfo)
    {
        string songId = songInfo.metadata.id;
        UnityEngine.Debug.Log($"Attempting to convert source audio for '{songId}' to compatible OGG.");
        string sourceAudioPath = FindSourceAudio(songId);
        if (string.IsNullOrEmpty(sourceAudioPath))
        {
            UnityEngine.Debug.LogError($"Could not find a source audio file (.wav or .mp3) for song '{songId}'.");
            return;
        }
        string outputOggPath = Path.Combine(Path.GetDirectoryName(sourceAudioPath), Path.GetFileNameWithoutExtension(sourceAudioPath) + ".ogg");
        
        if (sourceAudioPath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) {
            outputOggPath = Path.Combine(Path.GetDirectoryName(sourceAudioPath), Path.GetFileNameWithoutExtension(sourceAudioPath) + "_new.ogg");
        }
        string ffmpegArgs = $"-y -i \"{sourceAudioPath}\" -q:a 5 -ar 44100 \"{outputOggPath}\"";
        UnityEngine.Debug.Log($"Running ffmpeg with args: {ffmpegArgs}");
        string output, error;
        bool success = RunProcessSync(FfmpegPath, ffmpegArgs, out output, out error);
        if (success)
        {
            UnityEngine.Debug.Log($"Successfully converted '{Path.GetFileName(sourceAudioPath)}' to '{Path.GetFileName(outputOggPath)}'.\nOutput:\n{output}\n{error}");
            AssetDatabase.Refresh();
        }
        else
        {
            UnityEngine.Debug.LogError($"Failed to convert audio for '{songId}'.\nError:\n{error}");
        }
    }
            
    private void IncrementVersion(SongBuildInfo songToUpdate)
    {
        songToUpdate.version_edit++;
        songToUpdate.MarkAsDirty();
        Repaint();
    }
            
    private void ScanForNewSongs()
    {
        SongManifest manifest = new SongManifest();
        if (File.Exists(SongListPath)) { manifest = JsonUtility.FromJson<SongManifest>(File.ReadAllText(SongListPath)); } 
        else { manifest.songs = new List<SongMetadata>(); }
        var ignoreList = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "title" };
        var existingSongIds = new HashSet<string>(manifest.songs.Select(s => s.id), StringComparer.OrdinalIgnoreCase);
        var newSongIds = new List<string>();
        if (!Directory.Exists(SongsSourceBasePath)) { UnityEngine.Debug.LogWarning($"Song source path not found: {SongsSourceBasePath}"); return; }
        foreach (string folderPath in Directory.GetDirectories(SongsSourceBasePath))
        {
            string songId = Path.GetFileName(folderPath);
            if (!existingSongIds.Contains(songId) && !ignoreList.Contains(songId))
            {
                newSongIds.Add(songId);
                manifest.songs.Add(new SongMetadata {
                    id = songId,
                    version = 1,
                    unlockedByDefault = false,
                    priceCoins = 1000,
                    priceGems = 0,
                    requiredLevel = 1,
                    tags = new List<string> { "new" }
                });
            }
        }
        if (newSongIds.Count > 0)
        {
            File.WriteAllText(SongListPath, JsonUtility.ToJson(manifest, true));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Scan Complete", string.Format("Added {0} new song(s):\n{1}", newSongIds.Count, string.Join("\n", newSongIds)), "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Scan Complete", "No new unregistered songs found.", "OK");
        }
        ReloadData();
    }
            
    private void BuildAllSongs(bool forceRebuild) 
    { 
        BuildSongs(_songInfos.Select(s => s.metadata).ToList(), forceRebuild); 
    }
            
    private string FindSpecificAsset(string songId, string extension)
    {
        string songDirectory = SongsSourceBasePath + songId;
        if (!Directory.Exists(songDirectory)) return null;
        string[] files = Directory.GetFiles(songDirectory, $"*.{extension}");
        return files.Length > 0 ? files[0] : null;
    }

    private void OpenBuildFolder()
    {
        if (!Directory.Exists(AssetBundlesOutputPath)) Directory.CreateDirectory(AssetBundlesOutputPath);
        EditorUtility.RevealInFinder(AssetBundlesOutputPath);
    }

    private void OpenSongListJsonFolder()
    {
        if (File.Exists(SongListPath)) EditorUtility.RevealInFinder(SongListPath);
        else UnityEngine.Debug.LogError($"Song Manager: SongList.json not found at {SongListPath}");
    }
            
    private void BuildSongs(List<SongMetadata> songsToBuild, bool forceRebuild)
    {
        string platformDirectory = Path.Combine(AssetBundlesOutputPath, EditorUserBuildSettings.activeBuildTarget.ToString());

        if (forceRebuild && Directory.Exists(platformDirectory)) { Directory.Delete(platformDirectory, true); }
        if (!Directory.Exists(platformDirectory)) { Directory.CreateDirectory(platformDirectory); }

        List<string> allAudioPaths = new List<string>();
        List<string> allChartPaths = new List<string>();

        var ignoreList = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "title", "calibrationsong" };

        foreach (var song in songsToBuild)
        {
            if(ignoreList.Contains(song.id)) continue;

            string audioPath = FindSourceAudio(song.id);
            if (!string.IsNullOrEmpty(audioPath)) allAudioPaths.Add(audioPath);

            string songInfoPath = FindSpecificAsset(song.id, "asset");
            if (!string.IsNullOrEmpty(songInfoPath)) allChartPaths.Add(songInfoPath);
            
            string timelinePath = FindSpecificAsset(song.id, "playable");
            if (!string.IsNullOrEmpty(timelinePath)) allChartPaths.Add(timelinePath);
        }

        List<AssetBundleBuild> buildMap = new List<AssetBundleBuild>();

        if(allAudioPaths.Count > 0)
        {
            buildMap.Add(new AssetBundleBuild
            {
                assetBundleName = "songs/music",
                assetNames = allAudioPaths.ToArray()
            });
        }

        if(allChartPaths.Count > 0)
        {
            buildMap.Add(new AssetBundleBuild
            {
                assetBundleName = "songs/charts",
                assetNames = allChartPaths.ToArray()
            });
        }

        if (buildMap.Count == 0) { EditorUtility.DisplayDialog("No Songs Found", "Could not find any valid song source folders to build.", "OK"); return; }
    
        var finalBuildMapLog = new System.Text.StringBuilder();
        finalBuildMapLog.AppendLine("[GEMINI_DEBUG] Final Build Map to be processed by Unity:");
        foreach(var build in buildMap)
        {
            finalBuildMapLog.AppendLine($"  - Bundle Name: {build.assetBundleName}");
            foreach(var asset in build.assetNames)
            {
                finalBuildMapLog.AppendLine($"    - Asset: {asset}");
            }
        }
        UnityEngine.Debug.Log(finalBuildMapLog.ToString());

        BuildPipeline.BuildAssetBundles(platformDirectory, buildMap.ToArray(),
            BuildAssetBundleOptions.None,
            EditorUserBuildSettings.activeBuildTarget);

        UnityEngine.Debug.Log(string.Format("Build of {0} song(s) complete.", songsToBuild.Count));
        ReloadData();
    }
    #endregion

    #region Git Integration
    private void UpdateGitStatus()
    {
        _globalBranchStatus = "Checking...";
        _songListGitStatus = "Checking...";
        Repaint();
    }

    private void CommitAndPush()
    {
        UnityEngine.Debug.Log("--- Starting Git Commit & Push ---");

        string branchNameOutput = RunGitCommandSync("rev-parse --abbrev-ref HEAD");
        if (branchNameOutput.Contains("ERROR:") || string.IsNullOrEmpty(branchNameOutput))
        {
            UnityEngine.Debug.LogError("Could not determine current git branch. Aborting push.");
            return;
        }
        string currentBranch = branchNameOutput.Trim();
        UnityEngine.Debug.Log($"Detected current branch as '{currentBranch}'");

        string addResult = RunGitCommandSync("add .");
        UnityEngine.Debug.Log("Git Add Result:\n" + addResult);
        if (addResult.Contains("ERROR:") || addResult.Contains("EXCEPTION:")) return;

        string formattedMessage = _commitMessage.Replace("\"", "\\\"");
        string commitResult = RunGitCommandSync(string.Format("commit -m \"{0}\"", formattedMessage));
        UnityEngine.Debug.Log("Git Commit Result:\n" + commitResult);

        string pullResult = RunGitCommandSync($"pull --rebase origin {currentBranch}");
        UnityEngine.Debug.Log($"Git Pull --rebase Result (from branch {currentBranch}):\n" + pullResult);
        if (pullResult.Contains("ERROR:") || pullResult.Contains("EXCEPTION:") || pullResult.ToLower().Contains("conflict"))
        {
            UnityEngine.Debug.LogError("Aborting push due to pull/rebase failure. Please resolve conflicts manually in the terminal.");
            return;
        }

        string pushResult = RunGitCommandSync($"push -u origin {currentBranch}");
        UnityEngine.Debug.Log($"Git Push Result (to branch {currentBranch}):\n" + pushResult);

        UnityEngine.Debug.Log("--- Git Commit & Push finished. ---");

        EditorApplication.delayCall += UpdateGitStatus;
    }            
    private string RunGitCommandSync(string args)
    {
        string output, error;
        bool success = RunProcessSync("git", args, out output, out error);

        if (!success)
        {
            if (error.Contains("not a git repository")) return "ERROR: Not a Git Repository";
            return $"ERROR: {error}";
        }
        
        if (error.Contains("nothing to commit"))
        {
            UnityEngine.Debug.Log("Git: Nothing to commit.");
            return output; 
        }

        if (string.IsNullOrEmpty(output) && !string.IsNullOrEmpty(error))
        {
            return error;
        }

        return output.Trim();
    }

    private bool RunProcessSync(string command, string args, out string output, out string error)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(command)
            {
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Application.dataPath.Replace("/Assets", "")
            };

            Process process = new Process { StartInfo = startInfo };
            
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            output = outputBuilder.ToString().Trim();
            error = errorBuilder.ToString().Trim();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                 UnityEngine.Debug.LogError($"{command} Command Error (Exit Code: {process.ExitCode}):\n{error}");
                 return false;
            }
            
            return process.ExitCode == 0;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to run {command} command. Is it installed and in your system's PATH?\n{e.Message}");
            output = "";
            error = e.Message;
            return false;
        }
    }
    #endregion
}
