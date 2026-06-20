#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SmilerTextureReimport
{
    private const string TexturePath = "Assets/Art/Smiler/smiler.png";
    private const string Map03ScenePath = "Assets/Scenes/Map_03.unity";
    private const string SessionKey = "HorrorEscape.SmilerTextureReimport.v7";
    private const string PlayMap03SessionKey = "HorrorEscape.PlayMap03AfterReimport.v4";

    static SmilerTextureReimport()
    {
        EditorApplication.delayCall += TryAutoReimport;
    }

    [MenuItem("Horror Escape/Reimport Smiler And Play Map03")]
    public static void ReimportAndPlayMap03()
    {
        Reimport();
        SessionState.EraseBool(PlayMap03SessionKey);
        TryPlayMap03Once();
    }

    [MenuItem("Horror Escape/Reimport Smiler Texture")]
    public static void ReimportFromMenu() => Reimport();

    public static void ReimportBatch()
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
        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            UnityEngine.Debug.LogError($"SmilerTextureReimport: texture not found at {TexturePath}");
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.SaveAndReimport();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("SmilerTextureReimport: grayscale alpha + reimport complete.");
    }

    private static void TryPlayMap03Once()
    {
        if (SessionState.GetBool(PlayMap03SessionKey, false))
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        SessionState.SetBool(PlayMap03SessionKey, true);

        if (!System.IO.File.Exists(Map03ScenePath))
        {
            UnityEngine.Debug.LogError($"SmilerTextureReimport: Map_03 scene not found at {Map03ScenePath}");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(Map03ScenePath);
            EditorApplication.isPlaying = true;
            UnityEngine.Debug.Log("SmilerTextureReimport: opened Map_03 and entered Play mode.");
        }
    }
}
#endif
