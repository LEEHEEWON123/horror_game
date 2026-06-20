#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// itch.io WebGL 빌드: Unity 메뉴 Tools > Build WebGL for itch.io
/// CLI: Unity -batchmode -executeMethod WebGLBuild.BuildForItch -projectPath ...
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
                "WebGL Build",
                "Registry 갱신 → Prefab 링크 → WebGL 빌드를 실행합니다.\n\n" +
                $"출력: {OutputPath}\n\n시간이 꽤 걸릴 수 있습니다.",
                "빌드",
                "취소"))
            return;

        BuildForItch();
    }

    public static void BuildForItch()
    {
        Debug.Log("[WebGLBuild] Preparing HorrorAssetRegistry...");
        HorrorAssetRegistryBuilder.Build();

        Debug.Log("[WebGLBuild] Linking WebGL prefabs in map scenes...");
        WebGLPrefabLinker.LinkAllSilent();

        Debug.Log("[WebGLBuild] Shrinking textures for itch.io 200MB limit (2nd half-res + compression)...");
        WebGLTextureHalfResTool.PrepareForItchSilent();

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLBuild] No scenes in Build Settings.");
            ExitEditor(1);
            return;
        }

        Debug.Log($"[WebGLBuild] Building WebGL ({scenes.Length} scenes) → {OutputPath}");

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
            return;
        }

        Debug.Log($"[WebGLBuild] Succeeded in {summary.totalTime.TotalMinutes:F1} min → {OutputPath}");

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
