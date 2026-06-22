using UnityEngine;

/// <summary>
/// Resources/HorrorAssetRegistry.asset 에 저장되는 중앙 에셋 레지스트리.
/// WebGL 빌드에서 AssetDatabase 대신 사용됩니다.
/// Tools > Create Horror Asset Registry 로 생성하세요.
/// </summary>
[CreateAssetMenu(fileName = "HorrorAssetRegistry", menuName = "Horror/Asset Registry")]
public class HorrorAssetRegistry : ScriptableObject
{
    private static HorrorAssetRegistry _instance;
    public static HorrorAssetRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<HorrorAssetRegistry>("HorrorAssetRegistry");
            return _instance;
        }
    }

    [Header("Map Prefabs")]
    public GameObject cityPrefab;
    public GameObject backroomsLevelPrefab;
    public GameObject sponzaPrefab;

    [Header("Map_01 Backrooms Pieces")]
    public GameObject backroomsFloorPrefab;
    public GameObject backroomsCeilingPrefab;
    public GameObject backroomsPillarPrefab;

    [Header("Map_02 Parking Garage")]
    public Material parkgarageFloorMaterial;
    public Material parkgaragePillarMaterial;
    public Material parkgarageCeilingMaterial;
    public GameObject parkgarageLampPrefab;

    [Header("Map_03 Materials")]
    public Material sponzaFloorMaterial;
    public Material sponzaWallMaterial;
    public Material sponzaAltWallMaterial;
    public Material sponzaCeilingMaterial;

    [Header("Map_05 NYC City")]
    public GameObject[] nycBuildingPrefabs;
    public Material nycStreetMaterial;

    [Header("Map_04 Prototype")]
    public GameObject prototypeMapPrefab;

    [Header("Map_06 Interiors")]
    public GameObject interiorsFloorPrefab;
    public GameObject interiorsCorridorCeilingPrefab;
    public GameObject interiorsPillarPrefab;
    public GameObject interiorsPartitionPrefab;
    public GameObject interiorsOuterWallPrefab;
    public GameObject interiorsBottomTrimPrefab;
    public GameObject interiorsCeilingLightPrefab;
    public GameObject interiorsRailPrefab;
    public GameObject interiorsStairsPrefab;

    [Header("Map_07 Backrooms Like")]
    public Material backroomsLikeFloorMaterial;
    public Material backroomsLikeCeilingMaterial;
    public GameObject backroomsLikePillarPrefab;

    [Header("Player")]
    public GameObject protectiveSuitVisualPrefab;
    public RuntimeAnimatorController playerLocomotionController;

    [Header("Entity Visuals")]
    public GameObject mutantVisualPrefab;
    public GameObject zombieMaleVisualPrefab;
    public GameObject pumpkinVisualPrefab;
    public GameObject priestVisualPrefab;
    public GameObject insurgentVisualPrefab;

    [Header("Entity Animators")]
    public RuntimeAnimatorController entityLocomotionController;
    public RuntimeAnimatorController mutantAnimatorController;
    public RuntimeAnimatorController pumpkinAnimatorController;
    public RuntimeAnimatorController priestAnimatorController;
    public RuntimeAnimatorController insurgentAnimatorController;

    [Header("Smiler")]
    public Texture2D smilerTexture;

    [Header("Audio")]
    public AudioClip[] entityChaseClips;

    public GameObject ResolvePrefab(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        if (assetPath.EndsWith("BR_Floor_3x3.prefab", System.StringComparison.Ordinal))
            return backroomsFloorPrefab;
        if (assetPath.EndsWith("BR_Wall_B_3x3.prefab", System.StringComparison.Ordinal))
            return backroomsCeilingPrefab;
        if (assetPath.EndsWith("BR_Wall_B_Post_3m.prefab", System.StringComparison.Ordinal))
            return backroomsPillarPrefab;
        if (assetPath.EndsWith("Lamp_parkgarage_prefab.prefab", System.StringComparison.Ordinal))
            return parkgarageLampPrefab;
        if (assetPath.EndsWith("Wall_Pillar_A.prefab", System.StringComparison.Ordinal))
        {
            if (assetPath.Contains("Interiors_A", System.StringComparison.Ordinal))
                return interiorsPillarPrefab;
            return backroomsLikePillarPrefab;
        }

        if (assetPath.Contains("Interiors_A/prefabs", System.StringComparison.Ordinal))
        {
            if (assetPath.EndsWith("Floor/Floor_3x3.prefab", System.StringComparison.Ordinal))
                return interiorsFloorPrefab;
            if (assetPath.EndsWith("Floor/FloorCeiling_3x3.prefab", System.StringComparison.Ordinal))
                return interiorsCorridorCeilingPrefab;
            if (assetPath.EndsWith("wall/Wall_3m_1Side.prefab", System.StringComparison.Ordinal))
                return interiorsPartitionPrefab;
            if (assetPath.EndsWith("wall/Wall_3m.prefab", System.StringComparison.Ordinal))
                return interiorsOuterWallPrefab;
            if (assetPath.EndsWith("wall/Wall_Bottom_3m.prefab", System.StringComparison.Ordinal))
                return interiorsBottomTrimPrefab;
            if (assetPath.EndsWith("Decal/Decal_Ceiling_B_Light.prefab", System.StringComparison.Ordinal))
                return interiorsCeilingLightPrefab;
            if (assetPath.EndsWith("StairParts/Stair_Rail_3m.prefab", System.StringComparison.Ordinal))
                return interiorsRailPrefab;
            if (assetPath.EndsWith("StairSet/Stairs_2m_B_Grp.prefab", System.StringComparison.Ordinal))
                return interiorsStairsPrefab;
        }

        if (assetPath.EndsWith("PrototypeMap.prefab", System.StringComparison.Ordinal))
            return prototypeMapPrefab;

        if (nycBuildingPrefabs != null && assetPath.Contains("Buildings/building_"))
        {
            string fileName = System.IO.Path.GetFileName(assetPath);
            foreach (var prefab in nycBuildingPrefabs)
            {
                if (prefab != null && prefab.name + ".prefab" == fileName)
                    return prefab;
            }

            for (int i = 0; i < NycCityMapBuilder.BuildingPrefabFileNames.Length && i < nycBuildingPrefabs.Length; i++)
            {
                if (fileName == NycCityMapBuilder.BuildingPrefabFileNames[i])
                    return nycBuildingPrefabs[i];
            }
        }

        return null;
    }

    public Material ResolveMaterial(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        if (assetPath.EndsWith("Floor_Carpet_Mat.mat", System.StringComparison.Ordinal))
            return backroomsLikeFloorMaterial;
        if (assetPath.EndsWith("Ceiling_Office_Mat.mat", System.StringComparison.Ordinal))
            return backroomsLikeCeilingMaterial;

        if (assetPath.EndsWith("Parkgarage_floor.mat", System.StringComparison.Ordinal))
            return parkgarageFloorMaterial;
        if (assetPath.EndsWith("Big_poles.mat", System.StringComparison.Ordinal))
            return parkgaragePillarMaterial;
        if (assetPath.EndsWith("Floor_up_wall.mat", System.StringComparison.Ordinal))
            return parkgarageCeilingMaterial;

        return null;
    }

    public Material UrpLitTemplate =>
        sponzaFloorMaterial != null ? sponzaFloorMaterial
        : nycStreetMaterial != null ? nycStreetMaterial
        : sponzaWallMaterial != null ? sponzaWallMaterial
        : sponzaCeilingMaterial;
}
