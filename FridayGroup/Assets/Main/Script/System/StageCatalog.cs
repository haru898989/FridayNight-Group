using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class StageCatalogData
{
    public List<StageCatalogEntry> stages = new List<StageCatalogEntry>();
}

[Serializable]
public sealed class StageCatalogEntry
{
    public string groupFolder;
    public string stageFolder;
    public string resourcePath;

    public int timeLimit = 180;

    public string[] csvFiles = Array.Empty<string>();

    [NonSerialized] public string displayName;
    [NonSerialized] public string description;
    [NonSerialized] public string previewFile;
    [NonSerialized] public string[] csvOrder;

    public bool HasMapData => csvFiles != null && csvFiles.Length > 0;
}

[Serializable]
public sealed class StageMetadata
{
    public string displayName;
    public string description;
    public string previewFile = "preview";
    public string[] csvOrder;
}

/// <summary>
/// Resources/Stage/stage_catalog.jsonを読み込みます。
/// カタログ自体はStageCatalogBuilderがフォルダ構成から自動生成します。
/// </summary>
public static class StageCatalog
{
    private const string CatalogResourcePath = "Stage/stage_catalog";
    private const string MetadataFileName = "stage";

    public static List<StageCatalogEntry> Load()
    {
        TextAsset catalogAsset = Resources.Load<TextAsset>(CatalogResourcePath);
        if (catalogAsset == null)
        {
            Debug.LogError("Stage catalogが見つかりません。Unityメニューの FridayGroup/Stage/Rebuild Catalog を実行してください");
            return new List<StageCatalogEntry>();
        }

        StageCatalogData data = JsonUtility.FromJson<StageCatalogData>(catalogAsset.text);
        List<StageCatalogEntry> entries = data?.stages ?? new List<StageCatalogEntry>();

        foreach (StageCatalogEntry entry in entries)
        {
            ApplyMetadata(entry);
        }

        entries.Sort(CompareEntries);
        return entries;
    }

    public static TextAsset[] LoadMapFloorData(StageCatalogEntry entry)
    {
        if (entry == null)
        {
            return Array.Empty<TextAsset>();
        }

        string[] orderedFileNames = ResolveCsvOrder(entry);

        if (orderedFileNames == null || orderedFileNames.Length == 0)
        {
            return Array.Empty<TextAsset>();
        }

        List<TextAsset> assets = new List<TextAsset>();
        foreach (string fileName in orderedFileNames)
        {
            string resourceName = RemoveExtension(fileName);
            TextAsset csv = Resources.Load<TextAsset>($"{entry.resourcePath}/{resourceName}");
            if (csv == null)
            {
                Debug.LogError($"ステージCSVが見つかりません: {entry.resourcePath}/{resourceName}.csv");
                return Array.Empty<TextAsset>();
            }

            assets.Add(csv);
        }

        return assets.ToArray();
    }

    private static string[] ResolveCsvOrder(StageCatalogEntry entry)
    {
        if (entry.csvOrder != null && entry.csvOrder.Length > 0)
        {
            return entry.csvOrder;
        }

        string[] csvFiles = entry.csvFiles ?? Array.Empty<string>();
        if (csvFiles.Length != 5)
        {
            return csvFiles;
        }

        // 既存マップと同じ5ファイル構成なら、壁CSVを3層分へ自動展開します。
        // BF / B1Walls / 1F / 1FWalls / Roof の名前を含めればstage.jsonは不要です。
        string basementFloor = csvFiles.FirstOrDefault(name =>
            ContainsIgnoreCase(name, "BF") && !ContainsIgnoreCase(name, "Wall"));
        string basementWalls = csvFiles.FirstOrDefault(name => ContainsIgnoreCase(name, "B1Walls"));
        string firstFloor = csvFiles.FirstOrDefault(name =>
            ContainsIgnoreCase(name, "1F") && !ContainsIgnoreCase(name, "Wall"));
        string firstFloorWalls = csvFiles.FirstOrDefault(name => ContainsIgnoreCase(name, "1FWalls"));
        string roof = csvFiles.FirstOrDefault(name => ContainsIgnoreCase(name, "Roof"));

        if (basementFloor == null || basementWalls == null || firstFloor == null || firstFloorWalls == null || roof == null)
        {
            return csvFiles;
        }

        return new[]
        {
            basementFloor,
            basementWalls,
            basementWalls,
            basementWalls,
            firstFloor,
            firstFloorWalls,
            firstFloorWalls,
            firstFloorWalls,
            roof
        };
    }

    public static TextAsset[] LoadMapFloorData(string resourcePath)
    {
        string normalizedPath = NormalizePath(resourcePath);
        StageCatalogEntry entry = Load().FirstOrDefault(stage => stage.resourcePath == normalizedPath);
        if (entry == null)
        {
            Debug.LogError($"選択されたステージがStage catalogにありません: {normalizedPath}");
            return Array.Empty<TextAsset>();
        }

        return LoadMapFloorData(entry);
    }

    public static Texture2D LoadPreview(StageCatalogEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        string previewName = string.IsNullOrWhiteSpace(entry.previewFile) ? "preview" : RemoveExtension(entry.previewFile);
        return Resources.Load<Texture2D>($"{entry.resourcePath}/{previewName}");
    }

    private static void ApplyMetadata(StageCatalogEntry entry)
    {
        entry.displayName = entry.stageFolder;
        entry.description = entry.HasMapData ? "READY" : "CSV NOT SET";
        entry.previewFile = "preview";
        entry.csvOrder = null;

        TextAsset metadataAsset = Resources.Load<TextAsset>($"{entry.resourcePath}/{MetadataFileName}");
        if (metadataAsset == null)
        {
            return;
        }

        StageMetadata metadata = JsonUtility.FromJson<StageMetadata>(metadataAsset.text);
        if (metadata == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(metadata.displayName))
        {
            entry.displayName = metadata.displayName;
        }

        if (!string.IsNullOrWhiteSpace(metadata.description))
        {
            entry.description = metadata.description;
        }

        if (!string.IsNullOrWhiteSpace(metadata.previewFile))
        {
            entry.previewFile = metadata.previewFile;
        }

        if (metadata.csvOrder != null && metadata.csvOrder.Length > 0)
        {
            entry.csvOrder = metadata.csvOrder;
        }
    }

    private static int CompareEntries(StageCatalogEntry left, StageCatalogEntry right)
    {
        int groupComparison = NaturalCompare(left?.groupFolder, right?.groupFolder);
        return groupComparison != 0
            ? groupComparison
            : NaturalCompare(left?.stageFolder, right?.stageFolder);
    }

    public static int NaturalCompare(string left, string right)
    {
        left ??= string.Empty;
        right ??= string.Empty;

        int leftIndex = 0;
        int rightIndex = 0;

        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            if (char.IsDigit(left[leftIndex]) && char.IsDigit(right[rightIndex]))
            {
                int leftStart = leftIndex;
                int rightStart = rightIndex;
                while (leftIndex < left.Length && char.IsDigit(left[leftIndex])) leftIndex++;
                while (rightIndex < right.Length && char.IsDigit(right[rightIndex])) rightIndex++;

                long leftNumber = long.Parse(left.Substring(leftStart, leftIndex - leftStart), CultureInfo.InvariantCulture);
                long rightNumber = long.Parse(right.Substring(rightStart, rightIndex - rightStart), CultureInfo.InvariantCulture);
                int numberComparison = leftNumber.CompareTo(rightNumber);
                if (numberComparison != 0)
                {
                    return numberComparison;
                }

                continue;
            }

            int characterComparison = char.ToUpperInvariant(left[leftIndex]).CompareTo(char.ToUpperInvariant(right[rightIndex]));
            if (characterComparison != 0)
            {
                return characterComparison;
            }

            leftIndex++;
            rightIndex++;
        }

        return left.Length.CompareTo(right.Length);
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().Replace('\\', '/').Trim('/');
    }

    private static string RemoveExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        int extensionIndex = fileName.LastIndexOf('.');
        return extensionIndex > 0 ? fileName.Substring(0, extensionIndex) : fileName;
    }

    private static bool ContainsIgnoreCase(string value, string fragment)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
