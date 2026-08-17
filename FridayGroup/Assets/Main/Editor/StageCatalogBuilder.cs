using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Resources/Stage以下の「Stage*/面フォルダ」を走査して、実行時用カタログを自動生成します。
/// Stage3以降を追加しても、Unityへ戻った時点で一覧へ反映されます。
/// </summary>
public static class StageCatalogBuilder
{
    private const string StageRootAssetPath = "Assets/Main/Resources/Stage";
    private const string CatalogAssetPath = StageRootAssetPath + "/stage_catalog.json";

    private static bool rebuildScheduled;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        ScheduleRebuild();
    }

    [MenuItem("FridayGroup/Stage/Rebuild Catalog")]
    public static void RebuildCatalog()
    {
        rebuildScheduled = false;

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            return;
        }

        string stageRootFullPath = Path.Combine(projectRoot, StageRootAssetPath.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(stageRootFullPath))
        {
            Debug.LogWarning($"Stageフォルダが見つかりません: {StageRootAssetPath}");
            return;
        }

        StageCatalogData catalog = new StageCatalogData();
        List<string> groupDirectories = Directory.GetDirectories(stageRootFullPath).ToList();
        groupDirectories.Sort((left, right) => StageCatalog.NaturalCompare(Path.GetFileName(left), Path.GetFileName(right)));

        foreach (string groupDirectory in groupDirectories)
        {
            string groupFolder = Path.GetFileName(groupDirectory);
            List<string> stageDirectories = Directory.GetDirectories(groupDirectory).ToList();
            stageDirectories.Sort((left, right) => StageCatalog.NaturalCompare(Path.GetFileName(left), Path.GetFileName(right)));

            foreach (string stageDirectory in stageDirectories)
            {
                string stageFolder = Path.GetFileName(stageDirectory);
                string[] csvFiles = Directory
                    .GetFiles(stageDirectory, "*.csv", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderBy(fileName => fileName, Comparer<string>.Create(StageCatalog.NaturalCompare))
                    .ToArray();

                catalog.stages.Add(new StageCatalogEntry
                {
                    groupFolder = groupFolder,
                    stageFolder = stageFolder,
                    resourcePath = $"Stage/{groupFolder}/{stageFolder}",
                    csvFiles = csvFiles
                });
            }
        }

        string json = JsonUtility.ToJson(catalog, true).Replace("\r\n", "\n") + "\n";
        string currentJson = File.Exists(Path.Combine(projectRoot, CatalogAssetPath.Replace('/', Path.DirectorySeparatorChar)))
            ? File.ReadAllText(Path.Combine(projectRoot, CatalogAssetPath.Replace('/', Path.DirectorySeparatorChar))).Replace("\r\n", "\n")
            : null;

        if (currentJson == json)
        {
            return;
        }

        string catalogFullPath = Path.Combine(projectRoot, CatalogAssetPath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(catalogFullPath, json, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(CatalogAssetPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"Stage catalogを更新しました: {catalog.stages.Count} stages");
    }

    public static void ScheduleRebuild()
    {
        if (rebuildScheduled)
        {
            return;
        }

        rebuildScheduled = true;
        EditorApplication.delayCall += RebuildCatalog;
    }

    public static bool ContainsStageAssetPath(string assetPath)
    {
        return !string.IsNullOrEmpty(assetPath) &&
               assetPath.StartsWith(StageRootAssetPath + "/", StringComparison.OrdinalIgnoreCase) &&
               !assetPath.StartsWith(CatalogAssetPath, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class StageCatalogAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (ContainsStageAsset(importedAssets) ||
            ContainsStageAsset(deletedAssets) ||
            ContainsStageAsset(movedAssets) ||
            ContainsStageAsset(movedFromAssetPaths))
        {
            StageCatalogBuilder.ScheduleRebuild();
        }
    }

    private static bool ContainsStageAsset(IEnumerable<string> assetPaths)
    {
        return assetPaths != null && assetPaths.Any(StageCatalogBuilder.ContainsStageAssetPath);
    }
}
