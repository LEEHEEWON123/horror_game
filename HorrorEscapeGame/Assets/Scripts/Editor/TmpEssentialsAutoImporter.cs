#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TmpEssentialsAutoImporter
{
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    static TmpEssentialsAutoImporter()
    {
        EditorApplication.delayCall += TryImport;
    }

    private static void TryImport()
    {
        if (File.Exists(TmpSettingsPath)) return;

        var packagePath = Path.GetFullPath("Packages/com.unity.ugui");
        var unityPackage = packagePath + "/Package Resources/TMP Essential Resources.unitypackage";
        if (!File.Exists(unityPackage))
        {
            Debug.LogWarning("TMP Essential Resources package not found. Import via Window > TextMeshPro > Import TMP Essential Resources.");
            return;
        }

        AssetDatabase.importPackageCompleted += OnImportComplete;
        AssetDatabase.ImportPackage(unityPackage, false);
        Debug.Log("Importing TextMesh Pro Essential Resources...");
    }

    private static void OnImportComplete(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnImportComplete;
        if (packageName.Contains("TMP Essential"))
            Debug.Log("TextMesh Pro Essential Resources imported.");
    }
}
#endif
