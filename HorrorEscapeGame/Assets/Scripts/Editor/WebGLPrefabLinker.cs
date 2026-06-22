using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Link WebGL Prefabs
/// 모든 Bootstrap 씬을 열어서 [SerializeField] 프리팹 레퍼런스를 채우고 저장합니다.
/// WebGL 빌드 전에 반드시 한 번 실행하세요.
/// </summary>
public static class WebGLPrefabLinker
{
    [MenuItem("Tools/Link WebGL Prefabs (Run Before WebGL Build)")]
    public static void LinkAll()
    {
        if (!EditorUtility.DisplayDialog("Link WebGL Prefabs",
            "모든 Map 씬을 열어 프리팹 레퍼런스를 연결하고 저장합니다.\n\n현재 씬은 먼저 저장해주세요.",
            "실행", "취소"))
            return;

        var scenes = new[]
        {
            "Assets/Scenes/Map_00.unity",
            "Assets/Scenes/Map_01.unity",
            "Assets/Scenes/Map_02.unity",
            "Assets/Scenes/Map_03.unity",
            "Assets/Scenes/Map_04.unity",
            "Assets/Scenes/Map_05.unity",
            "Assets/Scenes/Map_06.unity",
            "Assets/Scenes/Map_07.unity",
        };

        int linked = LinkAllScenes(scenes);

        string message = linked > 0
            ? $"{linked}개 씬 저장 완료.\n이제 WebGL 빌드하세요."
            : "변경할 씬이 없습니다.\n(이미 Bootstrap + Prefab이 연결되어 있거나, WebGL은 HorrorAssetRegistry로도 동작합니다.)";

        EditorUtility.DisplayDialog("완료", message, "확인");
    }

    public static void LinkAllSilent()
    {
        var scenes = new[]
        {
            "Assets/Scenes/Map_00.unity",
            "Assets/Scenes/Map_01.unity",
            "Assets/Scenes/Map_02.unity",
            "Assets/Scenes/Map_03.unity",
            "Assets/Scenes/Map_04.unity",
            "Assets/Scenes/Map_05.unity",
            "Assets/Scenes/Map_06.unity",
            "Assets/Scenes/Map_07.unity",
        };

        int linked = LinkAllScenes(scenes);
        Debug.Log($"[WebGLPrefabLinker] Silent link complete ({linked} scenes updated).");
    }

    private static int LinkAllScenes(string[] scenes)
    {
        int linked = 0;

        foreach (var scenePath in scenes)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool dirty = LinkScene(scenePath);

            if (!dirty) continue;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            linked++;
            Debug.Log($"[WebGLPrefabLinker] Saved {scenePath}");
        }

        return linked;
    }

    private static bool LinkScene(string scenePath)
    {
        if (scenePath.EndsWith("Map_00.unity", System.StringComparison.Ordinal)) return LinkMap00();
        if (scenePath.EndsWith("Map_01.unity", System.StringComparison.Ordinal)) return LinkMap01();
        if (scenePath.EndsWith("Map_02.unity", System.StringComparison.Ordinal)) return LinkMap02();
        if (scenePath.EndsWith("Map_03.unity", System.StringComparison.Ordinal)) return LinkMap03();
        if (scenePath.EndsWith("Map_04.unity", System.StringComparison.Ordinal)) return LinkMap04();
        if (scenePath.EndsWith("Map_05.unity", System.StringComparison.Ordinal)) return LinkMap05();
        if (scenePath.EndsWith("Map_06.unity", System.StringComparison.Ordinal)) return LinkMap06();
        if (scenePath.EndsWith("Map_07.unity", System.StringComparison.Ordinal)) return LinkMap07();
        return false;
    }

    private static T EnsureBootstrap<T>(string objectName, out bool created) where T : Component
    {
        var bootstrap = Object.FindFirstObjectByType<T>();
        if (bootstrap != null)
        {
            created = false;
            return bootstrap;
        }

        var go = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(go, $"Create {objectName}");
        created = true;
        return go.AddComponent<T>();
    }

    // ─────────────────────────────────────────────────────────────
    // Map00Bootstrap — cityPrefab
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap00()
    {
        var bootstrap = EnsureBootstrap<Map00Bootstrap>("Map00Bootstrap", out bool created);

        bool changed = created;
        var so = new SerializedObject(bootstrap);

        changed |= AssignGameObject(so, "cityPrefab", DemoCityMapBuilder.CityPrefabPath);

        so.ApplyModifiedProperties();
        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    // Map01Bootstrap
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap01()
    {
        var bootstrap = EnsureBootstrap<Map01Bootstrap>("Map01Bootstrap", out bool created);

        bool changed = created;
        var so = new SerializedObject(bootstrap);

        changed |= AssignGameObject(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        changed |= AssignGameObject(so, "backroomsLevelPrefab", BackroomsMapBuilder.TstLevelAssetPath);
        changed |= AssignGameObject(so, "entityVisualPrefab", MutantVisualSetup.PrefabPath);
        changed |= AssignAnimatorController(so, "entityAnimatorController",
            "Assets/Zombie_Mutant/Animations/EntityLocomotion.controller");
        changed |= AssignChaseClips(so, "entityChaseClips");

        so.ApplyModifiedProperties();
        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    // Map02Bootstrap
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap02()
    {
        var bootstrap = EnsureBootstrap<Map02Bootstrap>("Map02Bootstrap", out bool created);

        bool changed = created;
        var so = new SerializedObject(bootstrap);

        changed |= AssignGameObject(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        changed |= AssignGameObject(so, "entityVisualPrefab", EntityVisualSetup.DefaultPrefabPath);
        changed |= AssignAnimatorController(so, "entityAnimatorController",
            "Assets/Zombie_Mutant/Animations/EntityLocomotion.controller");
        changed |= AssignChaseClips(so, "entityChaseClips");

        so.ApplyModifiedProperties();
        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    // Map03Bootstrap — smilerTexture
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap03()
    {
        var bootstrap = EnsureBootstrap<Map03Bootstrap>("Map03Bootstrap", out bool created);

        bool changed = created;
        var so = new SerializedObject(bootstrap);

        changed |= AssignGameObject(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        changed |= AssignTexture2D(so, "smilerTexture", SmilerVisualSetup.TexturePath);
        changed |= AssignChaseClips(so, "entityChaseClips");

        so.ApplyModifiedProperties();
        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    // Map04Bootstrap — pumpkinVisualPrefab
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap04()
    {
        var bootstrap = EnsureBootstrap<Map04Bootstrap>("Map04Bootstrap", out bool created);

        bool changed = created;
        var so = new SerializedObject(bootstrap);

        changed |= AssignGameObject(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        changed |= AssignGameObject(so, "pumpkinVisualPrefab", PumpkinVisualSetup.PrefabPath);
        changed |= AssignAnimatorController(so, "entityAnimatorController",
            PumpkinVisualSetup.ControllerPath);
        changed |= AssignChaseClips(so, "entityChaseClips");

        so.ApplyModifiedProperties();
        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    // Map05Bootstrap — priestVisualPrefab
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap05()
    {
        var bootstrap = EnsureBootstrap<Map05Bootstrap>("Map05Bootstrap", out bool created);

        bool changed = created;
        var so = new SerializedObject(bootstrap);

        changed |= AssignGameObject(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        changed |= AssignGameObject(so, "priestVisualPrefab", PriestVisualSetup.PrefabPath);
        changed |= AssignAnimatorController(so, "entityAnimatorController",
            PriestVisualSetup.ControllerPath);
        changed |= AssignChaseClips(so, "entityChaseClips");

        so.ApplyModifiedProperties();
        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    // Map06Bootstrap
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap06()
    {
        var bootstrap = EnsureBootstrap<Map06Bootstrap>("Map06Bootstrap", out bool created);

        bool changed = created;
        var so = new SerializedObject(bootstrap);

        changed |= AssignGameObject(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        changed |= AssignGameObject(so, "entityVisualPrefab", MutantVisualSetup.PrefabPath);
        changed |= AssignAnimatorController(so, "entityAnimatorController",
            "Assets/Zombie_Mutant/Animations/EntityLocomotion.controller");
        changed |= AssignChaseClips(so, "entityChaseClips");

        so.ApplyModifiedProperties();
        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    // Map07Bootstrap
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap07()
    {
        var bootstrap = EnsureBootstrap<Map07Bootstrap>("Map07Bootstrap", out bool created);

        bool changed = created;
        var so = new SerializedObject(bootstrap);

        changed |= AssignGameObject(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        changed |= ForceAssignGameObject(so, "entityVisualPrefab", InsurgentVisualSetup.PrefabPath);
        changed |= ForceAssignAnimatorController(so, "entityAnimatorController",
            InsurgentVisualSetup.ControllerPath);
        changed |= AssignChaseClips(so, "entityChaseClips");

        so.ApplyModifiedProperties();
        return changed;
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────
    private static bool AssignGameObject(SerializedObject so, string fieldName, string assetPath)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null || prop.objectReferenceValue != null) return false;

        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"[WebGLPrefabLinker] Not found: {assetPath}");
            return false;
        }

        prop.objectReferenceValue = asset;
        return true;
    }

    private static bool AssignAnimatorController(SerializedObject so, string fieldName, string assetPath)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null || prop.objectReferenceValue != null) return false;

        var asset = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"[WebGLPrefabLinker] Not found: {assetPath}");
            return false;
        }

        prop.objectReferenceValue = asset;
        return true;
    }

    private static bool AssignTexture2D(SerializedObject so, string fieldName, string assetPath)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null || prop.objectReferenceValue != null) return false;

        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"[WebGLPrefabLinker] Not found: {assetPath}");
            return false;
        }

        prop.objectReferenceValue = asset;
        return true;
    }

    private static bool AssignChaseClips(SerializedObject so, string fieldName)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null || prop.arraySize > 0) return false;

        var clips = EntityChaseAudioSetup.LoadChaseClips();
        if (clips == null || clips.Length == 0) return false;

        prop.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];

        return true;
    }

    private static bool ForceAssignGameObject(SerializedObject so, string fieldName, string assetPath)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null) return false;

        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"[WebGLPrefabLinker] Not found: {assetPath}");
            return false;
        }

        if (prop.objectReferenceValue == asset) return false;

        prop.objectReferenceValue = asset;
        return true;
    }

    private static bool ForceAssignAnimatorController(SerializedObject so, string fieldName, string assetPath)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null) return false;

        var asset = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"[WebGLPrefabLinker] Not found: {assetPath}");
            return false;
        }

        if (prop.objectReferenceValue == asset) return false;

        prop.objectReferenceValue = asset;
        return true;
    }
}
