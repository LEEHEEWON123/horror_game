#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class BackroomsFbxReimport
{
    private const string FbxPath =
        "Assets/LoafbrrAssets/BackroomsLikeAssetRe/FBX/BackroomsLikeAsset-RE.fbx";

    private const string SessionKey = "HorrorEscape.BackroomsFbxReimport.v1";

    static BackroomsFbxReimport()
    {
        EditorApplication.delayCall += TryAutoReimport;
    }

    [MenuItem("Horror Escape/Reimport Backrooms FBX")]
    public static void ReimportFromMenu()
    {
        Reimport();
    }

    private static void TryAutoReimport()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        Reimport();
    }

    public static void Reimport()
    {
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
        {
            UnityEngine.Debug.LogError($"BackroomsFbxReimport: FBX not found at {FbxPath}");
            return;
        }

        importer.isReadable = true;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("BackroomsFbxReimport: Read/Write enabled + reimport complete.");
    }
}
#endif
