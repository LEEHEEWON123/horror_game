#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// WebGL 빌드 용량 절감: 텍스처 Max Size를 절반으로 줄입니다 (WebGL 플랫폼 override).
/// Tools > WebGL > Half Texture Resolution
/// </summary>
public static class WebGLTextureHalfResTool
{
    private const string WebGlBuildTarget = "WebGL";
    private const int MinTextureSize = 64;

    public readonly struct HalfSummary
    {
        public readonly int Changed;
        public readonly int AlreadyMinimum;
        public readonly int Skipped;

        public HalfSummary(int changed, int alreadyMinimum, int skipped)
        {
            Changed = changed;
            AlreadyMinimum = alreadyMinimum;
            Skipped = skipped;
        }
    }

    private static readonly string[] SkipPathContains =
    {
        "/TextMesh Pro/",
        "/Editor Default Resources/",
    };

    [MenuItem("Tools/WebGL/Prepare Textures for itch.io (2nd pass)")]
    public static void PrepareForItchMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "WebGL itch.io 준비",
                "WebGL 텍스처 Max Size를 한 번 더 절반으로 줄입니다.\n\n" +
                "512 → 256, 1024 → 512 …\n" +
                "이후 Tools → Build WebGL for itch.io 로 빌드하세요.",
                "실행",
                "취소"))
            return;

        var summary = PrepareForItchSilent();
        EditorUtility.DisplayDialog(
            "WebGL itch.io 준비",
            $"완료\n\n변경: {summary.Changed}\n이미 최소: {summary.AlreadyMinimum}\n건너뜀: {summary.Skipped}",
            "확인");
    }

    [MenuItem("Tools/WebGL/Restore WebGL Texture Quality (512 min)")]
    public static void RestoreWebGLQualityMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "WebGL Texture Quality Restore",
                "WebGL 텍스처를 최소 512, Lightmap 512로 올리고\n" +
                "Crunched/LQ 압축을 해제합니다.\n\n" +
                "에디터/Standalone 품질은 그대로입니다.",
                "실행",
                "취소"))
            return;

        var summary = RestoreWebGLQualitySilent();
        EditorUtility.DisplayDialog(
            "WebGL Texture Quality Restore",
            $"완료\n\n복구: {summary.Changed}\n건너뜀: {summary.Skipped}",
            "확인");
    }

    public static HalfSummary RestoreWebGLQualitySilent()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int changed = 0;
        int skipped = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (ShouldSkipPath(path))
                {
                    skipped++;
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    skipped++;
                    continue;
                }

                var settings = importer.GetPlatformTextureSettings(WebGlBuildTarget);
                if (!settings.overridden)
                {
                    skipped++;
                    continue;
                }

                int minSize = path.Contains("Lightmap") || path.EndsWith(".exr")
                    ? 512
                    : 512;

                bool dirty = false;
                if (settings.maxTextureSize < minSize)
                {
                    settings.maxTextureSize = minSize;
                    dirty = true;
                }

                if (settings.crunchedCompression)
                {
                    settings.crunchedCompression = false;
                    dirty = true;
                }

                if (settings.textureCompression == TextureImporterCompression.CompressedLQ)
                {
                    settings.textureCompression = TextureImporterCompression.Compressed;
                    dirty = true;
                }

                if (!dirty)
                {
                    skipped++;
                    continue;
                }

                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[WebGLTextureHalfRes] RestoreWebGLQuality changed={changed}, skipped={skipped}");
        return new HalfSummary(changed, 0, skipped);
    }

    [MenuItem("Tools/WebGL/Half Texture Resolution for WebGL")]
    public static void HalfAllTextures()
    {
        if (!EditorUtility.DisplayDialog(
                "WebGL Texture Half Resolution",
                "Assets 폴더의 모든 텍스처에 WebGL Max Size override를 적용합니다.\n\n" +
                "2048 → 1024, 1024 → 512 … (최소 64)\n" +
                "Standalone/에디터 품질은 그대로입니다.\n\n" +
                "계속할까요?",
                "실행",
                "취소"))
            return;

        ApplyHalfResolution(AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }), silent: false);
    }

    /// <summary>
    /// itch.io 업로드 한도(200MB/파일)용: half-res 1회 + WebGL 압축 강화. WebGLBuild에서 자동 호출.
    /// </summary>
    public static HalfSummary PrepareForItchSilent()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        var summary = ApplyHalfResolution(guids, silent: true);
        Debug.Log(
            $"[WebGLTextureHalfRes] PrepareForItch: changed={summary.Changed}, " +
            $"alreadyMinimum={summary.AlreadyMinimum}, skipped={summary.Skipped}");
        return summary;
    }

    public static HalfSummary HalfAllTexturesSilent(int passes = 1)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        HalfSummary total = default;
        passes = Mathf.Max(1, passes);

        for (int pass = 0; pass < passes; pass++)
        {
            var summary = ApplyHalfResolution(guids, silent: true);
            total = new HalfSummary(
                total.Changed + summary.Changed,
                total.AlreadyMinimum + summary.AlreadyMinimum,
                total.Skipped + summary.Skipped);

            if (summary.Changed == 0)
                break;
        }

        return total;
    }

    [MenuItem("Tools/WebGL/Half Texture Resolution (Selected Only)")]
    public static void HalfSelectedTextures()
    {
        var paths = CollectSelectedTexturePaths();
        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "WebGL Texture Half Resolution",
                "Project 창에서 텍스처 또는 폴더를 선택해주세요.",
                "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "WebGL Texture Half Resolution",
                $"선택한 {paths.Count}개 텍스처에 WebGL half resolution을 적용합니다.",
                "실행",
                "취소"))
            return;

        var guids = new string[paths.Count];
        for (int i = 0; i < paths.Count; i++)
            guids[i] = AssetDatabase.AssetPathToGUID(paths[i]);

        ApplyHalfResolution(guids, silent: false);
    }

    private static List<string> CollectSelectedTexturePaths()
    {
        var results = new HashSet<string>();
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { path }))
                    results.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            else if (AssetImporter.GetAtPath(path) is TextureImporter)
            {
                results.Add(path);
            }
        }

        return new List<string>(results);
    }

    private static HalfSummary ApplyHalfResolution(string[] textureGuids, bool silent)
    {
        int changed = 0;
        int skipped = 0;
        int alreadySmall = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < textureGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                if (!silent)
                {
                    EditorUtility.DisplayProgressBar(
                        "WebGL Half Texture Resolution",
                        path,
                        (float)(i + 1) / textureGuids.Length);
                }

                switch (TryHalfTexture(path, out int fromSize, out int toSize))
                {
                    case HalfResult.Changed:
                        changed++;
                        if (!silent)
                            Debug.Log($"[WebGLTextureHalfRes] {path}: {fromSize} → {toSize}");
                        break;
                    case HalfResult.AlreadyMinimum:
                        alreadySmall++;
                        break;
                    default:
                        skipped++;
                        break;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            if (!silent)
                EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (!silent)
        {
            EditorUtility.DisplayDialog(
                "WebGL Texture Half Resolution",
                $"완료\n\n변경: {changed}\n이미 최소: {alreadySmall}\n건너뜀: {skipped}\n\nWebGL 다시 빌드하세요.",
                "확인");
        }

        Debug.Log(
            $"[WebGLTextureHalfRes] Done. changed={changed}, alreadyMinimum={alreadySmall}, skipped={skipped}");
        return new HalfSummary(changed, alreadySmall, skipped);
    }

    private static void ApplyWebGLCompression(string[] textureGuids, bool silent)
    {
        int changed = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < textureGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                if (ShouldSkipPath(path))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                var settings = importer.GetPlatformTextureSettings(WebGlBuildTarget);
                if (!settings.overridden)
                {
                    settings = importer.GetDefaultPlatformTextureSettings();
                    settings.name = WebGlBuildTarget;
                    settings.overridden = true;
                }

                bool dirty = false;

                if (settings.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    settings.textureCompression = TextureImporterCompression.CompressedLQ;
                    dirty = true;
                }
                else if (settings.textureCompression != TextureImporterCompression.CompressedLQ)
                {
                    settings.textureCompression = TextureImporterCompression.CompressedLQ;
                    dirty = true;
                }

                if (!settings.crunchedCompression)
                {
                    settings.crunchedCompression = true;
                    settings.compressionQuality = 0;
                    dirty = true;
                }
                else if (settings.compressionQuality > 0)
                {
                    settings.compressionQuality = 0;
                    dirty = true;
                }

                if (!dirty)
                    continue;

                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[WebGLTextureHalfRes] WebGL compression tightened on {changed} textures.");
    }

    private enum HalfResult
    {
        Skipped,
        AlreadyMinimum,
        Changed,
    }

    private static HalfResult TryHalfTexture(string path, out int fromSize, out int toSize)
    {
        fromSize = 0;
        toSize = 0;

        if (ShouldSkipPath(path))
            return HalfResult.Skipped;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return HalfResult.Skipped;

        if (importer.textureType == TextureImporterType.Sprite && path.Contains("/TextMesh Pro/"))
            return HalfResult.Skipped;

        fromSize = GetEffectiveMaxSize(importer);
        toSize = HalveMaxSize(fromSize);

        if (toSize >= fromSize)
            return HalfResult.AlreadyMinimum;

        var settings = importer.GetPlatformTextureSettings(WebGlBuildTarget);
        if (!settings.overridden)
            settings = importer.GetDefaultPlatformTextureSettings();

        settings.name = WebGlBuildTarget;
        settings.overridden = true;
        settings.maxTextureSize = toSize;

        if (settings.textureCompression == TextureImporterCompression.Uncompressed)
            settings.textureCompression = TextureImporterCompression.Compressed;

        importer.SetPlatformTextureSettings(settings);
        importer.SaveAndReimport();
        return HalfResult.Changed;
    }

    private static bool ShouldSkipPath(string path)
    {
        foreach (var skip in SkipPathContains)
        {
            if (path.Contains(skip))
                return true;
        }

        return false;
    }

    private static int GetEffectiveMaxSize(TextureImporter importer)
    {
        var webGl = importer.GetPlatformTextureSettings(WebGlBuildTarget);
        if (webGl.overridden && webGl.maxTextureSize > 0)
            return webGl.maxTextureSize;

        var defaults = importer.GetDefaultPlatformTextureSettings();
        if (defaults.maxTextureSize > 0)
            return defaults.maxTextureSize;

        return importer.maxTextureSize > 0 ? importer.maxTextureSize : 2048;
    }

    private static int HalveMaxSize(int size)
    {
        int target = Mathf.Max(MinTextureSize, size / 2);

        int[] allowed =
        {
            16384, 8192, 4096, 2048, 1024, 512, 256, 128, 64, 32,
        };

        for (int i = 0; i < allowed.Length; i++)
        {
            if (target >= allowed[i])
                return allowed[i];
        }

        return MinTextureSize;
    }
}
#endif
