using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

        int linked = 0;
        foreach (var scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool dirty = false;

            dirty |= LinkMap00();
            dirty |= LinkMap01();
            dirty |= LinkMap02();
            dirty |= LinkMap03();
            dirty |= LinkMap04();
            dirty |= LinkMap05();
            dirty |= LinkMap06();
            dirty |= LinkMap07();

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                linked++;
                Debug.Log($"[WebGLPrefabLinker] Saved {scenePath}");
            }
        }

        EditorUtility.DisplayDialog("완료", $"{linked}개 씬 저장 완료.\n이제 WebGL 빌드하세요.", "확인");
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

        int linked = 0;
        foreach (var scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool dirty = false;

            dirty |= LinkMap00();
            dirty |= LinkMap01();
            dirty |= LinkMap02();
            dirty |= LinkMap03();
            dirty |= LinkMap04();
            dirty |= LinkMap05();
            dirty |= LinkMap06();
            dirty |= LinkMap07();

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                linked++;
                Debug.Log($"[WebGLPrefabLinker] Saved {scenePath}");
            }
        }

        Debug.Log($"[WebGLPrefabLinker] Silent link complete ({linked} scenes updated).");
    }

    // ─────────────────────────────────────────────────────────────
    // Map00Bootstrap — cityPrefab
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap00()
    {
        var bootstrap = Object.FindFirstObjectByType<Map00Bootstrap>();
        if (bootstrap == null) return false;

        bool changed = false;
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
        var bootstrap = Object.FindFirstObjectByType<Map01Bootstrap>();
        if (bootstrap == null) return false;

        bool changed = false;
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
        var bootstrap = Object.FindFirstObjectByType<Map02Bootstrap>();
        if (bootstrap == null) return false;

        bool changed = false;
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
    // Map03Bootstrap — smilerTexture
    // ─────────────────────────────────────────────────────────────
    private static bool LinkMap03()
    {
        var bootstrap = Object.FindFirstObjectByType<Map03Bootstrap>();
        if (bootstrap == null) return false;

        bool changed = false;
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
        var bootstrap = Object.FindFirstObjectByType<Map04Bootstrap>();
        if (bootstrap == null) return false;

        bool changed = false;
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
        var bootstrap = Object.FindFirstObjectByType<Map05Bootstrap>();
        if (bootstrap == null) return false;

        bool changed = false;
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
        var bootstrap = Object.FindFirstObjectByType<Map06Bootstrap>();
        if (bootstrap == null) return false;

        bool changed = false;
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
        var bootstrap = Object.FindFirstObjectByType<Map07Bootstrap>();
        if (bootstrap == null) return false;

        bool changed = false;
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
}
