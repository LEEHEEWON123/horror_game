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

        Assign<GameObject>(so, "backroomsFloorPrefab",
            "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab/Floor/BR_Floor_3x3.prefab");
        Assign<GameObject>(so, "backroomsCeilingPrefab",
            "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab/Wall/BR_Wall_B_3x3.prefab");
        Assign<GameObject>(so, "backroomsPillarPrefab",
            "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab/Wall/BR_Wall_B_Post_3m.prefab");

        Assign<Material>(so, "sponzaFloorMaterial", SponzaMazeMapBuilder.FloorMatPath);
        Assign<Material>(so, "sponzaWallMaterial", SponzaMazeMapBuilder.WallMatPath);
        Assign<Material>(so, "sponzaAltWallMaterial", SponzaMazeMapBuilder.AltWallMatPath);
        Assign<Material>(so, "sponzaCeilingMaterial", SponzaMazeMapBuilder.CeilingMatPath);

        var nycBuildings = so.FindProperty("nycBuildingPrefabs");
        if (nycBuildings != null)
        {
            var buildingPaths = new[]
            {
                $"{NycCityMapBuilder.BuildingsFolder}/building_4_1 Variant.prefab",
                $"{NycCityMapBuilder.BuildingsFolder}/building_3_1 Variant.prefab",
                $"{NycCityMapBuilder.BuildingsFolder}/building_2_1 Variant.prefab",
                $"{NycCityMapBuilder.BuildingsFolder}/building_1_1 Variant.prefab",
            };
            nycBuildings.arraySize = buildingPaths.Length;
            for (int i = 0; i < buildingPaths.Length; i++)
            {
                var building = AssetDatabase.LoadAssetAtPath<GameObject>(buildingPaths[i]);
                if (building == null)
                    Debug.LogWarning($"[HorrorAssetRegistryBuilder] Not found: {buildingPaths[i]}");
                nycBuildings.GetArrayElementAtIndex(i).objectReferenceValue = building;
            }
        }

        Assign<Material>(so, "nycStreetMaterial",
            "Assets/(HDRP) NYC-Like City Buildings Set (PBR)/Materials/concrate.mat");

        Assign<Material>(so, "backroomsLikeFloorMaterial",
            "Assets/Asset/BackroomsLikeAsset/material/Floor_Carpet_Mat.mat");
        Assign<Material>(so, "backroomsLikeCeilingMaterial",
            "Assets/Asset/BackroomsLikeAsset/material/Ceiling_Office_Mat.mat");
        Assign<GameObject>(so, "backroomsLikePillarPrefab",
            "Assets/Asset/BackroomsLikeAsset/prefab/Walls/Wall_Pillar_A.prefab");

        Assign<GameObject>(so, "prototypeMapPrefab",
            PrototypeMapMapBuilder.PrefabPath);

        const string interiorsRoot = "Assets/LoafbrrAssets/Interiors_A/prefabs";
        Assign<GameObject>(so, "interiorsFloorPrefab", $"{interiorsRoot}/Floor/Floor_3x3.prefab");
        Assign<GameObject>(so, "interiorsCorridorCeilingPrefab", $"{interiorsRoot}/Floor/FloorCeiling_3x3.prefab");
        Assign<GameObject>(so, "interiorsPillarPrefab", $"{interiorsRoot}/wall/Wall_Pillar_A.prefab");
        Assign<GameObject>(so, "interiorsPartitionPrefab", $"{interiorsRoot}/wall/Wall_3m_1Side.prefab");
        Assign<GameObject>(so, "interiorsOuterWallPrefab", $"{interiorsRoot}/wall/Wall_3m.prefab");
        Assign<GameObject>(so, "interiorsBottomTrimPrefab", $"{interiorsRoot}/wall/Wall_Bottom_3m.prefab");
        Assign<GameObject>(so, "interiorsCeilingLightPrefab", $"{interiorsRoot}/Decal/Decal_Ceiling_B_Light.prefab");
        Assign<GameObject>(so, "interiorsRailPrefab", $"{interiorsRoot}/StairParts/Stair_Rail_3m.prefab");
        Assign<GameObject>(so, "interiorsStairsPrefab", $"{interiorsRoot}/StairSet/Stairs_2m_B_Grp.prefab");

        // Player
        Assign<GameObject>(so, "protectiveSuitVisualPrefab",
            "Assets/Protection_suite/Prefab/Mesh_protective suit.prefab");
        Assign<RuntimeAnimatorController>(so, "playerLocomotionController",
            PlayerLocomotionSetup.ControllerPath);

        // Entity visuals
        Assign<GameObject>(so, "mutantVisualPrefab", MutantVisualSetup.PrefabPath);
        Assign<GameObject>(so, "zombieMaleVisualPrefab", EntityVisualSetup.DefaultPrefabPath);
        Assign<GameObject>(so, "pumpkinVisualPrefab", PumpkinVisualSetup.PrefabPath);
        Assign<GameObject>(so, "priestVisualPrefab", PriestVisualSetup.PrefabPath);
        Assign<GameObject>(so, "insurgentVisualPrefab", InsurgentVisualSetup.PrefabPath);

        // Animators
        Assign<RuntimeAnimatorController>(so, "entityLocomotionController",
            "Assets/Zombie_Mutant/Animations/EntityLocomotion.controller");
        Assign<RuntimeAnimatorController>(so, "mutantAnimatorController",
            "Assets/Zombie_Mutant/Animations/EntityLocomotion.controller");

        Assign<Material>(so, "parkgarageFloorMaterial",
            "Assets/Parkgarage/Prefabs/parkgarage/Materials/Parkgarage_floor.mat");
        Assign<Material>(so, "parkgaragePillarMaterial",
            "Assets/Parkgarage/Prefabs/parkgarage/Materials/Big_poles.mat");
        Assign<Material>(so, "parkgarageCeilingMaterial",
            "Assets/Parkgarage/Prefabs/parkgarage/Materials/Floor_up_wall.mat");
        Assign<GameObject>(so, "parkgarageLampPrefab",
            "Assets/Parkgarage/Prefabs/lamps/Lamp_parkgarage_prefab.prefab");

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
