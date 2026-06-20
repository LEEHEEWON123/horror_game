using UnityEditor;
using UnityEngine;
using System.IO;

public static class HorrorAssetRegistryBuilder
{
    private const string ResourcesPath = "Assets/Resources";
    private const string AssetPath = "Assets/Resources/HorrorAssetRegistry.asset";

    [MenuItem("Tools/Create Horror Asset Registry (Run Before WebGL Build)")]
    public static void Build()
    {
        if (!Directory.Exists(ResourcesPath))
            Directory.CreateDirectory(ResourcesPath);

        var registry = AssetDatabase.LoadAssetAtPath<HorrorAssetRegistry>(AssetPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<HorrorAssetRegistry>();
            AssetDatabase.CreateAsset(registry, AssetPath);
            Debug.Log("[HorrorAssetRegistryBuilder] Created new HorrorAssetRegistry.asset");
        }

        var so = new SerializedObject(registry);

        // Map prefabs
        Assign<GameObject>(so, "cityPrefab", DemoCityMapBuilder.CityPrefabPath);
        Assign<GameObject>(so, "backroomsLevelPrefab", BackroomsMapBuilder.TstLevelAssetPath);
        Assign<GameObject>(so, "sponzaPrefab", SponzaMazeMapBuilder.SponzaFbxPath);

        Assign<Material>(so, "sponzaFloorMaterial", SponzaMazeMapBuilder.FloorMatPath);
        Assign<Material>(so, "sponzaWallMaterial", SponzaMazeMapBuilder.WallMatPath);
        Assign<Material>(so, "sponzaAltWallMaterial", SponzaMazeMapBuilder.AltWallMatPath);
        Assign<Material>(so, "sponzaCeilingMaterial", SponzaMazeMapBuilder.CeilingMatPath);

        // Player
        Assign<GameObject>(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");

        // Entity visuals
        Assign<GameObject>(so, "mutantVisualPrefab", MutantVisualSetup.PrefabPath);
        Assign<GameObject>(so, "pumpkinVisualPrefab", PumpkinVisualSetup.PrefabPath);
        Assign<GameObject>(so, "priestVisualPrefab", PriestVisualSetup.PrefabPath);
        Assign<GameObject>(so, "insurgentVisualPrefab", InsurgentVisualSetup.PrefabPath);

        // Animators
        Assign<RuntimeAnimatorController>(so, "mutantAnimatorController",
            "Assets/Zombie_Mutant/Animations/EntityLocomotion.controller");
        Assign<RuntimeAnimatorController>(so, "pumpkinAnimatorController",
            PumpkinVisualSetup.ControllerPath);
        Assign<RuntimeAnimatorController>(so, "priestAnimatorController",
            PriestVisualSetup.ControllerPath);
        Assign<RuntimeAnimatorController>(so, "insurgentAnimatorController",
            InsurgentVisualSetup.ControllerPath);

        // Smiler
        Assign<Texture2D>(so, "smilerTexture", SmilerVisualSetup.TexturePath);

        // Chase audio clips
        var clips = EntityChaseAudioSetup.LoadChaseClips();
        if (clips != null && clips.Length > 0)
        {
            var clipsProp = so.FindProperty("entityChaseClips");
            clipsProp.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("완료",
                $"HorrorAssetRegistry 생성/업데이트 완료!\n경로: {AssetPath}\n\n이제 WebGL 빌드하세요.",
                "확인");

            Selection.activeObject = registry;
            EditorGUIUtility.PingObject(registry);
        }
        else
        {
            Debug.Log($"[HorrorAssetRegistryBuilder] Updated {AssetPath}");
        }
    }

    private static void Assign<T>(SerializedObject so, string fieldName, string assetPath)
        where T : Object
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null) return;

        var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"[HorrorAssetRegistryBuilder] Not found: {assetPath}");
            return;
        }

        prop.objectReferenceValue = asset;
    }
}
