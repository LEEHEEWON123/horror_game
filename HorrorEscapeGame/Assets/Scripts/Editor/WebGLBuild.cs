#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// WebGL 빌드:
/// - itch.io: Tools > Build WebGL for itch.io (200MB 한도용 텍스처 축소)
/// - Netlify: Tools > Build WebGL for Netlify (용량 무관, 풀 품질)
/// CLI: Unity -batchmode -executeMethod WebGLBuild.BuildForNetlify -projectPath ...
/// </summary>
public static class WebGLBuild
{
    private const string OutputPath = "Builds/WebGL";
    private const string DataFileRelativePath = "Build/WebGL.data.br";
    /// <summary>itch.io HTML5 per-file limit (200 MiB).</summary>
    private const long ItchMaxDataBytes = 200L * 1024 * 1024;

    [MenuItem("Tools/Build WebGL for itch.io")]
    public static void BuildForItchMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "WebGL Build (itch.io)",
                "Registry 갱신 → Prefab 링크 → 텍스처 축소 → WebGL 빌드\n\n" +
                $"출력: {OutputPath}\n\n(itch.io 200MB 한도용)",
                "빌드",
                "취소"))
            return;

        BuildForItch();
    }

    [MenuItem("Tools/Build WebGL for Netlify")]
    public static void BuildForNetlifyMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "WebGL Build (Netlify)",
                "Registry 갱신 → Prefab 링크 → 텍스처 품질 복구 → WebGL 빌드\n\n" +
                $"출력: {OutputPath}\n\nNetlify용 — 용량 제한 없음, _headers 포함",
                "빌드",
                "취소"))
            return;

        BuildForNetlify();
    }

    public static void BuildForItch()
    {
        PrepareCommonAssets();
        Debug.Log("[WebGLBuild] Shrinking textures for itch.io 200MB limit...");
        WebGLTextureHalfResTool.PrepareForItchSilent();

        if (!RunWebGLBuild("itch.io"))
            return;

        if (!VerifyItchDataSize(out string sizeMessage))
        {
            Debug.LogError($"[WebGLBuild] {sizeMessage}");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "WebGL 빌드 — itch.io 용량 초과",
                    sizeMessage + "\n\nTools → WebGL → Half Texture Resolution 을 한 번 더 실행한 뒤 재빌드하세요.",
                    "확인");
            }

            ExitEditor(1);
            return;
        }

        Debug.Log($"[WebGLBuild] {sizeMessage}");
        ExitEditor(0);
    }

    public static void BuildForNetlify()
    {
        PrepareCommonAssets();
        Debug.Log("[WebGLBuild] Restoring WebGL texture quality for Netlify (no size cap)...");
        WebGLTextureHalfResTool.RestoreWebGLQualitySilent();

        if (!RunWebGLBuild("Netlify"))
            return;

        WriteNetlifyDeployFiles();
        LogBuildSize("Netlify");
        ExitEditor(0);
    }

    private static void PrepareCommonAssets()
    {
        Debug.Log("[WebGLBuild] Preparing HorrorAssetRegistry...");
        HorrorAssetRegistryBuilder.Build();

        Debug.Log("[WebGLBuild] Linking WebGL prefabs in map scenes...");
        WebGLPrefabLinker.LinkAllSilent();
    }

    private static bool RunWebGLBuild(string label)
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLBuild] No scenes in Build Settings.");
            ExitEditor(1);
            return false;
        }

        Debug.Log($"[WebGLBuild] Building WebGL for {label} ({scenes.Length} scenes) → {OutputPath}");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[WebGLBuild] Failed: {summary.result} ({summary.totalErrors} errors)");
            ExitEditor(1);
            return false;
        }

        Debug.Log($"[WebGLBuild] Succeeded in {summary.totalTime.TotalMinutes:F1} min → {OutputPath}");
        return true;
    }

    private static void WriteNetlifyDeployFiles()
    {
        Directory.CreateDirectory(OutputPath);

        File.WriteAllText(Path.Combine(OutputPath, "_headers"), NetlifyHeaders);
        File.WriteAllText(Path.Combine(OutputPath, "netlify.toml"), NetlifyToml);

        Debug.Log("[WebGLBuild] Wrote _headers and netlify.toml for Netlify deploy.");
    }

    private static void LogBuildSize(string label)
    {
        string dataPath = Path.Combine(OutputPath, DataFileRelativePath);
        if (!File.Exists(dataPath))
        {
            Debug.LogWarning($"[WebGLBuild] {label}: missing {DataFileRelativePath}");
            return;
        }

        double mb = new FileInfo(dataPath).Length / (1024.0 * 1024.0);
        Debug.Log($"[WebGLBuild] {label} build ready. WebGL.data.br = {mb:F1} MB (no size limit).");
    }

    private const string NetlifyHeaders =
        "/*\n" +
        "  Cross-Origin-Opener-Policy: same-origin\n" +
        "  Cross-Origin-Embedder-Policy: require-corp\n" +
        "\n" +
        "/Build/*.data.br\n" +
        "  Content-Type: application/octet-stream\n" +
        "  Content-Encoding: br\n" +
        "\n" +
        "/Build/*.wasm.br\n" +
        "  Content-Type: application/wasm\n" +
        "  Content-Encoding: br\n" +
        "\n" +
        "/Build/*.framework.js.br\n" +
        "  Content-Type: application/javascript\n" +
        "  Content-Encoding: br\n" +
        "\n" +
        "/Build/*.js.br\n" +
        "  Content-Type: application/javascript\n" +
        "  Content-Encoding: br\n";

    private const string NetlifyToml =
        "# Deploy this folder as the Netlify site root (drag-drop or publish directory).\n" +
        "[build]\n" +
        "  publish = \".\"\n";

    private static bool VerifyItchDataSize(out string message)
    {
        string dataPath = Path.Combine(OutputPath, DataFileRelativePath);
        if (!File.Exists(dataPath))
        {
            message = $"Missing {DataFileRelativePath} — cannot verify itch.io size limit.";
            return false;
        }

        long bytes = new FileInfo(dataPath).Length;
        double mb = bytes / (1024.0 * 1024.0);

        if (bytes <= ItchMaxDataBytes)
        {
            message = $"WebGL.data.br OK for itch.io: {mb:F1} MB (limit 200 MB).";
            return true;
        }

        message =
            $"WebGL.data.br is {mb:F1} MB — over itch.io's 200 MB per-file limit.\n" +
            $"Path: {dataPath}";
        return false;
    }

    private static void ExitEditor(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
#endif
