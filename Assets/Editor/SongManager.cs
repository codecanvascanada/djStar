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
    // ... (SongBuildInfo class and other fields remain the same) ...
    private string _songListGitStatus = "Press Refresh...";
    private string _globalBranchStatus = "Press Refresh...";
    
    // ...

    private void OnFocus()
    {
        ReloadData();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Song AssetBundle & Metadata Manager", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Manage song metadata, versions (via content hash), and builds.", MessageType.Info);

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
        
        // Display Git Status
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
    
    // ... (DrawHeader, DrawSongList, most of DrawGlobalActions remain the same) ...

    private void ReloadData()
    {
        _songInfos.Clear();
        _allKnownTags.Clear();
        if (!File.Exists(SongListPath)) { return; }

        string json = File.ReadAllText(SongListPath);
        SongManifest manifest = JsonUtility.FromJson<SongManifest>(json);

        if (manifest == null || manifest.songs == null) { return; }
        
        string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
        string platformBundlePath = Path.Combine(AssetBundlesOutputPath, platformName);
        
        HashSet<string> tagsSet = new HashSet<string>();
        var ignoreList = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "calibrationsong", "title" };

        foreach (var songMetadata in manifest.songs)
        {
            if (ignoreList.Contains(songMetadata.id)) { continue; }

            var info = new SongBuildInfo(songMetadata);
            string sourceFolderPath = SongsSourceBasePath + songMetadata.id;
            string bundleFilePath = Path.Combine(platformBundlePath, info.metadata.assetBundleName);

            if (info.metadata.tags != null)
            {
                foreach(var tag in info.metadata.tags) tagsSet.Add(tag);
            }

            if (!Directory.Exists(sourceFolderPath)) { info.status = "❌ Error"; info.statusColor = Color.red; }
            else if (string.IsNullOrEmpty(info.metadata.versionHash) || !File.Exists(bundleFilePath)) { info.status = "✨ New"; info.statusColor = Color.cyan; }
            else
            {
                DateTime sourceUpdateTime = Directory.GetLastWriteTimeUtc(sourceFolderPath);
                DateTime bundleUpdateTime = File.GetLastWriteTimeUtc(bundleFilePath);
                if (sourceUpdateTime > bundleUpdateTime) { info.status = "⚠️ Needs Build"; info.statusColor = Color.yellow; }
                else { info.status = "✅ Synced"; info.statusColor = Color.green; }
            }
            _songInfos.Add(info);
        }
        _allKnownTags = tagsSet.OrderBy(t => t).ToList();
        
        Repaint();
        UnityEngine.Debug.Log("Song Manager data reloaded.");
        
        // Update Git status after reloading data
        UpdateGitStatus();
    }
    
    // ... (SaveChanges, ScanForNewSongs, Build methods remain the same) ...
    
    #region Git Integration
    private void UpdateGitStatus()
    {
        string gitRootPath = RunGitCommandSync("rev-parse --show-toplevel");
        if (gitRootPath.Contains("ERROR:") || gitRootPath.Contains("EXCEPTION:"))
        {
            _songListGitStatus = "❌ Git Not Initialized or Not Found";
            _globalBranchStatus = "❌ Git Not Initialized or Not Found";
            Repaint();
            return;
        }

        // --- Check Global Branch Status (ahead/behind remote) ---
        RunGitCommandSync("fetch"); // Update local knowledge of remote branch
        string branchStatusOutput = RunGitCommandSync("status -uno"); // -uno hides untracked files for cleaner output

        if (branchStatusOutput.Contains("Your branch is ahead of"))
        {
            _globalBranchStatus = "⬆️ Needs Push";
        }
        else if (branchStatusOutput.Contains("Your branch is behind"))
        {
            _globalBranchStatus = "⬇️ Needs Pull";
        }
        else if (branchStatusOutput.Contains("up to date with"))
        {
            _globalBranchStatus = "✅ Synced with Remote";
        }
        else
        {
             _globalBranchStatus = "❓ Unknown Branch Status";
        }

        // --- Git status for SongList.json ---
        string songListFilePath = Path.Combine("Assets", "ServerMock", "SongList.json"); // Relative to project root
        string gitStatusOutput = RunGitCommandSync(string.Format("status --porcelain \"{0}\"", songListFilePath));
        
        if (gitStatusOutput.Contains("??")) 
        {
            _songListGitStatus = "✨ Untracked (Needs add/commit)";
        }
        else if (gitStatusOutput.Contains("M")) // Checking for M with a space is more robust
        {
            _songListGitStatus = "⬆️ Modified (Needs commit/push)";
        }
        else if (string.IsNullOrEmpty(gitStatusOutput.Trim())) 
        {
            // If clean locally, its status depends on the global branch status
            if(_globalBranchStatus.Contains("Needs Push"))
                _songListGitStatus = "✅ Committed (Ready to Push)";
            else
                _songListGitStatus = "✅ Synced";
        }
        else
        {
            _songListGitStatus = "❓ Unknown";
        }

        Repaint();
    }
    
    private string RunGitCommandSync(string args)
    {
        string gitCommand = "git";
        ProcessStartInfo startInfo = new ProcessStartInfo(gitCommand)
        {
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Application.dataPath.Replace("/Assets", "")
        };

        Process process = new Process { StartInfo = startInfo };
        
        StringBuilder outputBuilder = new StringBuilder();
        StringBuilder errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit(5000);

            string output = outputBuilder.ToString().Trim();
            string error = errorBuilder.ToString().Trim();

            if (process.ExitCode != 0)
            {
                if (error.Contains("not a git repository"))
                {
                    return "ERROR: Not a Git Repository";
                }
                UnityEngine.Debug.LogError("Git Command Error (Exit Code: " + process.ExitCode + "): " + error);
                return "ERROR: " + error;
            }
            return output;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Failed to run git command. Is git installed and in your system's PATH?\n" + e.Message);
            return "EXCEPTION: " + e.Message;
        }
    }
    #endregion
}
