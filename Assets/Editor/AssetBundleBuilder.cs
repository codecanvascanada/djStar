using UnityEditor;
using System.IO;
using UnityEngine;

public class AssetBundleBuilder
{
    [MenuItem("Assets/Build AssetBundles for All Platforms")]
    static void BuildAllAssetBundles()
    {
        // Main output directory
        string mainAssetBundleDirectory = "AssetBundles";

        // Create the main output directory if it doesn't exist
        if (!Directory.Exists(mainAssetBundleDirectory))
        {
            Directory.CreateDirectory(mainAssetBundleDirectory);
        }

        // List of target platforms
        BuildTarget[] targets = new BuildTarget[]
        {
            BuildTarget.Android,
            BuildTarget.WebGL,
            BuildTarget.iOS,
            BuildTarget.StandaloneOSX // For Mac Editor testing
        };

        foreach (BuildTarget target in targets)
        {
            string platformDirectory = Path.Combine(mainAssetBundleDirectory, target.ToString());
            if (!Directory.Exists(platformDirectory))
            {
                Directory.CreateDirectory(platformDirectory);
            }

            try
            {
                BuildPipeline.BuildAssetBundles(platformDirectory,
                                                BuildAssetBundleOptions.None,
                                                target);
                UnityEngine.Debug.Log(string.Format("AssetBundles for {0} built successfully and saved to '{1}'.", target, platformDirectory));
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError(string.Format("Failed to build AssetBundles for {0}: {1}", target, e.Message));
            }
        }
    }
}
